using System;
using System.Collections.Generic;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a wrapping list.
	/// </summary>
	public class WrappingListItem {
		public static implicit operator WrappingListItem( ComponentListItem item ) => item.ToWrappingListItem();

		internal readonly Func<Tuple<ComponentListItem, FlowComponentOrNode>> ItemAndComponentGetter;

		internal WrappingListItem( Func<Tuple<ComponentListItem, FlowComponentOrNode>> itemAndComponentGetter ) {
			ItemAndComponentGetter = itemAndComponentGetter;
		}
	}

	public static class WrappingListItemExtensionCreators {
		/// <summary>
		/// Creates a wrapping-list-item collection containing only this general list item.
		/// </summary>
		/// <param name="item"></param>
		public static IReadOnlyCollection<WrappingListItem> ToWrappingListItemCollection( this ComponentListItem item ) =>
			( (WrappingListItem)item ).ToCollection();

		/// <summary>
		/// Concatenates wrapping-list items.
		/// </summary>
		public static IEnumerable<WrappingListItem> ConcatWrappingListItems( this ComponentListItem item, IEnumerable<WrappingListItem> wrappingListItems ) =>
			( (WrappingListItem)item ).Concat( wrappingListItems );

		/// <summary>
		/// Returns a sequence of two wrapping-list items.
		/// </summary>
		public static IEnumerable<WrappingListItem> AppendWrappingListItem( this ComponentListItem item, WrappingListItem wrappingListItem ) =>
			( (WrappingListItem)item ).Append( wrappingListItem );

		/// <summary>
		/// Creates a wrapping-list item from this general list item. If you don't need to pass any arguments, don't use this method; general list items are
		/// implicitly converted to wrapping-list items.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="verticalAlignment">The vertical alignment of the item.</param>
		/// <param name="width">The width of the item.</param>
		public static WrappingListItem ToWrappingListItem(
			this ComponentListItem item, FlexboxVerticalAlignment verticalAlignment = FlexboxVerticalAlignment.NotSpecified, ContentBasedLength width = null ) =>
			new WrappingListItem( () => item.GetItemAndComponent( FlexboxVerticalAlignmentStatics.Class( verticalAlignment ), width ) );

		/// <summary>
		/// Concatenates wrapping-list items.
		/// </summary>
		public static IEnumerable<WrappingListItem> Concat( this WrappingListItem first, IEnumerable<WrappingListItem> second ) => second.Prepend( first );

		/// <summary>
		/// Returns a sequence of two wrapping-list items.
		/// </summary>
		public static IEnumerable<WrappingListItem> Append( this WrappingListItem first, WrappingListItem second ) =>
			Enumerable.Empty<WrappingListItem>().Append( first ).Append( second );
	}
}