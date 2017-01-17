using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a line list.
	/// </summary>
	public class LineListItem {
		public static implicit operator LineListItem( ComponentListItem item ) {
			return item.ToLineListItem();
		}

		internal readonly Func<Tuple<ComponentListItem, FlowComponentOrNode>> ItemAndComponentGetter;

		internal LineListItem( ComponentListItem item, FlexboxVerticalAlignment verticalAlignment ) {
			ItemAndComponentGetter = () => item.GetItemAndComponent( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ) );
		}
	}

	public static class LineListItemExtensionCreators {
		/// <summary>
		/// Creates a line-list item from this general list item. If you don't need to pass any arguments, don't use this method; general list items are implicitly
		/// converted to line-list items.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="verticalAlignment">The vertical alignment of the item.</param>
		public static LineListItem ToLineListItem( this ComponentListItem item, FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified ) {
			return new LineListItem( item, verticalAlignment );
		}
	}
}