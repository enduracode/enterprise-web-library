using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item group in a column primary table.
	/// </summary>
	public class ColumnPrimaryItemGroup: ColumnPrimaryItemGroup<int> {
		/// <summary>
		/// Creates an item group.
		/// </summary>
		/// <param name="groupName">The name of the group and any other information you want in the group head.</param>
		/// <param name="groupActions">Group action components.</param>
		/// <param name="groupHeadActivationBehavior">The activation behavior for the group head</param>
		/// <param name="selectedItemActions">Group selected-item actions. Passing one or more of these will add a new row to the table containing a checkbox for
		/// each item with an ID, within this group.</param>
		/// <param name="items">The items</param>
		public static ColumnPrimaryItemGroup Create(
			IReadOnlyCollection<FlowComponent> groupName, IReadOnlyCollection<ActionComponentSetup> groupActions = null,
			ElementActivationBehavior groupHeadActivationBehavior = null, IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions = null,
			IEnumerable<EwfTableItem> items = null ) =>
			new ColumnPrimaryItemGroup( groupName, groupActions, groupHeadActivationBehavior, selectedItemActions, items );

		/// <summary>
		/// Creates an item group with a specified item ID type.
		/// </summary>
		/// <param name="groupName">The name of the group and any other information you want in the group head.</param>
		/// <param name="groupActions">Group action components.</param>
		/// <param name="groupHeadActivationBehavior">The activation behavior for the group head</param>
		/// <param name="selectedItemActions">Group selected-item actions. Passing one or more of these will add a new row to the table containing a checkbox for
		/// each item with an ID, within this group.</param>
		/// <param name="items">The items</param>
		public static ColumnPrimaryItemGroup<ItemIdType> CreateWithItemIdType<ItemIdType>(
			IReadOnlyCollection<FlowComponent> groupName, IReadOnlyCollection<ActionComponentSetup> groupActions = null,
			ElementActivationBehavior groupHeadActivationBehavior = null, IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions = null,
			IEnumerable<EwfTableItem<ItemIdType>> items = null ) =>
			new ColumnPrimaryItemGroup<ItemIdType>( groupName, groupActions, groupHeadActivationBehavior, selectedItemActions, items );

		private ColumnPrimaryItemGroup(
			IReadOnlyCollection<FlowComponent> groupName, IReadOnlyCollection<ActionComponentSetup> groupActions,
			ElementActivationBehavior groupHeadActivationBehavior, IReadOnlyCollection<SelectedItemAction<int>> selectedItemActions, IEnumerable<EwfTableItem> items )
			: base( groupName, groupActions, groupHeadActivationBehavior, selectedItemActions, items ) {}
	}

	/// <summary>
	/// An item group in a column primary table.
	/// </summary>
	public class ColumnPrimaryItemGroup<ItemIdType> {
		private readonly IReadOnlyCollection<FlowComponent> groupName;
		private readonly IReadOnlyCollection<ActionComponentSetup> groupActions;
		private readonly ElementActivationBehavior groupHeadActivationBehavior;
		internal readonly IReadOnlyCollection<SelectedItemAction<ItemIdType>> SelectedItemActions;
		internal readonly List<EwfTableItem<ItemIdType>> Items;

		internal ColumnPrimaryItemGroup(
			IReadOnlyCollection<FlowComponent> groupName, IReadOnlyCollection<ActionComponentSetup> groupActions,
			ElementActivationBehavior groupHeadActivationBehavior, IReadOnlyCollection<SelectedItemAction<ItemIdType>> selectedItemActions,
			IEnumerable<EwfTableItem<ItemIdType>> items ) {
			this.groupName = groupName ?? Enumerable.Empty<FlowComponent>().Materialize();
			this.groupActions = groupActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();
			this.groupHeadActivationBehavior = groupHeadActivationBehavior;
			SelectedItemActions = selectedItemActions ?? Enumerable.Empty<SelectedItemAction<ItemIdType>>().Materialize();
			Items = ( items ?? Enumerable.Empty<EwfTableItem<ItemIdType>>() ).ToList();
		}

		internal IReadOnlyCollection<FlowComponent> GetHeadCellContent() {
			if( !groupName.Any() )
				return Enumerable.Empty<FlowComponent>().Materialize();
			return new GenericFlowContainer(
				new GenericFlowContainer( groupName ).Concat( TableStatics.GetGeneralActionList( null, groupActions ) ).Materialize(),
				classes: TableCssElementCreator.ItemGroupNameAndGeneralActionContainerClass ).ToCollection();
		}
	}
}