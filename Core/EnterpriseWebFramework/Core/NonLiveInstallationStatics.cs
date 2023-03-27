using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal static class NonLiveInstallationStatics {
	private const string intermediateAuthenticationCookieName = "IntermediateUser";
	private const string intermediateAuthenticationCookieValue = "213aslkja23w09fua90zo9735";
	private const string warningsHiddenCookieName = "NonLiveWarningsHidden";

	private static Func<bool> warningsHiddenInRequestGetter;
	private static Action<bool> warningsHiddenInRequestSetter;

	internal static void Init( Func<bool> warningsHiddenInRequestGetter, Action<bool> warningsHiddenInRequestSetter ) {
		NonLiveInstallationStatics.warningsHiddenInRequestGetter = warningsHiddenInRequestGetter;
		NonLiveInstallationStatics.warningsHiddenInRequestSetter = warningsHiddenInRequestSetter;
	}

	internal static bool IntermediateAuthenticationCookieExists() =>
		CookieStatics.TryGetCookieValue( intermediateAuthenticationCookieName, out var value ) && value == intermediateAuthenticationCookieValue;

	/// <summary>
	/// Sets the intermediate user cookie.
	/// </summary>
	internal static void SetIntermediateAuthenticationCookie() {
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
	internal static void ClearIntermediateAuthenticationCookie() {
		CookieStatics.ClearCookie( intermediateAuthenticationCookieName );
	}

	internal static bool WarningsHiddenCookieExists() => CookieStatics.TryGetCookieValue( warningsHiddenCookieName, out _ ) || warningsHiddenInRequestGetter();

	internal static void SetWarningsHiddenCookie() {
		CookieStatics.SetCookie( warningsHiddenCookieName, "", SystemClock.Instance.GetCurrentInstant() + Duration.FromHours( 1 ), false, false );
		warningsHiddenInRequestSetter( true );
	}
}