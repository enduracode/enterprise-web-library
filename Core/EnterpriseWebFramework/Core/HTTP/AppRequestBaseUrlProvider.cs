using System.Web;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Application-specific logic for the request base URL.
	/// </summary>
	public class AppRequestBaseUrlProvider {
		/// <summary>
		/// Returns true if the specified request is secure. Override this to be more than just <see cref="HttpRequest.IsSecureConnection"/> if you are using a
		/// reverse proxy to perform SSL termination. Remember that your implementation should support not just live installations, but also development and
		/// intermediate installations.
		/// </summary>
		protected internal virtual bool RequestIsSecure( HttpRequest request ) => request.IsSecureConnection;

		/// <summary>
		/// Returns the host name for the specified request. Override this if you are using a reverse proxy that is changing the Host header. Include the port
		/// number in the return value if it is not the default port. Never return null. If the host name is unavailable (i.e. the request uses HTTP 1.0 and does
		/// not include a Host header), return the empty string, which will cause a 400 status code to be returned. Remember that your implementation should support
		/// not just live installations, but also development and intermediate installations.
		/// </summary>
		protected internal virtual string GetRequestHost( HttpRequest request ) {
			var host = request.Headers[ "Host" ]; // returns null if field missing
			return host ?? "";
		}

		/// <summary>
		/// Returns the base path for the specified request. Override this if you are using a reverse proxy and are changing the base path. Never return null.
		/// Return the empty string to represent the root path. Remember that your implementation should support not just live installations, but also development
		/// and intermediate installations.
		/// </summary>
		protected internal virtual string GetRequestBasePath( HttpRequest request ) =>
			request.RawUrl.Truncate( HttpRuntime.AppDomainAppVirtualPath.Length ).Substring( "/".Length );
	}
}