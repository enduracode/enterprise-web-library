using System;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal sealed class MarkupBlockNode: Control, FlowComponentOrNode {
		internal readonly Func<string> MarkupGetter;

		public MarkupBlockNode( Func<string> markupGetter ) {
			MarkupGetter = markupGetter;
		}

		// Web Forms compatibility. Remove when EnduraCode goal 790 is complete.
		protected override void Render( HtmlTextWriter writer ) {
			var markup = MarkupGetter();
			if( markup.Length > 0 )
				writer.Write( markup );
		}
	}
}