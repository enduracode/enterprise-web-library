using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class TextNode: Control, PageNode, FlowComponent {
		internal readonly Func<string> TextGetter;

		public TextNode( Func<string> textGetter ) {
			TextGetter = textGetter;
		}

		IEnumerable<PageNode> FlowComponent.GetNodes() {
			return this.ToSingleElementArray();
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		protected override void Render( HtmlTextWriter writer ) {
			var text = TextGetter();
			if( text.Length > 0 )
				writer.Write( HttpUtility.HtmlEncode( text ) );
		}
	}
}