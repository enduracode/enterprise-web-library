using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item group in a column primary table.
	/// </summary>
	public class ColumnPrimaryItemGroup {
		private readonly IReadOnlyCollection<FlowComponent> groupName;
		private readonly IReadOnlyCollection<ActionComponentSetup> groupActions;
		private readonly ElementActivationBehavior groupHeadActivationBehavior;
		internal readonly ReadOnlyCollection<EwfTableItem> Items;

		/// <summary>
		/// Creates an item group.
		/// </summary>
		/// <param name="groupName">The name of the group and any other information you want in the group head.</param>
		/// <param name="groupActions">Group action components.</param>
		/// <param name="groupHeadActivationBehavior">The activation behavior for the group head</param>
		/// <param name="items">The items</param>
		public ColumnPrimaryItemGroup(
			IReadOnlyCollection<FlowComponent> groupName, IReadOnlyCollection<ActionComponentSetup> groupActions = null,
			ElementActivationBehavior groupHeadActivationBehavior = null, IEnumerable<EwfTableItem> items = null ) {
			this.groupName = groupName ?? Enumerable.Empty<FlowComponent>().Materialize();
			this.groupActions = groupActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();
			this.groupHeadActivationBehavior = groupHeadActivationBehavior;
			Items = ( items ?? new EwfTableItem[ 0 ] ).ToList().AsReadOnly();
		}

		internal IReadOnlyCollection<FlowComponent> GetHeadCellContent() {
			// NOTE: Group-level item actions should go in here.
			if( !groupName.Any() )
				return Enumerable.Empty<FlowComponent>().Materialize();
			return new GenericFlowContainer(
				new GenericFlowContainer( groupName ).Concat( TableStatics.GetGeneralActionList( null, groupActions ) ).Materialize(),
				classes: TableCssElementCreator.ItemGroupNameAndGeneralActionContainerClass ).ToCollection();
		}
	}
}