using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

/// <summary>
/// EWL use only.
/// </summary>
public static class UserImpersonationStatics {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public static void BeginImpersonation( SystemUser? userBeingImpersonated ) {
		SetCookie( userBeingImpersonated );
		ResourceBase.RefreshRequestState();
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public static void EndImpersonation() {
		clearCookie();
		ResourceBase.RefreshRequestState();
	}

	internal static void SetCookie( SystemUser? userBeingImpersonated ) {
		CookieStatics.SetCookie( CookieName, userBeingImpersonated?.UserId.ToString() ?? "", null, EwfConfigurationStatics.AppSupportsSecureConnections, true );
	}

	private static void clearCookie() {
		CookieStatics.ClearCookie( CookieName );
	}

	internal static string CookieName => "UserBeingImpersonated";
}