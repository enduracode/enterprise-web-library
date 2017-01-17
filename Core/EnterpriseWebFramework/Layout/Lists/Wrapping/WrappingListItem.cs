using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a wrapping list.
	/// </summary>
	public class WrappingListItem {
		public static implicit operator WrappingListItem( ComponentListItem item ) {
			return item.ToWrappingListItem();
		}

		internal readonly Func<Tuple<ComponentListItem, FlowComponentOrNode>> ItemAndComponentGetter;

		internal WrappingListItem( ComponentListItem item, FlexboxVerticalAlignment verticalAlignment ) {
			ItemAndComponentGetter = () => item.GetItemAndComponent( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ) );
		}
	}

	public static class WrappingListItemExtensionCreators {
		/// <summary>
		/// Creates a wrapping-list item from this general list item. If you don't need to pass any arguments, don't use this method; general list items are
		/// implicitly converted to wrapping-list items.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="verticalAlignment">The vertical alignment of the item.</param>
		public static WrappingListItem ToWrappingListItem(
			this ComponentListItem item, FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified ) {
			return new WrappingListItem( item, verticalAlignment );
		}
	}
}