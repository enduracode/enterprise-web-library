using EnterpriseWebLibrary.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class EwfConfigurationStatics {
		internal static IWebHostEnvironment HostEnvironment { get; private set; }
		internal static WebApplication AppConfiguration { get; private set; }

		internal static void Init( IWebHostEnvironment hostEnvironment ) {
			HostEnvironment = hostEnvironment;
			AppConfiguration = ConfigurationStatics.InstallationConfiguration.WebApplications.Single( a => a.Name == ConfigurationStatics.AppName );
		}

		internal static bool AppSupportsSecureConnections => AppConfiguration.SupportsSecureConnections;
	}
}