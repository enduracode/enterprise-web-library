using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

/// <summary>
/// Application-specific authentication logic.
/// </summary>
public class AppAuthenticationProvider {
	/// <summary>
	/// Returns the components that identify the authenticated user and let them log out, change their password, etc. Returns null for the framework’s built-in
	/// components.
	/// </summary>
	protected internal virtual IReadOnlyCollection<FlowComponent>? GetUserInfoComponents() => null;

	/// <summary>
	/// Returns the home resource for the authenticated user, which the framework uses as the destination after log-in if there is no return URL.
	/// </summary>
	protected internal virtual TrustedResourceInfo GetAuthenticatedUserHomeResource() => EwfConfigurationStatics.GetDefaultBaseResource();

	/// <summary>
	/// Returns the default log-in page for the application, or null for the framework’s built-in page.
	/// </summary>
	protected internal virtual PageContent? GetLogInPageContent( TrustedUrl? returnUrl, Action<TrustedUrl> returnUrlUpdater, string user, string code ) => null;

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
	protected internal virtual PageContent? GetChangePasswordPageContent( TrustedResourceInfo returnResource ) => null;
}