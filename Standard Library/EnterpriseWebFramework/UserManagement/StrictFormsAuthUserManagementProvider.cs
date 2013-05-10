using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// System-specific user management logic. Do not use unless the system absolutely requires micromanagement of authentication behavior.
	/// </summary>
	public interface StrictFormsAuthUserManagementProvider: FormsAuthCapableUserManagementProvider {
		/// <summary>
		/// Performs actions immediately after authentication, which could include counting failed authentication attempts or preventing a user from logging in.
		/// Only called if <see cref="user"/> is not null. Runs at modification time; it is safe to throw <see cref="EwfException"/> here.
		/// </summary>
		void PostAuthenticate( FormsAuthCapableUser user, bool authenticationSuccessful );

		/// <summary>
		/// Gets the authentication timeout. Returns null if the default should be used.
		/// </summary>
		int? AuthenticationTimeoutInMinutes { get; }

		/// <summary>
		/// Validates the specified password. Called when a user changes their password.
		/// </summary>
		void ValidatePassword( Validator validator, string password );
	}
}