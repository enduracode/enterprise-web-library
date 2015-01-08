using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal class CookieStatics {
		internal static string GetAppCookiePath() {
			// It's important that the cookie path not end with a slash. If it does, Internet Explorer will not transmit the cookie if the user requests the root URL
			// of the application without a trailing slash, e.g. integration.redstapler.biz/Todd. One justification for adding a trailing slash to the cookie path is
			// http://stackoverflow.com/questions/2156399/restful-cookie-path-fails-in-ie-without-trailing-slash.
			return EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes.Path ?? HttpRuntime.AppDomainAppVirtualPath;
		}
	}
}