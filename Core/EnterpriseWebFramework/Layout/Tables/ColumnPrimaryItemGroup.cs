using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item group in a column primary table.
	/// </summary>
	public class ColumnPrimaryItemGroup {
		internal readonly Control GroupName;
		internal readonly IReadOnlyCollection<ActionComponentSetup> GroupActions;
		internal readonly ElementActivationBehavior GroupHeadActivationBehavior;
		internal readonly ReadOnlyCollection<EwfTableItem> Items;

		/// <summary>
		/// Creates an item group.
		/// </summary>
		/// <param name="groupName">A control that contains the name of the group and any other information you want in the group head</param>
		/// <param name="groupActions">Group action components.</param>
		/// <param name="groupHeadActivationBehavior">The activation behavior for the group head</param>
		/// <param name="items">The items</param>
		public ColumnPrimaryItemGroup(
			Control groupName, IReadOnlyCollection<ActionComponentSetup> groupActions = null, ElementActivationBehavior groupHeadActivationBehavior = null,
			IEnumerable<EwfTableItem> items = null ) {
			GroupName = groupName;
			GroupActions = groupActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();
			GroupHeadActivationBehavior = groupHeadActivationBehavior;
			Items = ( items ?? new EwfTableItem[ 0 ] ).ToList().AsReadOnly();
		}
	}
}