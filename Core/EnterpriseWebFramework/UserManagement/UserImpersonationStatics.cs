using EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;
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
		RequestStateStatics.RefreshRequestState();
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public static void EndImpersonation() {
		clearCookie();
		RequestStateStatics.RefreshRequestState();
	}

	internal static void SetCookie( SystemUser? userBeingImpersonated ) {
		CookieStatics.SetCookie( CookieName, userBeingImpersonated?.UserId.ToString() ?? "", null, EwfConfigurationStatics.AppSupportsSecureConnections, true );
	}

	private static void clearCookie() {
		CookieStatics.ClearCookie( CookieName );
	}

	internal static string CookieName => "UserBeingImpersonated";
}