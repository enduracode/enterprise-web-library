using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A displayable element.
	/// </summary>
	public class DisplayableElement: FlowComponent, EtherealComponent {
		private readonly IReadOnlyCollection<ElementComponent> children;

		/// <summary>
		/// Creates a displayable element.
		/// </summary>
		public DisplayableElement( Func<ElementContext, DisplayableElementData> elementDataGetter, FormValue formValue = null ) {
			children = new ElementComponent( context => elementDataGetter( context ).BaseDataGetter( context ), formValue: formValue ).ToCollection();
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}

		IEnumerable<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}