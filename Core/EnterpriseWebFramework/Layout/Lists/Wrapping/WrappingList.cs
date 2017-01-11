using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class WrappingList: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a wrapping list.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
		/// <param name="setup">The setup object for the list.</param>
		public WrappingList( IEnumerable<ComponentListItem> items, ComponentListSetup setup = null ) {
			children = ( setup ?? new ComponentListSetup() ).GetComponents( CssElementCreator.WrappingListClass, items );
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}