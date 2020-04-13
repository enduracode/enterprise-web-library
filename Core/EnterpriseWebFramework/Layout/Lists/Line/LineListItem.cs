using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a line list.
	/// </summary>
	public class LineListItem {
		public static implicit operator LineListItem( ComponentListItem item ) => item.ToLineListItem();

		internal readonly Func<Tuple<ComponentListItem, FlowComponentOrNode>> ItemAndComponentGetter;

		internal LineListItem( Func<Tuple<ComponentListItem, FlowComponentOrNode>> itemAndComponentGetter ) {
			ItemAndComponentGetter = itemAndComponentGetter;
		}
	}

	public static class LineListItemExtensionCreators {
		/// <summary>
		/// Creates a line-list-item collection containing only this general list item.
		/// </summary>
		/// <param name="item"></param>
		public static IReadOnlyCollection<LineListItem> ToLineListItemCollection( this ComponentListItem item ) => ( (LineListItem)item ).ToCollection();

		/// <summary>
		/// Creates a line-list item from this general list item. If you don't need to pass any arguments, don't use this method; general list items are implicitly
		/// converted to line-list items.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="verticalAlignment">The vertical alignment of the item.</param>
		/// <param name="width">The width of the item.</param>
		public static LineListItem ToLineListItem(
			this ComponentListItem item, FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified, ContentBasedLength width = null ) =>
			new LineListItem( () => item.GetItemAndComponent( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ), width ) );
	}
}