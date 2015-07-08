using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public static class UserImpersonationStatics {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public static void BeginImpersonation( User userBeingImpersonated ) {
			SetCookie( userBeingImpersonated );
			AppRequestState.Instance.SetUserAndImpersonator( Tuple.Create( userBeingImpersonated ) );
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static void EndImpersonation() {
			clearCookie();
			AppRequestState.Instance.SetUserAndImpersonator( null );
		}

		internal static void SetCookie( User userBeingImpersonated ) {
			AppRequestState.AddNonTransactionalModificationMethod(
				() =>
				CookieStatics.SetCookie(
					CookieName,
					userBeingImpersonated != null ? userBeingImpersonated.UserId.ToString() : "",
					null,
					EwfConfigurationStatics.AppSupportsSecureConnections,
					true ) );
		}

		private static void clearCookie() {
			AppRequestState.AddNonTransactionalModificationMethod( () => CookieStatics.ClearCookie( CookieName ) );
		}

		internal static string CookieName { get { return "UserBeingImpersonated"; } }
	}
}