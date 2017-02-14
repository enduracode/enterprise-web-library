using System;
using System.Web;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal sealed class TextNode: Control, FlowComponentOrNode {
		internal readonly Func<string> TextGetter;

		public TextNode( Func<string> textGetter ) {
			TextGetter = textGetter;
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		protected override void Render( HtmlTextWriter writer ) {
			var text = TextGetter();
			if( text.Length > 0 )
				writer.Write( HttpUtility.HtmlEncode( text ) );
		}
	}
}