namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

/// <summary>
/// Application-specific authentication logic.
/// </summary>
public class AppAuthenticationProvider {
	/// <summary>
	/// Returns the default log-in page for the application, or null for the framework’s built-in page.
	/// </summary>
	protected internal virtual PageContent? GetLogInPageContent( string returnUrl, string user, string code, bool authenticatedUserDeniedAccess ) => null;

	/// <summary>
	/// Returns the configuration for automatic user registration via the local identity provider on the default log-in page for the application. Only the
	/// framework’s built-in page uses this method, so you must implement the functionality yourself when using <see cref="GetLogInPageContent"/>.
	/// </summary>
	protected internal virtual LogInPageAutoUserRegistrationSetup? GetLogInPageAutoUserRegistrationSetup() => null;

	/// <summary>
	/// Returns the components to be shown at the bottom of the framework’s built-in log-in page.
	/// </summary>
	protected internal virtual IReadOnlyCollection<FlowComponent> GetLogInPageSpecialInstructions() => [ ];

	/// <summary>
	/// Returns the change-password page for the application, or null for the framework’s built-in page.
	/// </summary>
	protected internal virtual PageContent? GetChangePasswordPageContent( string returnUrl ) => null;
}