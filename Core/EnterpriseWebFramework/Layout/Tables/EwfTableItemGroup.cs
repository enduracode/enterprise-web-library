using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item group in an EWF table.
	/// </summary>
	public class EwfTableItemGroup: EwfTableItemGroup<int> {
		internal static Lazy<EwfTableItem<IdType>> GetItemLazy<IdType>( Func<EwfTableItem<IdType>> item ) =>
			new Lazy<EwfTableItem<IdType>>( item, LazyThreadSafetyMode.None );

		/// <summary>
		/// Creates an item group.
		/// </summary>
		/// <param name="remainingDataGetter"></param>
		/// <param name="items"></param>
		/// <param name="selectedItemActions">Group selected-item actions. Passing one or more of these will add a new column to the table containing a checkbox for
		/// each item with an ID, within this group.</param>
		public static EwfTableItemGroup Create(
			Func<EwfTableItemGroupRemainingData> remainingDataGetter, IEnumerable<Func<EwfTableItem>> items,
			IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions = null ) =>
			new EwfTableItemGroup( remainingDataGetter, selectedItemActions, items );

		/// <summary>
		/// Creates an item group with a specified item ID type.
		/// </summary>
		/// <param name="remainingDataGetter"></param>
		/// <param name="items"></param>
		/// <param name="selectedItemActions">Group selected-item actions. Passing one or more of these will add a new column to the table containing a checkbox for
		/// each item with an ID, within this group.</param>
		public static EwfTableItemGroup<ItemIdType> CreateWithItemIdType<ItemIdType>(
			Func<EwfTableItemGroupRemainingData> remainingDataGetter, IEnumerable<Func<EwfTableItem<ItemIdType>>> items,
			IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions = null ) =>
			new EwfTableItemGroup<ItemIdType>( remainingDataGetter, selectedItemActions, items );

		private EwfTableItemGroup(
			Func<EwfTableItemGroupRemainingData> remainingDataGetter, IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions,
			IEnumerable<Func<EwfTableItem>> items ): base( remainingDataGetter, selectedItemActions, items ) {}
	}

	/// <summary>
	/// An item group in an EWF table.
	/// </summary>
	public class EwfTableItemGroup<ItemIdType> {
		internal readonly Lazy<EwfTableItemGroupRemainingData> RemainingData;
		internal readonly IReadOnlyCollection<SelectedItemAction<ItemIdType>> SelectedItemActions;
		internal readonly List<Lazy<EwfTableItem<ItemIdType>>> Items;

		internal EwfTableItemGroup(
			Func<EwfTableItemGroupRemainingData> remainingDataGetter, IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions,
			IEnumerable<Func<EwfTableItem<ItemIdType>>> items ) {
			RemainingData = new Lazy<EwfTableItemGroupRemainingData>( remainingDataGetter );
			SelectedItemActions = selectedItemActions ?? Enumerable.Empty<SelectedItemAction<ItemIdType>>().Materialize();
			Items = items.Select( EwfTableItemGroup.GetItemLazy ).ToList();
		}

		internal IReadOnlyCollection<EwfTableItem> GetHeadItem( int fieldCount ) {
			// NOTE: If group is collapsible, set up item-count display and "click to expand" button.
			// NOTE: The item-count display should be wrapped in a NamingPlaceholder that is part of all tail update regions for this group.
			if( !RemainingData.Value.GroupName.Any() )
				return Enumerable.Empty<EwfTableItem>().Materialize();
			return EwfTableItem.Create(
					EwfTableItemSetup.Create( activationBehavior: RemainingData.Value.GroupHeadActivationBehavior ),
					new GenericFlowContainer(
						new GenericFlowContainer( RemainingData.Value.GroupName ).Concat( TableStatics.GetGeneralActionList( null, RemainingData.Value.GroupActions ) )
							.Materialize(),
						classes: TableCssElementCreator.ItemGroupNameAndGeneralActionContainerClass ).ToCell( new TableCellSetup( fieldSpan: fieldCount ) ) )
				.ToCollection();
		}
	}
}