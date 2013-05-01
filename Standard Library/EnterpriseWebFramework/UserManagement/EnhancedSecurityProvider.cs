using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Implement this in UserManagement to provide additional security requirements.
	/// </summary>
	public interface EnhancedSecurityProvider: FormsAuthCapableUserManagementProvider {
		/// <summary>
		/// This method will be called when a user changes their password.
		/// </summary>
		void ValidatePassword( Validator validator, string password );

		/// <summary>
		/// This method will be called before a user is authenticated. Only called if <see cref="user"/> is not null.
		/// Runs at modification time. It is safe to throw <see cref="EwfException"/> here.
		/// </summary>
		void PreAuthorize( FormsAuthCapableUser user, bool passwordIsValid );

		/// <summary>
		/// If null, defaults to 12 minutes.
		/// </summary>
		int? InActivityTimeoutMinutes { get; }
	}
}