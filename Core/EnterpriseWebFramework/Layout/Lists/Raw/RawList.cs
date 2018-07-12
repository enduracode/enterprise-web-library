using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class RawList: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a raw list.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
		/// <param name="setup">The setup object for the list.</param>
		public RawList( IEnumerable<ComponentListItem> items, ComponentListSetup setup = null ) {
			children = ( setup ?? new ComponentListSetup() ).GetComponents(
				ElementClassSet.Empty,
				from i in items select i.GetItemAndComponent( ElementClassSet.Empty, null ) );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}