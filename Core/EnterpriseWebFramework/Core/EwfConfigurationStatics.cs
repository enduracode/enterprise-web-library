using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public static class EwfConfigurationStatics {
	internal static WebApplication AppConfiguration { get; private set; } = null!;
	private static Func<ResourceBase> defaultBaseResourceGetter = null!;

	internal static void Init( Func<ResourceBase> defaultBaseResourceGetter ) {
		AppConfiguration = ConfigurationStatics.InstallationConfiguration.WebApplications.Single( a => a.Name == ConfigurationStatics.AppName );
		EwfConfigurationStatics.defaultBaseResourceGetter = defaultBaseResourceGetter;
	}

	internal static bool AppSupportsSecureConnections => AppConfiguration.SupportsSecureConnections;

	internal static TrustedResourceInfo GetDefaultBaseResource() => defaultBaseResourceGetter();
}