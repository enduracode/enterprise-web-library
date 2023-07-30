#nullable disable
using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal sealed class ElementNode: FlowComponentOrNode, EtherealComponentOrElement {
		internal readonly Func<ElementContext, ElementNodeData> ElementDataGetter;
		internal readonly FormValue FormValue;

		public ElementNode( Func<ElementContext, ElementNodeData> elementDataGetter, FormValue formValue = null ) {
			ElementDataGetter = elementDataGetter;
			FormValue = formValue;
		}
	}
}