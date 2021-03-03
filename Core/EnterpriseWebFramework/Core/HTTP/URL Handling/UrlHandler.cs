using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that defines a particular URL in the application and handles a request for it or any of its descendants.
	/// </summary>
	public interface UrlHandler: BasicUrlHandler {
		/// <summary>
		/// Returns the URL handler that will determine this handler’s canonical URL.
		/// </summary>
		UrlHandler GetParent();

		/// <summary>
		/// Returns the URL handler and encoder that will determine the specified encoder’s canonical URL.
		/// </summary>
		( UrlHandler, UrlEncoder ) GetCanonicalHandler( UrlEncoder encoder );

		/// <summary>
		/// Returns this handler’s child URL patterns.
		/// </summary>
		IEnumerable<UrlPattern> GetChildPatterns();
	}
}