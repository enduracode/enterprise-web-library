using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal sealed class TextNode: FlowComponentOrNode {
		internal readonly Func<string> TextGetter;

		public TextNode( Func<string> textGetter ) {
			TextGetter = textGetter;
		}
	}
}