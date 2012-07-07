using System;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Methods that support intermediate installation authentication.
	/// </summary>
	public static class IntermediateAuthenticationMethods {
		private const string cookieName = "IntermediateUser";
		private const string cookieValue = "213aslkja23w09fua90zo9735";

		internal static bool CookieExists() {
			var cookie = HttpContext.Current.Request.Cookies[ cookieName ];
			return cookie != null && cookie.Value == cookieValue;
		}

		/// <summary>
		/// Sets the intermediate user cookie.
		/// </summary>
		public static void SetCookie() {
			HttpContext.Current.Response.Cookies.Add( new HttpCookie( cookieName, cookieValue )
			                                          	{ Path = cookiePath, Secure = true, Expires = DateTime.Now.AddMonths( 1 ), HttpOnly = true } );
		}

		/// <summary>
		/// Clears the intermediate user cookie.
		/// </summary>
		public static void ClearCookie() {
			HttpContext.Current.Response.Cookies.Add( new HttpCookie( cookieName ) { Path = cookiePath, Expires = DateTime.Now.AddDays( -1 ) } );
		}

		private static string cookiePath {
			get {
				// It's important that the cookie path not end with a slash. If it does, Internet Explorer will not transmit the cookie if the user requests the root
				// URL of the site without a trailing slash, e.g. integration.redstapler.biz/Todd. One justification for adding a trailing slash to the cookie path is
				// http://stackoverflow.com/questions/2156399/restful-cookie-path-fails-in-ie-without-trailing-slash.
				return HttpRuntime.AppDomainAppVirtualPath;
			}
		}
	}
}