using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using EnterpriseWebLibrary.IO;
using JetBrains.Annotations;
using Serilog;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

/// <summary>
/// Installation Support Utility use only.
/// </summary>
[ PublicAPI ]
public static class DataStatics {
	public static string GetPackageZipFilePath( string installationFullName ) =>
		EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "Local Data Packages", installationFullName + ".zip" );

	public static void ExportDatabase( ExistingInstalledInstallation installation, Database database, string packageFolderPath ) {
		if( database is not NoDatabase )
			database.ExportToFile(
				getDatabaseExportFile( installation, database, installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType, packageFolderPath ) );
	}

	public static Action DownloadDataPackageAndGetDataUpdateMethod(
		ExistingInstallation installation, bool installationIsStandbyDb, DataSource? source, bool forceNewPackageDownload, OperationResult operationResult ) {
		var recognizedInstallation = installation as RecognizedInstallation;

		InstallationType sourceInstallationType;
		string packagePath;
		if( source is null ) {
			sourceInstallationType = InstallationType.Intermediate;
			var path = EwlStatics.CombinePaths(
				DataSource.GetDownloadedPackagesFolderPath(),
				installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + FileExtensions.Zip );
			packagePath = File.Exists( path ) ? path : "";
		}
		else {
			sourceInstallationType = source.InstallationType;
			packagePath = installation.ExistingInstallationLogic.InstallationInAzure
				              ? source.BlobPrefix
				              : source.GetDataPackage( forceNewPackageDownload, operationResult );
		}

		return () => {
			IoMethods.ExecuteWithTempFolder( tempFolderPath => {
				string packageFolderPath;
				if( installation.ExistingInstallationLogic.InstallationInAzure || source?.IsAzureInstallation == true )
					packageFolderPath = packagePath;
				else {
					packageFolderPath = EwlStatics.CombinePaths( tempFolderPath, "Package" );
					if( packagePath.Any() )
						ZipOps.UnZipFileAsFolder( packagePath, packageFolderPath );
				}

				IReadOnlyCollection<string> dataMigrationUsers = [ ];
				IReadOnlyCollection<string> dataModificationUsers = [ ];
				if( installation.ExistingInstallationLogic.InstallationInAzure ) {
					dataMigrationUsers = AzureStatics.GetDataMigratorIdentityName(
							installation.ExistingInstallationLogic.RuntimeConfiguration,
							installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationShortName )
						.ToCollection();
					dataModificationUsers = AzureStatics
						.GetAppServiceName(
							installation.ExistingInstallationLogic.RuntimeConfiguration,
							installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationShortName )
						.ToCollection()
						.Append(
							AzureStatics.GetContainerAppJobName(
								installation.ExistingInstallationLogic.RuntimeConfiguration,
								installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType ) )
						.Materialize();
				}

				// Delete and re-create databases.
				deleteAndReCreateDatabase(
					installation,
					installation.ExistingInstallationLogic.Database,
					databaseHasMinimumDataRevision( installation.ExistingInstallationLogic.RuntimeConfiguration.PrimaryDatabaseSystemConfiguration ),
					sourceInstallationType,
					packageFolderPath,
					dataMigrationUsers,
					dataModificationUsers );
				if( recognizedInstallation != null )
					foreach( var secondaryDatabase in recognizedInstallation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
						deleteAndReCreateDatabase(
							installation,
							secondaryDatabase,
							databaseHasMinimumDataRevision(
								installation.ExistingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseSystemConfiguration(
									secondaryDatabase.SecondaryDatabaseName ) ),
							sourceInstallationType,
							packageFolderPath,
								[ ],
								[ ] );
			} );

			DatabaseOps.WaitForDatabaseRecovery( installation.ExistingInstallationLogic.Database );
			if( recognizedInstallation != null )
				recompileProceduresInSecondaryOracleDatabases( recognizedInstallation );

			if( !installationIsStandbyDb ) {
				// Bring database logic up to date with the rest of the logic in this installation. In other words, reapply changes lost when we deleted the database.
				Log.Information( "Migrating data." );
				var message = "Migrated data.";
				try {
					installation.ExistingInstallationLogic.MigrateData();
				}
				catch when( installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType == InstallationType.Development ) {
					message = "Did not migrate data, likely because the system’s Migrator application was not yet available. Please update dependent logic.";
				}
				Log.Information( message );
			}

			// If we’re an intermediate installation and we are getting data from a live installation, sanitize the data and do other conversion commands.
			var installationIsIntermediate = installation is RecognizedInstalledInstallation recognized
				                                 ? recognized.KnownInstallationLogic.RsisInstallation.InstallationTypeElements is IntermediateInstallationElements
				                                 : installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType == InstallationType.Intermediate;
			if( installationIsIntermediate && sourceInstallationType == InstallationType.Live ) {
				Log.Information( "Executing live -> intermediate conversion commands..." );
				doDatabaseLiveToIntermediateConversionIfCommandsExist(
					installation,
					installation.ExistingInstallationLogic.Database,
					installation.ExistingInstallationLogic.RuntimeConfiguration.PrimaryDatabaseSystemConfiguration );
				foreach( var secondaryDatabase in recognizedInstallation!.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
					doDatabaseLiveToIntermediateConversionIfCommandsExist(
						installation,
						secondaryDatabase,
						installation.ExistingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseSystemConfiguration( secondaryDatabase.SecondaryDatabaseName ) );
			}
		};
	}

	private static void deleteAndReCreateDatabase(
		ExistingInstallation installation, Database database, bool databaseHasMinimumDataRevision, InstallationType sourceInstallationType,
		string packageFolderPath, IReadOnlyCollection<string> dataMigrationUsers, IReadOnlyCollection<string> dataModificationUsers ) {
		if( database is NoDatabase )
			return;

		var file = getDatabaseExportFile( installation as ExistingInstalledInstallation, database, sourceInstallationType, packageFolderPath );

		bool fileExists;
		if( file.IsAzureBlob ) {
			file.TryGetAzureBlob( out var containerUrl, out var blobName );
			var blobClient = new BlobClient(
				new Uri( $"{containerUrl}/{blobName}" ),
				new DefaultAzureCredential(
					new DefaultAzureCredentialOptions
						{
							TenantId = ( (ExistingInstalledInstallation)installation ).ExistingInstallationLogic.RuntimeConfiguration.AzureHosting!.TenantId
						} ) );
			if( !( fileExists = blobClient.Exists() ) )
				file = new ExportFile( null, "", "" );
		}
		else {
			file.TryGetFilePath( out var filePath );
			if( !( fileExists = File.Exists( filePath ) ) )
				file = new ExportFile( "", null, null );
		}

		if( databaseHasMinimumDataRevision && !fileExists )
			throw new UserCorrectableException(
				"Failed to re-create the {0} because the data package did not exist, or did not contain a file.".FormatWith(
					DatabaseOps.GetDatabaseNounPhrase( database ) ) );
		database.DeleteAndReCreateFromFile( file, dataMigrationUsers, dataModificationUsers );
		if( !fileExists )
			Log.Information(
				"Created a new {0} because the data package did not exist, or did not contain a file.".FormatWith( DatabaseOps.GetDatabaseNounPhrase( database ) ) );
	}

	private static bool databaseHasMinimumDataRevision( Configuration.SystemGeneral.Database? database ) =>
		( database?.MinimumDataRevisionSpecified ?? false ) && database.MinimumDataRevision > 0;

	/// <summary>
	/// Recompile procedures in secondary Oracle databases in case there are inter-database dependencies that prevented the procedures from being valid when the
	/// database was created.
	/// </summary>
	private static void recompileProceduresInSecondaryOracleDatabases( RecognizedInstallation installation ) {
		foreach( var secondaryDatabase in installation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
			if( secondaryDatabase is Oracle )
				secondaryDatabase.ExecuteDbMethod( cn => {
					foreach( var procedure in secondaryDatabase.GetProcedures() ) {
						var command = cn.DatabaseInfo.CreateCommand();
						command.CommandText = "ALTER PROCEDURE " + procedure + " COMPILE";
						cn.ExecuteNonQueryCommand( command );
					}
				} );
	}

	private static void doDatabaseLiveToIntermediateConversionIfCommandsExist(
		ExistingInstallation installation, Database database, Configuration.SystemGeneral.Database? configuration ) {
		if( !( configuration?.LiveToIntermediateConversionCommands ?? Enumerable.Empty<string>() ).Any() )
			return;

		database.ExecuteDbMethod( cn => {
			foreach( var commandText in configuration!.LiveToIntermediateConversionCommands ) {
				var cmd = cn.DatabaseInfo.CreateCommand();
				cmd.CommandText = commandText;
				cn.ExecuteNonQueryCommand( cmd, isLongRunning: true );
			}
		} );
		database.ShrinkAfterPostUpdateDataCommands( installation.ExistingInstallationLogic.InstallationInAzure );
	}

	private static ExportFile getDatabaseExportFile(
		ExistingInstalledInstallation? installation, Database database, InstallationType exportInstallationType, string packageFolderPath ) {
		var fileName = ( database.SecondaryDatabaseName.Length > 0 ? database.SecondaryDatabaseName : "Primary" ) + ".bak";

		if( installation?.ExistingInstallationLogic.InstallationInAzure == true )
			return new ExportFile(
				null,
				AzureStatics.GetDataPackageContainerUrl(
					AzureStatics.DiscoverGeneralStorageAccountName(
						new DefaultAzureCredential(
							new DefaultAzureCredentialOptions { TenantId = installation.ExistingInstallationLogic.RuntimeConfiguration.AzureHosting!.TenantId } ) ),
					installation.ExistingInstallationLogic.RuntimeConfiguration,
					exportInstallationType ),
				packageFolderPath + fileName );

		return new ExportFile( EwlStatics.CombinePaths( packageFolderPath, fileName ), null, null );
	}
}