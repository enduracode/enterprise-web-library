using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a radio list.
	/// </summary>
	public static class RadioListSetup {
		/// <summary>
		/// Creates a setup object for a standard radio list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="displaySetup"></param>
		/// <param name="useHorizontalLayout">Pass true if you want the radio buttons to be laid out horizontally instead of vertically.</param>
		/// <param name="classes">The classes on the list container.</param>
		/// <param name="unlistedSelectedItemLabelGetter">A function that will be called if the selected item ID does not match any list item and is not the default
		/// value of the type. The function takes the selected item ID and returns the label of the unlisted selected item, which will appear before all other
		/// items in the list. The string " (invalid)" will be appended to the label.</param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		/// <param name="action">The action that will occur when the user hits Enter on a radio button. Pass null to use the current default action.</param>
		/// <param name="selectionChangedAction">The action that will occur when the selection is changed. Pass null for no action.</param>
		/// <param name="itemIdPageModificationValue"></param>
		/// <param name="itemMatchPageModificationSetups"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static RadioListSetup<ItemIdType> Create<ItemIdType>(
			IEnumerable<SelectListItem<ItemIdType>> items, DisplaySetup displaySetup = null, bool useHorizontalLayout = false, ElementClassSet classes = null,
			Func<ItemIdType, string> unlistedSelectedItemLabelGetter = null, bool disableSingleButtonDetection = false, SpecifiedValue<FormAction> action = null,
			FormAction selectionChangedAction = null, PageModificationValue<ItemIdType> itemIdPageModificationValue = null,
			IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups = null, Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null ) =>
			new RadioListSetup<ItemIdType>(
				displaySetup,
				useHorizontalLayout,
				false,
				classes,
				unlistedSelectedItemLabelGetter,
				items,
				FreeFormRadioListSetup.Create(
					disableSingleButtonDetection: disableSingleButtonDetection,
					selectionChangedAction: selectionChangedAction,
					itemIdPageModificationValue: itemIdPageModificationValue,
					itemMatchPageModificationSetups: itemMatchPageModificationSetups,
					validationPredicate: validationPredicate,
					validationErrorNotifier: validationErrorNotifier ),
				action );

		/// <summary>
		/// Creates a setup object for a read-only radio list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="displaySetup"></param>
		/// <param name="useHorizontalLayout">Pass true if you want the radio buttons to be laid out horizontally instead of vertically.</param>
		/// <param name="classes">The classes on the list container.</param>
		/// <param name="unlistedSelectedItemLabelGetter">A function that will be called if the selected item ID does not match any list item and is not the default
		/// value of the type. The function takes the selected item ID and returns the label of the unlisted selected item, which will appear before all other
		/// items in the list. The string " (invalid)" will be appended to the label.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static RadioListSetup<ItemIdType> CreateReadOnly<ItemIdType>(
			IEnumerable<SelectListItem<ItemIdType>> items, DisplaySetup displaySetup = null, bool useHorizontalLayout = false, ElementClassSet classes = null,
			Func<ItemIdType, string> unlistedSelectedItemLabelGetter = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) =>
			new RadioListSetup<ItemIdType>(
				displaySetup,
				useHorizontalLayout,
				true,
				classes,
				unlistedSelectedItemLabelGetter,
				items,
				FreeFormRadioListSetup.Create<ItemIdType>( validationPredicate: validationPredicate, validationErrorNotifier: validationErrorNotifier ),
				null );
	}

	/// <summary>
	/// The configuration for a radio list.
	/// </summary>
	public class RadioListSetup<ItemIdType> {
		internal readonly DisplaySetup DisplaySetup;
		internal readonly bool UseHorizontalLayout;
		internal readonly bool IsReadOnly;
		internal readonly ElementClassSet Classes;
		internal readonly Func<ItemIdType, string> UnlistedSelectedItemLabelGetter;
		internal readonly IEnumerable<SelectListItem<ItemIdType>> Items;
		internal readonly FreeFormRadioListSetup<ItemIdType> FreeFormSetup;
		internal readonly FormAction Action;

		internal RadioListSetup(
			DisplaySetup displaySetup, bool useHorizontalLayout, bool isReadOnly, ElementClassSet classes, Func<ItemIdType, string> unlistedSelectedItemLabelGetter,
			IEnumerable<SelectListItem<ItemIdType>> items, FreeFormRadioListSetup<ItemIdType> freeFormSetup, SpecifiedValue<FormAction> action ) {
			DisplaySetup = displaySetup;
			UseHorizontalLayout = useHorizontalLayout;
			IsReadOnly = isReadOnly;
			Classes = classes;
			UnlistedSelectedItemLabelGetter = unlistedSelectedItemLabelGetter;
			Items = items;
			FreeFormSetup = freeFormSetup;
			Action = action != null ? action.Value : FormState.Current.FormControlDefaultAction;
		}
	}
}