using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

namespace EnterpriseWebLibrary {
	// Do NOT add anything new to this class; we will delete it after we figure out where to move the User property.
	public static class AppTools {
		/// <summary>
		/// Gets the user object for the authenticated user. Returns null if the user has not been authenticated. In a web application, do not use from the
		/// initDefaultOptionalParameterPackage or init methods of Info classes because the page has not yet been able to correct the connection security of the
		/// request, if necessary, and because parent authorization logic has not yet executed. To use from
		/// createParentPageInfo--which you should only do if, for a given set of parameters, there is no single parent that all users can access--you must
		/// explicitly specify the connection security as SecureIfPossible in the current item. With this use, keep in mind that no parent authorization
		/// logic has executed and therefore you cannot assume anything about the user. Does not currently work outside of web applications.
		/// </summary>
		public static User User => EwfApp.Instance != null && EwfApp.Instance.RequestState != null ? EwfApp.Instance.RequestState.UserAndImpersonator.Item1 : null;
	}
}