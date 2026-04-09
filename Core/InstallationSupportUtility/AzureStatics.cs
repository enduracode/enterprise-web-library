using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.Storage;
using EnterpriseWebLibrary.Configuration;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

[ PublicAPI ]
public static class AzureStatics {
	// names follow Cloud Adoption Framework; see https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming

	private static string discoverGeneralStorageAccountName( TokenCredential credential ) {
		var client = new ArmClient( credential );
		foreach( var subscription in client.GetSubscriptions() ) {
			if( !subscription.GetResourceGroups().Exists( "rg-general" ) )
				continue;

			return subscription.GetResourceGroups().Get( "rg-general" ).Value.GetStorageAccounts().Single().Data.Name;
		}
		throw new Exception();
	}

	public static string DiscoverGeneralContainerRegistryLoginServer( TokenCredential credential ) {
		var client = new ArmClient( credential );
		foreach( var subscription in client.GetSubscriptions() ) {
			if( !subscription.GetResourceGroups().Exists( "rg-general" ) )
				continue;

			return subscription.GetResourceGroups().Get( "rg-general" ).Value.GetContainerRegistries().Single().Data.LoginServer;
		}
		throw new Exception();
	}

	public static string GetResourceGroupName( InstallationConfiguration installationConfiguration, InstallationType installationType ) =>
		$"rg-{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}";

	public static string GetContainerImageName(
		InstallationConfiguration installationConfiguration, string installationShortName, InstallationType installationType ) =>
		$"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}:{GetInstallationName( installationShortName )}";

	public static string GetContainerAppJobName( InstallationConfiguration installationConfiguration, string installationShortName ) =>
		$"caj-{getSystemName( installationConfiguration )}-{GetInstallationName( installationShortName )}";

	internal static string GetDataMigratorIdentityName( InstallationConfiguration installationConfiguration, string installationShortName ) =>
		$"id-{getSystemName( installationConfiguration )}-{GetInstallationName( installationShortName )}-datamigrator";

	public static string GetIsuContainerAppJobName( InstallationConfiguration installationConfiguration, InstallationType installationType ) =>
		$"caj-{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-isu";

	public static string GetIsuInstallationContainerName( InstallationConfiguration installationConfiguration, InstallationType installationType ) =>
		$"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-isu-installations";

	internal static string GetDataPackageContainerUrl(
		InstallationConfiguration installationConfiguration, InstallationType installationType, TokenCredential? credential = null ) {
		var containerName = $"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-data-packages";
		return GetStorageContainerUrl( containerName, credential: credential );
	}

	public static string GetDataBlobPrefix( string installationShortName ) => $"{GetInstallationName( installationShortName )}-";

	public static string GetAppServiceName( InstallationConfiguration installationConfiguration, string installationShortName ) =>
		$"app-{getSystemName( installationConfiguration )}-{GetInstallationName( installationShortName )}";

	private static string getSystemName( InstallationConfiguration installationConfiguration ) => installationConfiguration.SystemShortName.ToUrlSlug();

	public static string GetInstallationName( string installationShortName ) => installationShortName.ToUrlSlug();

	private static string getInstallationType( InstallationType installationType ) => installationType == InstallationType.Live ? "prod" : "intermediate";


	public static string GetStorageContainerUrl( string containerName, TokenCredential? credential = null ) {
		var storageAccountName = discoverGeneralStorageAccountName( credential ?? new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned ) );
		return $"https://{storageAccountName}.blob.core.windows.net/{containerName}";
	}

	internal static void RunContainerAppJob( InstallationConfiguration configuration, IEnumerable<string> arguments ) {
		var credential = new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned );
		var container = new JobExecutionContainer { Name = "main" };
		foreach( var arg in arguments )
			container.Args.Add( arg );

		new ArmClient( credential ).GetContainerAppJobResource(
				ContainerAppJobResource.CreateResourceIdentifier(
					configuration.AzureHosting!.SubscriptionId,
					GetResourceGroupName( configuration, configuration.InstallationType ),
					GetContainerAppJobName( configuration, configuration.InstallationShortName ) ) )
			.Start( WaitUntil.Completed, template: new ContainerAppJobExecutionTemplate { Containers = { container } } );
	}
}