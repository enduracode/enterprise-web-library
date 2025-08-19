using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public static class EwfConfigurationStatics {
	internal static WebApplication AppConfiguration { get; private set; } = null!;

	internal static void Init() {
		AppConfiguration = ConfigurationStatics.InstallationConfiguration.WebApplications.Single( a => a.Name == ConfigurationStatics.AppName );
	}

	internal static bool AppSupportsSecureConnections => AppConfiguration.SupportsSecureConnections;

	internal static TrustedResourceInfo GetDefaultBaseResource() =>
		new TrustedExternalResource( new ExternalResource( AppConfiguration.DefaultBaseUrl.GetUrlString( AppConfiguration.SupportsSecureConnections ) ) );
}