using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a wrapping list.
	/// </summary>
	public class WrappingListItem {
		public static implicit operator WrappingListItem( ComponentListItem item ) {
			return new WrappingListItem( item );
		}

		internal readonly Func<Tuple<ComponentListItem, FlowComponentOrNode>> ItemAndComponentGetter;

		/// <summary>
		/// Creates a list item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="verticalAlignment">The vertical alignment of the item.</param>
		public WrappingListItem( ComponentListItem item, FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified ) {
			ItemAndComponentGetter = () => item.GetItemAndComponent( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ) );
		}
	}
}