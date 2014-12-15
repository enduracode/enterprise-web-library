using System.Linq;

namespace RedStapler.StandardLibrary.Configuration {
	public static class ConfigurationStatics {
		/// <summary>
		/// Returns the default base URL for the specified web application. This will never have a trailing slash.
		/// </summary>
		public static string GetWebApplicationDefaultBaseUrl( string applicationName, bool secure ) {
			return AppTools.InstallationConfiguration.WebApplications.Single( i => i.Name == applicationName ).DefaultBaseUrl.GetUrlString( secure );
		}
	}
}