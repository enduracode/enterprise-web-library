using Azure.Identity;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using JetBrains.Annotations;

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
		DatabaseOps.ExportDatabaseToFile( database, getDatabaseExportFile( installation, database, packageFolderPath ) );
	}

	private static ExportFile getDatabaseExportFile( ExistingInstalledInstallation installation, Database database, string packageFolderPath ) {
		var fileName = ( database.SecondaryDatabaseName.Length > 0 ? database.SecondaryDatabaseName : "Primary" ) + ".bak";

		if( installation.ExistingInstalledInstallationLogic.InstallationInAzure )
			return new ExportFile(
				null,
				AzureStatics.GetDataPackageContainerUrl(
					AzureStatics.DiscoverGeneralStorageAccountName(
						new DefaultAzureCredential(
							new DefaultAzureCredentialOptions { TenantId = installation.ExistingInstallationLogic.RuntimeConfiguration.AzureHosting!.TenantId } ) ),
					installation,
					InstallationType.Live ),
				fileName );

		return new ExportFile( EwlStatics.CombinePaths( packageFolderPath, fileName ), null, null );
	}
}