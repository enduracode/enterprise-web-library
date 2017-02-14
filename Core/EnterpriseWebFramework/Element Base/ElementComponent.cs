using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A page element.
	/// </summary>
	public class ElementComponent: FlowComponent, EtherealComponent {
		private readonly IReadOnlyCollection<ElementNode> children;

		/// <summary>
		/// Creates an element.
		/// </summary>
		public ElementComponent( Func<ElementContext, ElementData> elementDataGetter, FormValue formValue = null ) {
			children = new ElementNode( context => elementDataGetter( context ).NodeDataGetter( context ), formValue: formValue ).ToCollection();
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}

		IEnumerable<EtherealComponentOrElement> EtherealComponent.GetChildren() {
			return children;
		}
	}
}