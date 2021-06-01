using System.Web;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Useful methods that require a web context.
	/// </summary>
	public static class EwfExtensionMethods {
		/// <summary>
		/// Returns true if this request is secure. Always use this method instead of IsSecureConnection.
		/// </summary>
		public static bool IsSecure( this HttpRequest request ) => EwfApp.Instance.RequestIsSecure( request );
	}
}