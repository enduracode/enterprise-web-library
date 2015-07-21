using System.Linq;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class EwfConfigurationStatics {
		internal static WebApplication AppConfiguration { get; private set; }

		internal static void Init() {
			AppConfiguration = ConfigurationStatics.InstallationConfiguration.WebApplications.Single( a => a.Name == ConfigurationStatics.AppName );
		}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static bool AppSupportsSecureConnections {
			get { return AppConfiguration.SupportsSecureConnections || ConfigurationStatics.IsIntermediateInstallation; }
		}
	}
}