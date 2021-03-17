using System;
using System.Web;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that defines a particular URL in the application and handles a request for it.
	/// </summary>
	public interface BasicUrlHandler: IEquatable<BasicUrlHandler> {
		/// <summary>
		/// Returns this handler’s URL encoder.
		/// </summary>
		UrlEncoder GetEncoder();

		/// <summary>
		/// Handles an HTTP request for this handler’s URL.
		/// </summary>
		void HandleRequest( HttpContext context );
	}
}