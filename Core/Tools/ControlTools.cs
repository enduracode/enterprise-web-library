using System.Web.UI;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// Provides helpful System.Web.UI.Control methods.
	/// </summary>
	public static class ControlTools {
		/// <summary>
		/// Converts a URL into one that is usable on the client side.
		/// </summary>
		public static string GetClientUrl( this Control control, string url ) {
			// ResolveUrl and ResolveClientUrl are almost identical, but ResolveUrl produces site root relative URLs while ResolveClientUrl produces resource relative
			// URLs. Site root relative URLs are robust across Transfer and TransferRequest calls.
			return control.ResolveUrl( url );
		}
	}
}