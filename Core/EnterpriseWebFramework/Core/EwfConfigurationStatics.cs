#nullable disable
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class EwfConfigurationStatics {
		internal static WebApplication AppConfiguration { get; private set; }

		internal static void Init() {
			AppConfiguration = ConfigurationStatics.InstallationConfiguration.WebApplications.Single( a => a.Name == ConfigurationStatics.AppName );
		}

		internal static bool AppSupportsSecureConnections => AppConfiguration.SupportsSecureConnections;
	}
}