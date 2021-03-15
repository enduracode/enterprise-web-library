using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that defines a particular URL in the application and handles a request for it or any of its descendants.
	/// </summary>
	public interface UrlHandler: BasicUrlHandler, IEquatable<UrlHandler> {
		/// <summary>
		/// Returns the URL handler that will determine this handler’s canonical URL.
		/// </summary>
		UrlHandler GetParent();

		/// <summary>
		/// Returns the parent and child URL handlers that will determine the specified child handler’s canonical URL.
		/// </summary>
		( UrlHandler parent, UrlHandler child ) GetCanonicalHandlerPair( UrlHandler child );

		/// <summary>
		/// Returns this handler’s child URL patterns.
		/// </summary>
		IEnumerable<UrlPattern> GetChildPatterns();
	}
}