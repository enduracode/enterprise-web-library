using System.Web;

namespace RedStapler.StandardLibrary {
	internal static class HttpRequestTools {
		/// <summary>
		/// Gets the user agent for the request.
		/// </summary>
		internal static string GetUserAgent( this HttpRequest request ) {
			// UserAgent can return null even though this behavior is totally undocumented.
			return request.UserAgent ?? "";
		}
	}
}