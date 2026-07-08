using System.Collections.Immutable;
using System.Threading;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppContainers;
using Azure.ResourceManager.AppContainers.Models;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using EnterpriseWebLibrary.Configuration;
using JetBrains.Annotations;
using static MoreLinq.Extensions.AtLeastExtension;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

[ PublicAPI ]
public static class AzureStatics {
	// names follow Cloud Adoption Framework; see https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming

	private static string discoverGeneralStorageAccountName( TokenCredential credential ) {
		var client = new ArmClient( credential );
		foreach( var subscription in client.GetSubscriptions() ) {
			if( !subscription.GetResourceGroups().Exists( "rg-general" ) )
				continue;

			return subscription.GetResourceGroups().Get( "rg-general" ).Value.GetStorageAccounts().Single( i => i.Data.Kind == StorageKind.StorageV2 ).Data.Name;
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

	public static string GetJobContainerUrl(
		InstallationConfiguration installationConfiguration, InstallationType installationType, TokenCredential? credential = null ) {
		var containerName = $"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-jobs";
		return GetStorageContainerUrl( containerName, credential: credential );
	}

	internal static string GetDataMigratorIdentityName( InstallationConfiguration installationConfiguration, string installationShortName ) =>
		$"id-{getSystemName( installationConfiguration )}-{GetInstallationName( installationShortName )}-datamigrator";

	public static string GetDataMigratorIdentityClientId(
		InstallationConfiguration installationConfiguration, string installationShortName, string subscriptionId, InstallationType installationType,
		TokenCredential credential ) {
		return new ArmClient( credential ).GetUserAssignedIdentityResource(
				UserAssignedIdentityResource.CreateResourceIdentifier(
					subscriptionId,
					GetResourceGroupName( installationConfiguration, installationType ),
					GetDataMigratorIdentityName( installationConfiguration, installationShortName ) ) )
			.Get()
			.Value.Data.ClientId!.Value.ToString();
	}

	public static string GetIsuContainerAppJobName( InstallationConfiguration installationConfiguration, InstallationType installationType ) =>
		$"caj-{getSystemName( installationConfiguration )}-{( installationType == InstallationType.Live ? "prod" : "int" )}-isu";

	public static string GetIsuInstallationContainerName( InstallationConfiguration installationConfiguration, InstallationType installationType ) =>
		$"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-isu-installations";

	internal static string GetDataPackageContainerUrl(
		InstallationConfiguration installationConfiguration, InstallationType installationType, TokenCredential? credential = null ) {
		var containerName = $"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-data-packages";
		return GetStorageContainerUrl( containerName, credential: credential );
	}

	public static string GetDataBlobPrefix( string installationShortName ) => $"{GetInstallationName( installationShortName )}-";

	public static string GetIsuJobContainerUrl(
		InstallationConfiguration installationConfiguration, InstallationType installationType, TokenCredential? credential = null ) {
		var containerName = $"{getSystemName( installationConfiguration )}-{getInstallationType( installationType )}-isu-jobs";
		return GetStorageContainerUrl( containerName, credential: credential );
	}

	public static string GetAppServiceName( InstallationConfiguration installationConfiguration, string installationShortName, WebApplication app ) =>
		$"app-brossgroup-{getSystemName( installationConfiguration )}-{GetInstallationName( installationShortName )}" +
		$"{( installationConfiguration.WebApplications.AtLeast( 2 ) ? $"-{app.Name.ToUrlSlug()}" : "" )}";

	private static string getSystemName( InstallationConfiguration installationConfiguration ) => installationConfiguration.SystemShortName.ToUrlSlug();

	public static string GetInstallationName( string installationShortName ) => installationShortName.ToUrlSlug();

	private static string getInstallationType( InstallationType installationType ) => installationType == InstallationType.Live ? "prod" : "intermediate";


	public static string GetStorageContainerUrl( string containerName, TokenCredential? credential = null ) {
		var storageAccountName = discoverGeneralStorageAccountName( credential ?? new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned ) );
		return $"https://{storageAccountName}.blob.core.windows.net/{containerName}";
	}

	internal static void RunContainerAppJob(
		InstallationConfiguration configuration, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environmentVariables = null,
		string input = "" ) {
		var credential = new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned );

		var job = new ArmClient( credential ).GetContainerAppJobResource(
			ContainerAppJobResource.CreateResourceIdentifier(
				configuration.AzureHosting!.SubscriptionId,
				GetResourceGroupName( configuration, configuration.InstallationType ),
				GetContainerAppJobName( configuration, configuration.InstallationShortName ) ) );
		var executionName = job.Start( WaitUntil.Started ).Value.Name;

		InstallationConfiguration.WriteAzureJobStartData(
			GetJobContainerUrl( configuration, configuration.InstallationType, credential: credential ),
			executionName,
			new AzureJobStartData( environmentVariables ?? ImmutableDictionary<string, string>.Empty, arguments, input ) );

		var execution = job.GetContainerAppJobExecutions().Get( executionName ).Value;
		while( true ) {
			var status = execution.Get().Value.Data.Status;

			if( status == JobExecutionRunningState.Succeeded )
				return;

			if( status is not null && status != JobExecutionRunningState.Running && status != JobExecutionRunningState.Processing )
				throw new Exception( $"Container App Job execution failed with status {status}." );

			Thread.Sleep( 10000 );
		}
	}
}