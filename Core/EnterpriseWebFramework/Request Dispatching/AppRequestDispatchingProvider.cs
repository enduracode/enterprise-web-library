namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Application-specific request-dispatching logic.
	/// </summary>
	public abstract class AppRequestDispatchingProvider {
		/// <summary>
		/// Returns the base URL patterns for the application.
		/// </summary>
		protected internal abstract IEnumerable<BaseUrlPattern> GetBaseUrlPatterns();

		/// <summary>
		/// Returns the URL handler that will determine the canonical URL patterns for the framework.
		/// </summary>
		public abstract UrlHandler GetFrameworkUrlParent();

		/// <summary>
		/// Framework use only.
		/// </summary>
		protected internal abstract UrlPattern GetStaticFilesFolderUrlPattern( string urlSegment );
	}
}