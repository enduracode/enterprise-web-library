#nullable disable
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class InlineList: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates an inline list, which separates items with a pipe character.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
		/// <param name="setup">The setup object for the list.</param>
		public InlineList( IEnumerable<ComponentListItem> items, ComponentListSetup setup = null ) {
			children = ( setup ?? new ComponentListSetup() ).GetComponents(
				CssElementCreator.InlineListClass,
				from i in items select i.GetItemAndComponent( ElementClassSet.Empty, null, includeContentContainer: true ) );
		}

		IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}