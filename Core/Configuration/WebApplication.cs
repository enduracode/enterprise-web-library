using System.IO;
using System.Linq;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;

namespace EnterpriseWebLibrary.Configuration {
	/// <summary>
	/// A web application.
	/// </summary>
	public class WebApplication {
		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public const string WebConfigFileName = "Web.config";

		/// <summary>
		/// Development Utility and internal use only.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Development Utility and internal use only.
		/// </summary>
		public readonly string Path;

		internal readonly bool SupportsSecureConnections;
		internal readonly IisApplication IisApplication;
		internal readonly BaseUrl DefaultBaseUrl;
		internal readonly DefaultCookieAttributes DefaultCookieAttributes;

		internal WebApplication(
			string name, string installationPath, bool supportsSecureConnections, string systemShortName, bool systemHasMultipleWebApplications,
			WebProject configuration ) {
			Name = name;
			Path = EwlStatics.CombinePaths( installationPath, name );
			SupportsSecureConnections = supportsSecureConnections;

			var iisExpress = File.ReadAllText( EwlStatics.CombinePaths( Path, name + ".csproj" ) ).Contains( "<UseIISExpress>true</UseIISExpress>" );

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

		internal WebApplication(
			string name, string installationPath, bool supportsSecureConnections, InstallationStandardWebApplication configuration, string installationFullShortName,
			bool systemHasMultipleWebApplications ): this(
			name,
			installationPath,
			supportsSecureConnections,
			configuration.IisApplication,
			installationFullShortName,
			systemHasMultipleWebApplications,
			configuration.DefaultBaseUrl,
			configuration.DefaultCookieAttributes ) {}

		internal WebApplication(
			string name, string installationPath, bool supportsSecureConnections, IisApplication iisApplication, string installationFullShortName,
			bool systemHasMultipleWebApplications, InstallationStandardBaseUrl baseUrl, InstallationStandardCookieAttributes cookieAttributes ) {
			Name = name;
			Path = EwlStatics.CombinePaths( installationPath, name );
			SupportsSecureConnections = supportsSecureConnections;
			IisApplication = iisApplication;

			var site = iisApplication as Site;
			var siteHostName = site?.HostNames.First();
			var virtualDirectory = iisApplication as VirtualDirectory;

			if( virtualDirectory != null && virtualDirectory.Name == null )
				virtualDirectory.Name = installationFullShortName + ( systemHasMultipleWebApplications ? name.EnglishToPascal() : "" );

			// We must pass values for all components since we will not have defaults to fall back on when getting the URL string for this object.
			DefaultBaseUrl = baseUrl != null
				                 ? new BaseUrl(
					                 baseUrl.Host,
					                 baseUrl.NonsecurePortSpecified ? baseUrl.NonsecurePort : 80,
					                 baseUrl.SecurePortSpecified ? baseUrl.SecurePort : 443,
					                 baseUrl.Path ?? "" )
				                 : site != null
					                 ? new BaseUrl(
						                 siteHostName.Name,
						                 siteHostName.NonsecurePortSpecified ? siteHostName.NonsecurePort : 80,
						                 siteHostName.SecureBinding != null && siteHostName.SecureBinding.PortSpecified ? siteHostName.SecureBinding.Port : 443,
						                 "" )
					                 : new BaseUrl( virtualDirectory.Site, 80, 443, virtualDirectory.Name );

			DefaultCookieAttributes = cookieAttributes != null
				                          ? new DefaultCookieAttributes( cookieAttributes.Domain, cookieAttributes.Path, cookieAttributes.NamePrefix )
				                          : new DefaultCookieAttributes( null, null, null );
		}

		/// <summary>
		/// Internal and Development Utility use only.
		/// </summary>
		public string WebConfigFilePath => EwlStatics.CombinePaths( Path, WebConfigFileName );
	}
}