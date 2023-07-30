#nullable disable
using System;
using System.Collections.Generic;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a checkbox list.
	/// </summary>
	public static class CheckboxListSetup {
		/// <summary>
		/// Creates a setup object for a checkbox list.
		/// </summary>
		/// <param name="items">The items in the list.</param>
		/// <param name="displaySetup"></param>
		/// <param name="includeSelectAndDeselectAllButtons"></param>
		/// <param name="minColumnWidth">The minimum width of each column in the list. Pass null to force a single column.</param>
		/// <param name="action">The action that will occur when the user hits Enter on any of the checkboxes. Pass null to use the current default action.</param>
		/// <param name="selectionChangedAction">The action that will occur when the selection is changed. Pass null for no action.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static CheckboxListSetup<ItemIdType> Create<ItemIdType>(
			IEnumerable<SelectListItem<ItemIdType>> items, DisplaySetup displaySetup = null, bool includeSelectAndDeselectAllButtons = false,
			ContentBasedLength minColumnWidth = null, SpecifiedValue<FormAction> action = null, FormAction selectionChangedAction = null,
			Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			return new CheckboxListSetup<ItemIdType>(
				displaySetup,
				includeSelectAndDeselectAllButtons,
				items,
				minColumnWidth,
				action,
				selectionChangedAction,
				validationPredicate,
				validationErrorNotifier );
		}
	}

	/// <summary>
	/// The configuration for a checkbox list.
	/// </summary>
	public class CheckboxListSetup<ItemIdType> {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly bool IncludeSelectAndDeselectAllButtons;
		internal readonly IReadOnlyCollection<SelectListItem<ItemIdType>> Items;
		internal readonly ContentBasedLength MinColumnWidth;
		internal readonly FormAction Action;
		internal readonly FormAction SelectionChangedAction;
		internal readonly Func<bool, bool> ValidationPredicate;
		internal readonly Action ValidationErrorNotifier;

		internal CheckboxListSetup(
			DisplaySetup displaySetup, bool includeSelectAndDeselectAllButtons, IEnumerable<SelectListItem<ItemIdType>> items, ContentBasedLength minColumnWidth,
			SpecifiedValue<FormAction> action, FormAction selectionChangedAction, Func<bool, bool> validationPredicate, Action validationErrorNotifier ) {
			DisplaySetup = displaySetup;
			IncludeSelectAndDeselectAllButtons = includeSelectAndDeselectAllButtons;
			Items = items.Materialize();
			MinColumnWidth = minColumnWidth;
			Action = action != null ? action.Value : FormState.Current.FormControlDefaultAction;
			SelectionChangedAction = selectionChangedAction;
			ValidationPredicate = validationPredicate;
			ValidationErrorNotifier = validationErrorNotifier;
		}
	}
}