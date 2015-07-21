using System;
using System.Collections.Generic;
using System.Linq;
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

		/// <summary>
		/// A new instance given by the delimiterCreator function will delimit this collection of Controls.
		/// Do not use this method.
		/// </summary>
		public static List<Control> Delimit<T, R>( this IEnumerable<T> enumerable, Func<R> delimiterCreator ) where T: Control where R: Control {
			var list = new List<Control>();
			foreach( var e in enumerable.Take( enumerable.Count() - 1 ) ) {
				list.Add( e );
				list.Add( delimiterCreator() );
			}
			list.Add( enumerable.Last() );
			return list;
		}

		internal static bool IsOnPage( this Control control ) {
			return control.Page != null;
		}
	}
}