using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class NonLiveInstallationStatics {
		private const string intermediateAuthenticationCookieName = "IntermediateUser";
		private const string intermediateAuthenticationCookieValue = "213aslkja23w09fua90zo9735";
		private const string warningsHiddenCookieName = "NonLiveWarningsHidden";

		internal static bool IntermediateAuthenticationCookieExists() {
			var cookie = CookieStatics.GetCookie( intermediateAuthenticationCookieName );
			return cookie != null && cookie.Value == intermediateAuthenticationCookieValue;
		}

		/// <summary>
		/// Sets the intermediate user cookie.
		/// </summary>
		public static void SetIntermediateAuthenticationCookie() {
			// The intermediate user cookie is secure to make it harder for unauthorized users to access intermediate installations, which often are placed on the
			// Internet with no additional security.
			CookieStatics.SetCookie(
				intermediateAuthenticationCookieName,
				intermediateAuthenticationCookieValue,
				SystemClock.Instance.GetCurrentInstant() + Duration.FromDays( 30 ),
				true,
				true );
		}

		/// <summary>
		/// Clears the intermediate user cookie.
		/// </summary>
		public static void ClearIntermediateAuthenticationCookie() {
			CookieStatics.ClearCookie( intermediateAuthenticationCookieName );
		}

		/// <summary>
		/// BasicPage.master use only.
		/// </summary>
		public static bool WarningsHiddenCookieExists() => CookieStatics.GetCookie( warningsHiddenCookieName ) != null;

		/// <summary>
		/// EWF use only.
		/// </summary>
		public static void SetWarningsHiddenCookie() =>
			CookieStatics.SetCookie( warningsHiddenCookieName, "", SystemClock.Instance.GetCurrentInstant() + Duration.FromHours( 1 ), false, false );
	}
}