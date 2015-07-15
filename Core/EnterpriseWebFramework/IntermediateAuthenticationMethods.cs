using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Methods that support intermediate installation authentication.
	/// </summary>
	public static class IntermediateAuthenticationMethods {
		private const string cookieName = "IntermediateUser";
		private const string cookieValue = "213aslkja23w09fua90zo9735";

		internal static bool CookieExists() {
			var cookie = CookieStatics.GetCookie( cookieName );
			return cookie != null && cookie.Value == cookieValue;
		}

		/// <summary>
		/// Sets the intermediate user cookie.
		/// </summary>
		public static void SetCookie() {
			// The intermediate user cookie is secure to make it harder for unauthorized users to access intermediate installations, which often are placed on the
			// Internet with no additional security.
			CookieStatics.SetCookie( cookieName, cookieValue, DateTime.Now.AddMonths( 1 ), true, true );
		}

		/// <summary>
		/// Clears the intermediate user cookie.
		/// </summary>
		public static void ClearCookie() {
			CookieStatics.ClearCookie( cookieName );
		}
	}
}