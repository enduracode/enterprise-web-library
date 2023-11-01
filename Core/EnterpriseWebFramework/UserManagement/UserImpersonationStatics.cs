#nullable disable
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

/// <summary>
/// EWL use only.
/// </summary>
public static class UserImpersonationStatics {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public static void BeginImpersonation( SystemUser userBeingImpersonated ) {
		SetCookie( userBeingImpersonated );
		AppRequestState.Instance.SetUserAndImpersonator( new SpecifiedValue<SystemUser>( userBeingImpersonated ) );
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public static void EndImpersonation() {
		clearCookie();
		AppRequestState.Instance.SetUserAndImpersonator( null );
	}

	internal static void SetCookie( SystemUser userBeingImpersonated ) {
		AutomaticDatabaseConnectionManager.AddNonTransactionalModificationMethod(
			() => CookieStatics.SetCookie(
				CookieName,
				userBeingImpersonated?.UserId.ToString() ?? "",
				null,
				EwfConfigurationStatics.AppSupportsSecureConnections,
				true ) );
	}

	private static void clearCookie() {
		AutomaticDatabaseConnectionManager.AddNonTransactionalModificationMethod( () => CookieStatics.ClearCookie( CookieName ) );
	}

	internal static string CookieName => "UserBeingImpersonated";
}