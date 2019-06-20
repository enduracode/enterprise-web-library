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
		/// <param name="useHorizontalLayout">Pass true if you want the radio buttons to be laid out horizontally instead of vertically.</param>
		/// <param name="unlistedSelectedItemLabelGetter">A function that will be called if the selected item ID does not match any list item and is not the default
		/// value of the type. The function takes the selected item ID and returns the label of the unlisted selected item, which will appear before all other
		/// items in the list. The string " (invalid)" will be appended to the label.</param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		/// <param name="action">The action that will occur when the user hits Enter on a radio button. Pass null to use the current default action.</param>
		/// <param name="selectionChangedAction">The action that will occur when the selection is changed. Pass null for no action.</param>
		/// <param name="itemIdPageModificationValue"></param>
		/// <param name="itemMatchPageModificationSetups"></param>
		public static RadioListSetup<ItemIdType> Create<ItemIdType>(
			IEnumerable<SelectListItem<ItemIdType>> items, bool useHorizontalLayout = false, Func<ItemIdType, string> unlistedSelectedItemLabelGetter = null,
			bool disableSingleButtonDetection = false, FormAction action = null, FormAction selectionChangedAction = null,
			PageModificationValue<ItemIdType> itemIdPageModificationValue = null,
			IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups = null ) =>
			new RadioListSetup<ItemIdType>(
				useHorizontalLayout,
				false,
				unlistedSelectedItemLabelGetter,
				items,
				FreeFormRadioListSetup.Create(
					disableSingleButtonDetection: disableSingleButtonDetection,
					selectionChangedAction: selectionChangedAction,
					itemIdPageModificationValue: itemIdPageModificationValue,
					itemMatchPageModificationSetups: itemMatchPageModificationSetups ),
				action );

		/// <summary>
		/// Creates a setup object for a read-only radio list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="useHorizontalLayout">Pass true if you want the radio buttons to be laid out horizontally instead of vertically.</param>
		/// <param name="unlistedSelectedItemLabelGetter">A function that will be called if the selected item ID does not match any list item and is not the default
		/// value of the type. The function takes the selected item ID and returns the label of the unlisted selected item, which will appear before all other
		/// items in the list. The string " (invalid)" will be appended to the label.</param>
		public static RadioListSetup<ItemIdType> CreateReadOnly<ItemIdType>(
			IEnumerable<SelectListItem<ItemIdType>> items, bool useHorizontalLayout = false, Func<ItemIdType, string> unlistedSelectedItemLabelGetter = null ) =>
			new RadioListSetup<ItemIdType>( useHorizontalLayout, true, unlistedSelectedItemLabelGetter, items, FreeFormRadioListSetup.Create<ItemIdType>(), null );
	}

	/// <summary>
	/// The configuration for a radio list.
	/// </summary>
	public class RadioListSetup<ItemIdType> {
		internal readonly bool UseHorizontalLayout;
		internal readonly bool IsReadOnly;
		internal readonly Func<ItemIdType, string> UnlistedSelectedItemLabelGetter;
		internal readonly IEnumerable<SelectListItem<ItemIdType>> Items;
		internal readonly FreeFormRadioListSetup<ItemIdType> FreeFormSetup;
		internal readonly FormAction Action;

		internal RadioListSetup(
			bool useHorizontalLayout, bool isReadOnly, Func<ItemIdType, string> unlistedSelectedItemLabelGetter, IEnumerable<SelectListItem<ItemIdType>> items,
			FreeFormRadioListSetup<ItemIdType> freeFormSetup, FormAction action ) {
			UseHorizontalLayout = useHorizontalLayout;
			IsReadOnly = isReadOnly;
			UnlistedSelectedItemLabelGetter = unlistedSelectedItemLabelGetter;
			Items = items;
			FreeFormSetup = freeFormSetup;
			Action = action ?? FormState.Current.DefaultAction;
		}
	}
}