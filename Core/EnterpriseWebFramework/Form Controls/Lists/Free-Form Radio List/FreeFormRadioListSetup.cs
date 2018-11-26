using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a free-form radio list.
	/// </summary>
	public static class FreeFormRadioListSetup {
		/// <summary>
		/// Creates a setup object for a free-form radio list.
		/// </summary>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		/// <param name="selectionChangedAction">The action that will occur when the selection is changed. Pass null for no action.</param>
		/// <param name="itemIdPageModificationValue"></param>
		/// <param name="itemMatchPageModificationSetups"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static FreeFormRadioListSetup<ItemIdType> Create<ItemIdType>(
			bool disableSingleButtonDetection = false, FormAction selectionChangedAction = null, PageModificationValue<ItemIdType> itemIdPageModificationValue = null,
			IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups = null, Func<bool, bool> validationPredicate = null,
			Action validationErrorNotifier = null ) {
			return new FreeFormRadioListSetup<ItemIdType>(
				disableSingleButtonDetection,
				selectionChangedAction,
				itemIdPageModificationValue,
				itemMatchPageModificationSetups,
				validationPredicate,
				validationErrorNotifier );
		}
	}

	/// <summary>
	/// The configuration for a free-form radio list.
	/// </summary>
	public class FreeFormRadioListSetup<ItemIdType> {
		internal readonly bool DisableSingleButtonDetection;
		internal readonly FormAction SelectionChangedAction;
		internal readonly PageModificationValue<ItemIdType> ItemIdPageModificationValue;
		internal readonly IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> ItemMatchPageModificationSetups;
		internal readonly Func<bool, bool> ValidationPredicate;
		internal readonly Action ValidationErrorNotifier;

		/// <summary>
		/// Creates a setup object for a free-form radio list.
		/// </summary>
		internal FreeFormRadioListSetup(
			bool disableSingleButtonDetection, FormAction selectionChangedAction, PageModificationValue<ItemIdType> itemIdPageModificationValue,
			IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups, Func<bool, bool> validationPredicate,
			Action validationErrorNotifier ) {
			DisableSingleButtonDetection = disableSingleButtonDetection;
			SelectionChangedAction = selectionChangedAction;
			ItemIdPageModificationValue = itemIdPageModificationValue;
			ItemMatchPageModificationSetups = itemMatchPageModificationSetups ?? Enumerable.Empty<ListItemMatchPageModificationSetup<ItemIdType>>().Materialize();
			ValidationPredicate = validationPredicate;
			ValidationErrorNotifier = validationErrorNotifier;
		}
	}
}