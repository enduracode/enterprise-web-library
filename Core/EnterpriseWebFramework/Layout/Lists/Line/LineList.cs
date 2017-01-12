using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class LineList: FlowComponent {
		private readonly IReadOnlyCollection<FlowComponentOrNode> children;

		/// <summary>
		/// Creates a line list.
		/// </summary>
		/// <param name="items">The items. Do not pass null.</param>
		/// <param name="generalSetup">The setup object for the list.</param>
		/// <param name="alignment">The horizontal alignment of the items in the list.</param>
		/// <param name="verticalAlignment">The default vertical alignment of the items in the list.</param>
		public LineList(
			IEnumerable<ComponentListItem> items, ComponentListSetup generalSetup = null, FlexboxAlignment alignment = FlexboxAlignment.NotSpecified,
			FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified ) {
			children =
				( generalSetup ?? new ComponentListSetup() ).GetComponents(
					CssElementCreator.LineListClass.Union( FlexboxAlignmentStatics.Class( alignment ) ).Union( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ) ),
					items );
		}

		IEnumerable<FlowComponentOrNode> FlowComponent.GetChildren() {
			return children;
		}
	}
}