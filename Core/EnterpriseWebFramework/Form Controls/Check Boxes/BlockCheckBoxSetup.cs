using System;
using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a block-level check box.
	/// </summary>
	public class BlockCheckBoxSetup {
		internal readonly bool HighlightedWhenChecked;
		internal readonly FormAction Action;
		internal readonly bool TriggersPostBackWhenCheckedOrUnchecked;
		internal readonly Func<IEnumerable<Control>> NestedControlListGetter;
		internal readonly bool NestedControlsAlwaysVisible;

		/// <summary>
		/// Creates a check-box setup object.
		/// </summary>
		/// <param name="highlightedWhenChecked"></param>
		/// <param name="action">The action that will occur when the user hits Enter on the check box.</param>
		/// <param name="triggersActionWhenCheckedOrUnchecked">Pass true if you want an action to occur when the box is checked or unchecked.</param>
		/// <param name="nestedControlListGetter">A function that gets the controls that will appear beneath the check box's label only when the box is checked.
		/// </param>
		/// <param name="nestedControlsAlwaysVisible">Pass true to force the nested controls, if any exist, to be always visible instead of only visible when the
		/// box is checked.</param>
		public BlockCheckBoxSetup(
			bool highlightedWhenChecked = false, FormAction action = null, bool triggersActionWhenCheckedOrUnchecked = false,
			Func<IEnumerable<Control>> nestedControlListGetter = null, bool nestedControlsAlwaysVisible = false ) {
			HighlightedWhenChecked = highlightedWhenChecked;
			Action = action;
			TriggersPostBackWhenCheckedOrUnchecked = triggersActionWhenCheckedOrUnchecked;
			NestedControlListGetter = nestedControlListGetter;
			NestedControlsAlwaysVisible = nestedControlsAlwaysVisible;
		}
	}
}