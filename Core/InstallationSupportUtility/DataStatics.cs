using Azure.Identity;
using Azure.Storage.Blobs;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using JetBrains.Annotations;
using Serilog;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

/// <summary>
/// Installation Support Utility use only.
/// </summary>
[ PublicAPI ]
public static class DataStatics {
	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public static string GetPackageZipFilePath( string installationFullName ) =>
		EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "Local Data Packages", installationFullName + ".zip" );

	public static void ExportDatabase( ExistingInstalledInstallation installation, Database database, string packageFolderPath ) {
		if( database is not NoDatabase )
			database.ExportToFile(
				getDatabaseExportFile( installation, database, installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType, packageFolderPath ) );
	}

	public static void DeleteAndReCreateDatabase(
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