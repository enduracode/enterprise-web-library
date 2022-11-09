namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	/// <summary>
	/// Application-specific authentication logic.
	/// </summary>
	public abstract class AppAuthenticationProvider {
		/// <summary>
		/// Returns the default log-in page for the application, or null for the framework’s built-in page.
		/// </summary>
		protected internal virtual PageContent GetLogInPageContent( string returnUrl, string user, string code ) => null;
	}
}