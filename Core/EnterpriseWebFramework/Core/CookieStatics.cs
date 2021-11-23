using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Humanizer;
using NodaTime;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class CookieStatics {
		internal static HttpCookie GetCookie( string name, bool omitNamePrefix = false ) {
			var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
			return HttpContext.Current.Request.Cookies[ ( omitNamePrefix ? "" : defaultAttributes.NamePrefix ?? "" ) + name ];
		}

		internal static void SetCookie(
			string name, string value, Instant? expires, bool secure, bool httpOnly, string domain = null, string path = null, bool omitNamePrefix = false ) {
			var nameAndDomainAndPath = getNameAndDomainAndPath( name, domain, path, omitNamePrefix );
			HttpContext.Current.Response.Cookies.Add(
				new HttpCookie( nameAndDomainAndPath.Item1, value )
					{
						Domain = nameAndDomainAndPath.Item2,
						Path = nameAndDomainAndPath.Item3,
						Expires = expires?.ToDateTimeUtc() ?? DateTime.MinValue,
						Secure = secure,
						HttpOnly = httpOnly,
						SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax
					} );
		}

		internal static void ClearCookie( string name, string domain = null, string path = null, bool omitNamePrefix = false ) {
			var nameAndDomainAndPath = getNameAndDomainAndPath( name, domain, path, omitNamePrefix );
			HttpContext.Current.Response.Cookies.Add(
				new HttpCookie( nameAndDomainAndPath.Item1 )
					{
						Domain = nameAndDomainAndPath.Item2,
						Path = nameAndDomainAndPath.Item3,
						Expires = SystemClock.Instance.GetCurrentInstant().Minus( Duration.FromDays( 1 ) ).ToDateTimeUtc()
					} );
		}

		private static Tuple<string, string, string> getNameAndDomainAndPath( string name, string domain, string path, bool omitNamePrefix ) {
			var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
			var defaultBaseUrl = new Uri( AppRequestState.Instance.BaseUrl );

			domain = domain ?? defaultAttributes.Domain ?? "";

			// It's important that the cookie path not end with a slash. If it does, Internet Explorer will not transmit the cookie if the user requests the root URL
			// of the application without a trailing slash, e.g. integration.redstapler.biz/Todd. One justification for adding a trailing slash to the cookie path is
			// http://stackoverflow.com/questions/2156399/restful-cookie-path-fails-in-ie-without-trailing-slash.
			path = path ?? defaultAttributes.Path;
			path = path != null ? "/" + path : defaultBaseUrl.AbsolutePath;

			// Ensure that the domain and path of the cookie are in scope for both the request URL and page URL. These two URLs can be different on requests that
			// transfer to the log-in page, etc.
			var requestUrls = new List<string> { AppRequestState.Instance.Url };
			if( PageBase.Current != null )
				requestUrls.Add( PageBase.Current.GetUrl( false, false ) );
			foreach( var url in requestUrls ) {
				var uri = new Uri( url );
				if( domain.Any() && !( "." + uri.Host ).EndsWith( "." + domain ) )
					throw new ApplicationException( "The cookie domain of \"{0}\" is not in scope for \"{1}\".".FormatWith( domain, url ) );
				if( path != "/" && !( uri.AbsolutePath + "/" ).StartsWith( path + "/" ) )
					throw new ApplicationException( "The cookie path of \"{0}\" is not in scope for \"{1}\".".FormatWith( path, url ) );
			}
			if( !domain.Any() ) {
				var requestHosts = requestUrls.Select( i => new Uri( i ).Host );
				if( requestHosts.Distinct().Count() > 1 )
					throw new ApplicationException(
						"The cookie domain could arbitrarily be either {0} depending upon the request URL.".FormatWith(
							StringTools.ConcatenateWithDelimiter( " or ", requestHosts.ToArray() ) ) );
			}

			return Tuple.Create( ( omitNamePrefix ? "" : defaultAttributes.NamePrefix ?? "" ) + name, domain, path );
		}
	}
}