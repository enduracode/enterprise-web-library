using System.IO;
using RedStapler.StandardLibrary.Configuration.InstallationStandard;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;

namespace RedStapler.StandardLibrary.Configuration {
	/// <summary>
	/// A web application.
	/// </summary>
	public class WebApplication {
		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public readonly string Name;

		internal readonly bool SupportsSecureConnections;
		internal readonly BaseUrl DefaultBaseUrl;
		internal readonly DefaultCookieAttributes DefaultCookieAttributes;

		internal WebApplication(
			string name, bool supportsSecureConnections, string installationPath, string systemShortName, bool systemHasMultipleWebApplications, WebProject configuration ) {
			Name = name;
			SupportsSecureConnections = supportsSecureConnections;

			var iisExpress =
				File.ReadAllText( StandardLibraryMethods.CombinePaths( installationPath, name, name + ".csproj" ) ).Contains( "<UseIISExpress>true</UseIISExpress>" );

			// We must pass values for all components since we will not have defaults to fall back on when getting the URL string for this object.
			DefaultBaseUrl = new BaseUrl(
				"localhost",
				iisExpress ? 8080 : 80,
				iisExpress ? 44300 : 443,
				systemShortName + ( systemHasMultipleWebApplications ? name.EnglishToPascal() : "" ) );

			var cookieAttributes = configuration.DefaultCookieAttributes;
			DefaultCookieAttributes = cookieAttributes != null
				                          ? new DefaultCookieAttributes( null, cookieAttributes.Path, cookieAttributes.NamePrefix )
				                          : new DefaultCookieAttributes( null, null, null );
		}

		internal WebApplication( string name, bool supportsSecureConnections, bool machineIsStandbyServer, LiveInstallationWebApplication configuration )
			: this(
				name,
				supportsSecureConnections,
				machineIsStandbyServer ? configuration.StandbyDefaultBaseUrl : configuration.DefaultBaseUrl,
				machineIsStandbyServer ? configuration.StandbyDefaultCookieAttributes : configuration.DefaultCookieAttributes ) {}

		internal WebApplication( string name, bool supportsSecureConnections, IntermediateInstallationWebApplication configuration )
			: this( name, supportsSecureConnections, configuration.DefaultBaseUrl, configuration.DefaultCookieAttributes ) {}

		internal WebApplication(
			string name, bool supportsSecureConnections, InstallationStandardBaseUrl baseUrl, InstallationStandardCookieAttributes cookieAttributes ) {
			Name = name;
			SupportsSecureConnections = supportsSecureConnections;

			// We must pass values for all components since we will not have defaults to fall back on when getting the URL string for this object.
			DefaultBaseUrl = new BaseUrl(
				baseUrl.Host,
				baseUrl.NonsecurePortSpecified ? baseUrl.NonsecurePort : 80,
				baseUrl.SecurePortSpecified ? baseUrl.SecurePort : 443,
				baseUrl.Path ?? "" );

			DefaultCookieAttributes = cookieAttributes != null
				                          ? new DefaultCookieAttributes( cookieAttributes.Domain, cookieAttributes.Path, cookieAttributes.NamePrefix )
				                          : new DefaultCookieAttributes( null, null, null );
		}
	}
}