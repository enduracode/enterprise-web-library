using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a flow checkbox.
	/// </summary>
	public class FlowCheckboxSetup {
		/// <summary>
		/// Creates a setup object for a standard checkbox.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the container.</param>
		/// <param name="highlightedWhenChecked"></param>
		/// <param name="action">The action that will occur when the user hits Enter on the checkbox. Pass null to use the current default action.</param>
		/// <param name="valueChangedAction">The action that will occur when the checkbox value is changed. Pass null for no action.</param>
		/// <param name="pageModificationValue"></param>
		/// <param name="nestedContentGetter">A function that gets the content that will appear beneath the checkbox.</param>
		/// <param name="nestedContentAlwaysDisplayed">Pass true to force the nested content to always be displayed instead of only when the box is checked.</param>
		public static FlowCheckboxSetup Create(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, bool highlightedWhenChecked = false, SpecifiedValue<FormAction> action = null,
			FormAction valueChangedAction = null, PageModificationValue<bool> pageModificationValue = null,
			Func<IReadOnlyCollection<FlowComponent>> nestedContentGetter = null, bool nestedContentAlwaysDisplayed = false ) {
			return new FlowCheckboxSetup(
				displaySetup,
				classes,
				CheckboxSetup.Create( action: action, valueChangedAction: valueChangedAction, pageModificationValue: pageModificationValue ),
				highlightedWhenChecked,
				nestedContentGetter,
				nestedContentAlwaysDisplayed );
		}

		/// <summary>
		/// Creates a setup object for a read-only checkbox.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the container.</param>
		/// <param name="highlightedWhenChecked"></param>
		/// <param name="nestedContentGetter">A function that gets the content that will appear beneath the checkbox.</param>
		/// <param name="nestedContentAlwaysDisplayed">Pass true to force the nested content to always be displayed instead of only when the box is checked.</param>
		public static FlowCheckboxSetup CreateReadOnly(
			DisplaySetup displaySetup = null, ElementClassSet classes = null, bool highlightedWhenChecked = false,
			Func<IReadOnlyCollection<FlowComponent>> nestedContentGetter = null, bool nestedContentAlwaysDisplayed = false ) {
			return new FlowCheckboxSetup(
				displaySetup,
				classes,
				CheckboxSetup.CreateReadOnly(),
				highlightedWhenChecked,
				nestedContentGetter,
				nestedContentAlwaysDisplayed );
		}

		internal readonly DisplaySetup DisplaySetup;
		internal readonly ElementClassSet Classes;
		internal readonly CheckboxSetup CheckboxSetup;
		internal readonly bool HighlightedWhenChecked;
		internal readonly Func<IReadOnlyCollection<FlowComponent>> NestedContentGetter;
		internal readonly bool NestedContentAlwaysDisplayed;

		private FlowCheckboxSetup(
			DisplaySetup displaySetup, ElementClassSet classes, CheckboxSetup checkboxSetup, bool highlightedWhenChecked,
			Func<IReadOnlyCollection<FlowComponent>> nestedContentGetter, bool nestedContentAlwaysDisplayed ) {
			DisplaySetup = displaySetup;
			Classes = classes;
			CheckboxSetup = checkboxSetup;
			HighlightedWhenChecked = highlightedWhenChecked;
			NestedContentGetter = nestedContentGetter;
			NestedContentAlwaysDisplayed = nestedContentAlwaysDisplayed;
		}
	}
}