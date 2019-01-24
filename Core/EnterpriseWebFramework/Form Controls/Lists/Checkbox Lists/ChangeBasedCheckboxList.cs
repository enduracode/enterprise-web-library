using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A checkbox list that is based on changes to the selections rather than the absolute set of selected items.
	/// </summary>
	public static class ChangeBasedCheckboxList {
		/// <summary>
		/// Creates a change-based checkbox list, which is a checkbox list that is based on changes to the selections rather than the absolute set of selected
		/// items.
		/// </summary>
		/// <param name="items">The items in the list.</param>
		/// <param name="selectedItemIds">The selected-item IDs.</param>
		/// <param name="modificationMethod">A method that executes the change handlers of the items that were selected or deselected on this post back.</param>
		/// <param name="displaySetup"></param>
		/// <param name="includeSelectAndDeselectAllButtons"></param>
		/// <param name="minColumnWidth">The minimum width of each column in the list. Pass null to force a single column.</param>
		/// <param name="uiSelectedItemIds"></param>
		/// <param name="action">The action that will occur when the user hits Enter on any of the checkboxes. Pass null to use the current default action.</param>
		/// <param name="selectionChangedAction">The action that will occur when the selection is changed. Pass null for no action.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static CheckboxList<ItemIdType> Create<ItemIdType>(
			IEnumerable<ChangeBasedListItem<ItemIdType>> items, IEnumerable<ItemIdType> selectedItemIds, out Action modificationMethod,
			DisplaySetup displaySetup = null, bool includeSelectAndDeselectAllButtons = false, ContentBasedLength minColumnWidth = null,
			IEnumerable<ItemIdType> uiSelectedItemIds = null, FormAction action = null, FormAction selectionChangedAction = null,
			Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			items = items.Materialize();
			var selectedItemIdSet = selectedItemIds.ToImmutableHashSet();

			ImmutableHashSet<ItemIdType> selectedItemIdsInPostBack = null;
			modificationMethod = () => {
				if( selectedItemIdsInPostBack == null )
					return;
				var changedItemIds = selectedItemIdsInPostBack.Except( selectedItemIdSet ).Union( selectedItemIdSet.Except( selectedItemIdsInPostBack ) ).ToArray();
				foreach( var i in items.Where( i => changedItemIds.Contains( i.Item.Id ) ) )
					i.ChangeHandler( selectedItemIdsInPostBack.Contains( i.Item.Id ) );
			};

			return new CheckboxList<ItemIdType>(
				CheckboxListSetup.Create(
					from i in items select i.Item,
					displaySetup: displaySetup,
					includeSelectAndDeselectAllButtons: includeSelectAndDeselectAllButtons,
					minColumnWidth: minColumnWidth,
					action: action,
					selectionChangedAction: selectionChangedAction,
					validationPredicate: validationPredicate,
					validationErrorNotifier: validationErrorNotifier ),
				uiSelectedItemIds ?? selectedItemIdSet,
				validationMethod: ( postBackValue, validator ) => selectedItemIdsInPostBack = postBackValue.ToImmutableHashSet() );
		}

		/// <summary>
		/// Creates a change-based checkbox list, which is a checkbox list that is based on changes to the selections rather than the absolute set of selected
		/// items.
		/// </summary>
		/// <param name="items">The items in the list.</param>
		/// <param name="modificationMethod">A method that executes the change handlers of the items that were selected or deselected on this post back.</param>
		/// <param name="displaySetup"></param>
		/// <param name="includeSelectAndDeselectAllButtons"></param>
		/// <param name="minColumnWidth">The minimum width of each column in the list. Pass null to force a single column.</param>
		/// <param name="action">The action that will occur when the user hits Enter on any of the checkboxes. Pass null to use the current default action.</param>
		/// <param name="selectionChangedAction">The action that will occur when the selection is changed. Pass null for no action.</param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationErrorNotifier"></param>
		public static CheckboxList<ItemIdType> Create<ItemIdType>(
			IEnumerable<ChangeBasedListItemWithSelectionState<ItemIdType>> items, out Action modificationMethod, DisplaySetup displaySetup = null,
			bool includeSelectAndDeselectAllButtons = false, ContentBasedLength minColumnWidth = null, FormAction action = null,
			FormAction selectionChangedAction = null, Func<bool, bool> validationPredicate = null, Action validationErrorNotifier = null ) {
			items = items.Materialize();
			return Create(
				from i in items select i.Item,
				from i in items where i.IsSelected select i.Item.Item.Id,
				out modificationMethod,
				displaySetup: displaySetup,
				includeSelectAndDeselectAllButtons: includeSelectAndDeselectAllButtons,
				minColumnWidth: minColumnWidth,
				uiSelectedItemIds: from i in items where i.IsSelectedInUi select i.Item.Item.Id,
				action: action,
				selectionChangedAction: selectionChangedAction,
				validationPredicate: validationPredicate,
				validationErrorNotifier: validationErrorNotifier );
		}
	}
}