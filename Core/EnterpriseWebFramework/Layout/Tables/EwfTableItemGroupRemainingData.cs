using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Lazy loaded data used by an EWF table item group.
	/// </summary>
	public class EwfTableItemGroupRemainingData {
		internal readonly Control GroupName;
		internal readonly ReadOnlyCollection<Tuple<string, Action>> GroupActions;
		internal readonly ClickScript GroupHeadClickScript;
		internal readonly bool? InitiallyCollapsed;
		internal readonly IReadOnlyCollection<TailUpdateRegion> TailUpdateRegions;

		/// <summary>
		/// Creates a remaining data object.
		/// </summary>
		/// <param name="groupName">A control that contains the name of the group and any other information you want in the group head</param>
		/// <param name="groupActions">Group action buttons</param>
		/// <param name="groupHeadClickScript">The click script for the group head</param>
		/// <param name="initiallyCollapsed">Whether the group is initially collapsed. Null means the group cannot be collapsed and is always visible.</param>
		/// <param name="tailUpdateRegions">The tail update regions for the group</param>
		public EwfTableItemGroupRemainingData(
			Control groupName, IEnumerable<Tuple<string, Action>> groupActions = null, ClickScript groupHeadClickScript = null, bool? initiallyCollapsed = null,
			IEnumerable<TailUpdateRegion> tailUpdateRegions = null ) {
			GroupName = groupName;
			GroupActions = ( groupActions ?? new Tuple<string, Action>[ 0 ] ).ToList().AsReadOnly();
			GroupHeadClickScript = groupHeadClickScript;
			InitiallyCollapsed = initiallyCollapsed;
			TailUpdateRegions = tailUpdateRegions != null ? tailUpdateRegions.ToImmutableArray() : ImmutableArray<TailUpdateRegion>.Empty;
		}
	}
}