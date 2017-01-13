using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class StackList: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a stack list.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
		/// <param name="setup">The setup object for the list.</param>
		public StackList( IEnumerable<ComponentListItem> items, ComponentListSetup setup = null ) {
			children = ( setup ?? new ComponentListSetup() ).GetComponents(
				CssElementCreator.StackListClass,
				from i in items select i.GetItemAndComponent( ElementClassSet.Empty ) );
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}