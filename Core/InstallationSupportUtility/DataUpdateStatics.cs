using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using EnterpriseWebLibrary.IO;
using Serilog;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

/// <summary>
/// Installation Support Utility use only.
/// </summary>
public class DataUpdateStatics {
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
			packagePath = installation is ExistingInstalledInstallation { ExistingInstalledInstallationLogic.InstallationInAzure: true }
				              ? source.BlobPrefix
				              : source.GetDataPackage( forceNewPackageDownload, operationResult );
		}

		return () => {
			IoMethods.ExecuteWithTempFolder( tempFolderPath => {
				string packageFolderPath;
				if( installation is ExistingInstalledInstallation { ExistingInstalledInstallationLogic.InstallationInAzure: true } ||
				    source?.IsAzureInstallation == true )
					packageFolderPath = packagePath;
				else {
					packageFolderPath = EwlStatics.CombinePaths( tempFolderPath, "Package" );
					if( packagePath.Any() )
						ZipOps.UnZipFileAsFolder( packagePath, packageFolderPath );
				}

				IReadOnlyCollection<string> dataMigrationUsers = [ ];
				IReadOnlyCollection<string> dataModificationUsers = [ ];
				if( installation is ExistingInstalledInstallation { ExistingInstalledInstallationLogic.InstallationInAzure: true } ) {
					dataMigrationUsers = AzureStatics.GetDataMigratorIdentityName(
							installation,
							installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationShortName )
						.ToCollection();
					dataModificationUsers = AzureStatics
						.GetAppServiceName( installation, installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationShortName )
						.ToCollection()
						.Append( AzureStatics.GetContainerAppJobName( installation, installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType ) )
						.Materialize();
				}

				// Delete and re-create databases.
				DataStatics.DeleteAndReCreateDatabase(
					installation,
					installation.ExistingInstallationLogic.Database,
					databaseHasMinimumDataRevision( installation.ExistingInstallationLogic.RuntimeConfiguration.PrimaryDatabaseSystemConfiguration ),
					sourceInstallationType,
					packageFolderPath,
					dataMigrationUsers,
					dataModificationUsers );
				if( recognizedInstallation != null )
					foreach( var secondaryDatabase in recognizedInstallation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
						DataStatics.DeleteAndReCreateDatabase(
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

	private static bool databaseHasMinimumDataRevision( Configuration.SystemGeneral.Database? database ) =>
		( database?.MinimumDataRevisionSpecified ?? false ) && database.MinimumDataRevision > 0;

	/// <summary>
	/// Recompile procedures in secondary Oracle databases in case there are inter-database dependencies that prevented the procedures from being valid when the
	/// database was created.
	/// </summary>
	private static void recompileProceduresInSecondaryOracleDatabases( RecognizedInstallation installation ) {
		foreach( var secondaryDatabase in installation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
			if( secondaryDatabase is DatabaseAbstraction.Databases.Oracle )
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
		database.ShrinkAfterPostUpdateDataCommands(
			installation is ExistingInstalledInstallation { ExistingInstalledInstallationLogic.InstallationInAzure: true } );
	}
}