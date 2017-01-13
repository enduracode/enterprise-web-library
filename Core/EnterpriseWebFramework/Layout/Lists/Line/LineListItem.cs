using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a line list.
	/// </summary>
	public class LineListItem {
		public static implicit operator LineListItem( ComponentListItem item ) {
			return new LineListItem( item );
		}

		internal readonly Func<Tuple<ComponentListItem, FlowComponentOrNode>> ItemAndComponentGetter;

		/// <summary>
		/// Creates a list item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="verticalAlignment">The vertical alignment of the item.</param>
		public LineListItem( ComponentListItem item, FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified ) {
			ItemAndComponentGetter = () => item.GetItemAndComponent( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ) );
		}
	}
}