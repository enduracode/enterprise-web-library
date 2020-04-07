using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Lazy loaded data used by an EWF table item group.
	/// </summary>
	public class EwfTableItemGroupRemainingData {
		internal readonly IReadOnlyCollection<FlowComponent> GroupName;
		internal readonly ReadOnlyCollection<Tuple<string, Action>> GroupActions;
		internal readonly ElementActivationBehavior GroupHeadActivationBehavior;
		internal readonly bool? InitiallyCollapsed;
		internal readonly IReadOnlyCollection<TailUpdateRegion> TailUpdateRegions;

		/// <summary>
		/// Creates a remaining data object.
		/// </summary>
		/// <param name="groupName">The name of the group and any other information you want in the group head.</param>
		/// <param name="groupActions">Group action buttons</param>
		/// <param name="groupHeadActivationBehavior">The activation behavior for the group head</param>
		/// <param name="initiallyCollapsed">Whether the group is initially collapsed. Null means the group cannot be collapsed and is always visible.</param>
		/// <param name="tailUpdateRegions">The tail update regions for the group. If a table uses item limiting, these regions will include all subsequent item
		/// groups in the table. This is necessary because any number of items could be appended to this item group, potentially causing subsequent item groups to
		/// become invisible.</param>
		public EwfTableItemGroupRemainingData(
			IReadOnlyCollection<FlowComponent> groupName, IEnumerable<Tuple<string, Action>> groupActions = null,
			ElementActivationBehavior groupHeadActivationBehavior = null, bool? initiallyCollapsed = null,
			IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions = null ) {
			GroupName = groupName ?? Enumerable.Empty<FlowComponent>().Materialize();
			GroupActions = ( groupActions ?? new Tuple<string, Action>[ 0 ] ).ToList().AsReadOnly();
			GroupHeadActivationBehavior = groupHeadActivationBehavior;
			InitiallyCollapsed = initiallyCollapsed;
			TailUpdateRegions = tailUpdateRegions ?? Enumerable.Empty<TailUpdateRegion>().Materialize();
		}
	}
}