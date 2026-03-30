using Azure;
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

	public static string DiscoverGeneralStorageAccountName( DefaultAzureCredential credential ) {
		var client = new ArmClient( credential );
		foreach( var subscription in client.GetSubscriptions() ) {
			if( !subscription.GetResourceGroups().Exists( "rg-general" ) )
				continue;

			return subscription.GetResourceGroups().Get( "rg-general" ).Value.GetStorageAccounts().Single().Data.Name;
		}
		throw new Exception();
	}

	public static string DiscoverGeneralContainerRegistryLoginServer( DefaultAzureCredential credential ) {
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
		$"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}:{getInstallationName( installationShortName )}";

	public static string GetContainerAppJobName( InstallationConfiguration installationConfiguration, InstallationType installationType ) =>
		$"caj-{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-system";

	public static string GetDataMigratorIdentityName( InstallationConfiguration installationConfiguration, string installationShortName ) =>
		$"id-{getSystemName( installationConfiguration )}-{getInstallationName( installationShortName )}-datamigrator";

	public static string GetDataPackageContainerUrl(
		string storageAccountName, InstallationConfiguration installationConfiguration, InstallationType installationType ) {
		var containerName = $"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-data-packages";
		return $"https://{storageAccountName}.blob.core.windows.net/{containerName}";
	}

	public static string GetDataBlobPrefix( string installationShortName ) => $"{getInstallationName( installationShortName )}-";

	public static string GetAppServiceName( InstallationConfiguration installationConfiguration, string installationShortName ) =>
		$"app-{getSystemName( installationConfiguration )}-{getInstallationName( installationShortName )}";

	private static string getSystemName( InstallationConfiguration installationConfiguration ) => installationConfiguration.SystemShortName.ToUrlSlug();

	private static string getInstallationName( string installationShortName ) => installationShortName.ToUrlSlug();

	private static string getInstallationType( InstallationType installationType ) => installationType == InstallationType.Live ? "prod" : "intermediate";


	public static void RunContainerAppJob( InstallationConfiguration configuration, IEnumerable<string> arguments ) {
		var credential = new DefaultAzureCredential();
		var container = new JobExecutionContainer
			{
				Image =
					$"{DiscoverGeneralContainerRegistryLoginServer( credential )}/{GetContainerImageName( configuration, configuration.InstallationShortName, configuration.InstallationType )}",
				Name = "main"
			};
		foreach( var arg in arguments )
			container.Args.Add( arg );

		new ArmClient( credential ).GetContainerAppJobResource(
				ContainerAppJobResource.CreateResourceIdentifier(
					configuration.AzureHosting!.SubscriptionId,
					GetResourceGroupName( configuration, configuration.InstallationType ),
					GetContainerAppJobName( configuration, configuration.InstallationType ) ) )
			.Start( WaitUntil.Completed, template: new ContainerAppJobExecutionTemplate { Containers = { container } } );
	}
}