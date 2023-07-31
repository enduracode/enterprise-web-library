#nullable disable
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;

namespace EnterpriseWebLibrary.Configuration;

/// <summary>
/// A web application.
/// </summary>
public class WebApplication {
	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public readonly string Name;

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public readonly string Path;

	internal readonly bool SupportsSecureConnections;
	internal readonly bool? UsesKestrel;
	internal readonly IisApplication IisApplication;
	internal readonly BaseUrl DefaultBaseUrl;
	internal readonly DefaultCookieAttributes DefaultCookieAttributes;

	internal WebApplication(
		string name, string installationPath, bool supportsSecureConnections, string systemShortName, bool systemHasMultipleWebApplications,
		WebProject configuration ) {
		Name = name;
		Path = EwlStatics.CombinePaths( installationPath, name );
		SupportsSecureConnections = supportsSecureConnections;

		var userFilePath = EwlStatics.CombinePaths( Path, name + ".csproj.user" );
		UsesKestrel = !File.Exists( userFilePath ) || File.ReadAllText( userFilePath ).Contains( "<ActiveDebugProfile>Kestrel</ActiveDebugProfile>" );

		// We must pass values for all components since we will not have defaults to fall back on when getting the URL string for this object.
		DefaultBaseUrl = new BaseUrl(
			"localhost",
			UsesKestrel.Value ? 44311 : 80,
			UsesKestrel.Value ? 44310 : 443,
			systemShortName + ( systemHasMultipleWebApplications ? name.EnglishToPascal() : "" ) );

		var cookieAttributes = configuration.DefaultCookieAttributes;
		DefaultCookieAttributes = cookieAttributes != null
			                          ? new DefaultCookieAttributes( null, cookieAttributes.Path, cookieAttributes.NamePrefix )
			                          : new DefaultCookieAttributes( null, null, null );
	}

	internal WebApplication(
		string name, string installationPath, bool supportsSecureConnections, InstallationStandardWebApplication configuration, string installationFullShortName,
		bool systemHasMultipleWebApplications ) {
		Name = name;
		Path = EwlStatics.CombinePaths( installationPath, name );
		SupportsSecureConnections = supportsSecureConnections;
		IisApplication = configuration.IisApplication;

		var site = configuration.IisApplication as Site;
		var siteHostName = site?.HostNames.First();
		var virtualDirectory = configuration.IisApplication as VirtualDirectory;

		if( virtualDirectory != null && virtualDirectory.Name == null )
			virtualDirectory.Name = installationFullShortName + ( systemHasMultipleWebApplications ? name.EnglishToPascal() : "" );

		// We must pass values for all components since we will not have defaults to fall back on when getting the URL string for this object.
		DefaultBaseUrl = configuration.DefaultBaseUrl != null
			                 ?
			                 new BaseUrl(
				                 configuration.DefaultBaseUrl.Host,
				                 configuration.DefaultBaseUrl.NonsecurePortSpecified ? configuration.DefaultBaseUrl.NonsecurePort : 80,
				                 configuration.DefaultBaseUrl.SecurePortSpecified ? configuration.DefaultBaseUrl.SecurePort : 443,
				                 configuration.DefaultBaseUrl.Path ?? "" )
			                 : site != null
				                 ? new BaseUrl(
					                 siteHostName.Name,
					                 siteHostName.NonsecurePortSpecified ? siteHostName.NonsecurePort : 80,
					                 siteHostName.SecureBinding != null && siteHostName.SecureBinding.PortSpecified ? siteHostName.SecureBinding.Port : 443,
					                 "" )
				                 : new BaseUrl( virtualDirectory.Site, 80, 443, virtualDirectory.Name );

		DefaultCookieAttributes = configuration.DefaultCookieAttributes != null
			                          ? new DefaultCookieAttributes(
				                          configuration.DefaultCookieAttributes.Domain,
				                          configuration.DefaultCookieAttributes.Path,
				                          configuration.DefaultCookieAttributes.NamePrefix )
			                          : new DefaultCookieAttributes( null, null, null );
	}

	/// <summary>
	/// Internal and Development Utility use only.
	/// </summary>
	public string WebConfigFilePath => EwlStatics.CombinePaths( Path, "web.config" );

	internal string DiagnosticLogFilePath => EwlStatics.CombinePaths( Path, "Diagnostic Log" + FileExtensions.Txt );
}