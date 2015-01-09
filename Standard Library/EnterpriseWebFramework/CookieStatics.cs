using System;
using System.Web;
using Humanizer;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal class CookieStatics {
		internal static HttpCookie GetCookie( string name, bool omitNamePrefix = false ) {
			var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
			return HttpContext.Current.Request.Cookies[ ( omitNamePrefix ? "" : defaultAttributes.NamePrefix ?? "" ) + name ];
		}

		internal static void SetCookie(
			string name, string value, DateTime? expires, bool secure, bool httpOnly, string domain = null, string path = null, bool omitNamePrefix = false ) {
			var nameAndDomainAndPath = getNameAndDomainAndPath( name, domain, path, omitNamePrefix );
			HttpContext.Current.Response.Cookies.Add(
				new HttpCookie( nameAndDomainAndPath.Item1, value )
					{
						Domain = nameAndDomainAndPath.Item2,
						Path = nameAndDomainAndPath.Item3,
						Expires = expires ?? DateTime.MinValue,
						Secure = secure,
						HttpOnly = httpOnly
					} );
		}

		internal static void ClearCookie( string name, string domain = null, string path = null, bool omitNamePrefix = false ) {
			var nameAndDomainAndPath = getNameAndDomainAndPath( name, domain, path, omitNamePrefix );
			HttpContext.Current.Response.Cookies.Add(
				new HttpCookie( nameAndDomainAndPath.Item1 )
					{
						Domain = nameAndDomainAndPath.Item2,
						Path = nameAndDomainAndPath.Item3,
						Expires = DateTime.Now.AddDays( -1 )
					} );
		}

		private static Tuple<string, string, string> getNameAndDomainAndPath( string name, string domain, string path, bool omitNamePrefix ) {
			var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
			var defaultBaseUrl = new Uri( EwfApp.GetDefaultBaseUrl( false ) );

			domain = domain ?? defaultAttributes.Domain ?? defaultBaseUrl.Host;

			// It's important that the cookie path not end with a slash. If it does, Internet Explorer will not transmit the cookie if the user requests the root URL
			// of the application without a trailing slash, e.g. integration.redstapler.biz/Todd. One justification for adding a trailing slash to the cookie path is
			// http://stackoverflow.com/questions/2156399/restful-cookie-path-fails-in-ie-without-trailing-slash.
			path = path ?? defaultAttributes.Path;
			path = path != null ? "/" + path : defaultBaseUrl.AbsolutePath;

			// Ensure that the domain and path of the cookie are in scope for both the request URL and resource URL. These two URLs can be different on shortcut URL
			// requests, requests that transfer to the log-in page, etc.
			var currentUrls = new[] { AppRequestState.Instance.Url, EwfPage.Instance.InfoAsBaseType.GetUrl() };
			foreach( var url in currentUrls ) {
				var uri = new Uri( url );
				if( !( "." + uri.Host ).EndsWith( "." + domain ) )
					throw new ApplicationException( "The cookie domain of \"{0}\" is not in scope for \"{1}\".".FormatWith( domain, url ) );
				if( !( uri.AbsolutePath + "/" ).StartsWith( path + "/" ) )
					throw new ApplicationException( "The cookie path of \"{0}\" is not in scope for \"{1}\".".FormatWith( path, url ) );
			}

			return Tuple.Create( ( omitNamePrefix ? "" : defaultAttributes.NamePrefix ?? "" ) + name, domain, path );
		}
	}
}