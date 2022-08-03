using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that defines a particular URL in the application and handles a request to it.
	/// </summary>
	public interface BasicUrlHandler: IEquatable<BasicUrlHandler> {
		/// <summary>
		/// Returns this handler’s URL encoder.
		/// </summary>
		UrlEncoder GetEncoder();

		/// <summary>
		/// Handles an HTTP request.
		/// </summary>
		void HandleRequest( HttpContext context );
	}
}