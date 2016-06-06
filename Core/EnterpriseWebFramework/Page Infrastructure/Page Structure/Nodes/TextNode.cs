using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class TextNode: PageNode, FlowComponent {
		internal readonly string Text;

		public TextNode( string text ) {
			Text = text;
		}

		IEnumerable<PageNode> FlowComponent.GetNodes() {
			return this.ToSingleElementArray();
		}
	}
}