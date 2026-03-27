using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

[ PublicAPI ]
public static class AzureStatics {
	// names follow Cloud Adoption Framework; see https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming

	public static string DiscoverGeneralStorageAccountName( DefaultAzureCredential credential ) {
		var client = new ArmClient( credential );
		foreach( var subscription in client.GetSubscriptions() ) {
			if( !subscription.GetResourceGroups().Exists( "rg-general" ) )
				continue;

			return subscription.GetResourceGroups().Get( "rg-general" ).Value.GetStorageAccounts().Single().Data.Name;
		}
		throw new Exception();
	}

	public static string GetResourceGroupName( ExistingInstallation installation, InstallationType installationType ) =>
		$"rg-{getSystemName( installation )}-{getInstallationType( installationType )}";

	public static string GetContainerImageName( ExistingInstallation installation, string installationShortName, InstallationType installationType ) =>
		$"{getSystemName( installation )}-{getInstallationType( installationType )}:{getInstallationName( installationShortName )}";

	public static string GetContainerAppJobName( ExistingInstallation installation, InstallationType installationType ) =>
		$"caj-{getSystemName( installation )}-{getInstallationType( installationType )}-system";

	public static string GetDataMigratorIdentityName( ExistingInstallation installation, string installationShortName ) =>
		$"id-{getSystemName( installation )}-{getInstallationName( installationShortName )}-datamigrator";

	public static string GetDataPackageContainerUrl( string storageAccountName, ExistingInstallation installation, InstallationType installationType ) {
		var containerName = $"{getSystemName( installation )}-{getInstallationType( installationType )}-data-packages";
		return $"https://{storageAccountName}.blob.core.windows.net/{containerName}";
	}

	public static string GetDataBlobPrefix( string installationShortName ) => $"{getInstallationName( installationShortName )}-";

	public static string GetAppServiceName( ExistingInstallation installation, string installationShortName ) =>
		$"app-{getSystemName( installation )}-{getInstallationName( installationShortName )}";

	private static string getSystemName( ExistingInstallation installation ) =>
		installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName.ToUrlSlug();

	private static string getInstallationName( string installationShortName ) => installationShortName.ToUrlSlug();

	private static string getInstallationType( InstallationType installationType ) => installationType == InstallationType.Live ? "prod" : "intermediate";
}