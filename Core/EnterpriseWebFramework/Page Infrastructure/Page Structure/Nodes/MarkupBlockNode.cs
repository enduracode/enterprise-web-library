#nullable disable
using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal sealed class MarkupBlockNode: FlowComponentOrNode {
		internal readonly Func<string> MarkupGetter;

		public MarkupBlockNode( Func<string> markupGetter ) {
			MarkupGetter = markupGetter;
		}
	}
}