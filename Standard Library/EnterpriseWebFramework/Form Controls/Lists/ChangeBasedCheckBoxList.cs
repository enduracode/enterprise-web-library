using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A check box list that is based on changes to the selections rather than the absolute set of selected items.
	/// </summary>
	public static class ChangeBasedCheckBoxList {
		/// <summary>
		/// Creates a form item with a change based check box list, which is a check box list that is based on changes to the selections rather than the absolute
		/// set of selected items.
		/// </summary>
		/// <typeparam name="ItemIdType"></typeparam>
		/// <param name="label"></param>
		/// <param name="items"></param>
		/// <param name="selectedItemIds"></param>
		/// <param name="modificationMethod">A method that executes the change handlers of the items that were selected or deselected on this post back.</param>
		/// <param name="caption"></param>
		/// <param name="includeSelectAndDeselectAllButtons"></param>
		/// <param name="numberOfColumns"></param>
		/// <param name="uiSelectedItemIds"></param>
		/// <param name="cellSpan"></param>
		/// <param name="textAlignment"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationList"></param>
		/// <returns></returns>
		public static FormItem<ChangeBasedCheckBoxList<ItemIdType>> GetFormItem<ItemIdType>( string label, IEnumerable<ChangeBasedListItem<ItemIdType>> items,
		                                                                                     IEnumerable<ItemIdType> selectedItemIds, out Action modificationMethod,
		                                                                                     string caption = "", bool includeSelectAndDeselectAllButtons = false,
		                                                                                     byte numberOfColumns = 1,
		                                                                                     IEnumerable<ItemIdType> uiSelectedItemIds = null, int? cellSpan = null,
		                                                                                     TextAlignment textAlignment = TextAlignment.NotSpecified,
		                                                                                     Func<bool> validationPredicate = null,
		                                                                                     ValidationList validationList = null ) {
			var checkBoxList = Create( items,
			                           selectedItemIds,
			                           caption: caption,
			                           includeSelectAndDeselectAllButtons: includeSelectAndDeselectAllButtons,
			                           numberOfColumns: numberOfColumns,
			                           uiSelectedItemIds: uiSelectedItemIds );
			modificationMethod = checkBoxList.ModifyData;
			return FormItem.Create( label,
			                        checkBoxList,
			                        cellSpan: cellSpan,
			                        textAlignment: textAlignment,
			                        validationGetter: control => new Validation( ( pbv, validator ) => {
				                        if( validationPredicate != null && !validationPredicate() )
					                        return;
				                        control.Validate( pbv );
			                        },
			                                                                     validationList ?? EwfPage.Instance.PostBackDataModification ) );
		}

		/// <summary>
		/// Creates a form item with a change based check box list, which is a check box list that is based on changes to the selections rather than the absolute
		/// set of selected items.
		/// </summary>
		/// <typeparam name="ItemIdType"></typeparam>
		/// <param name="label"></param>
		/// <param name="items"></param>
		/// <param name="modificationMethod">A method that executes the change handlers of the items that were selected or deselected on this post back.</param>
		/// <param name="caption"></param>
		/// <param name="includeSelectAndDeselectAllButtons"></param>
		/// <param name="numberOfColumns"></param>
		/// <param name="cellSpan"></param>
		/// <param name="textAlignment"></param>
		/// <param name="validationPredicate"></param>
		/// <param name="validationList"></param>
		/// <returns></returns>
		public static FormItem<ChangeBasedCheckBoxList<ItemIdType>> GetFormItem<ItemIdType>( string label,
		                                                                                     IEnumerable<ChangeBasedListItemWithSelectionState<ItemIdType>> items,
		                                                                                     out Action modificationMethod, string caption = "",
		                                                                                     bool includeSelectAndDeselectAllButtons = false, byte numberOfColumns = 1,
		                                                                                     int? cellSpan = null,
		                                                                                     TextAlignment textAlignment = TextAlignment.NotSpecified,
		                                                                                     Func<bool> validationPredicate = null,
		                                                                                     ValidationList validationList = null ) {
			var checkBoxList = Create( items, caption: caption, includeSelectAndDeselectAllButtons: includeSelectAndDeselectAllButtons, numberOfColumns: numberOfColumns );
			modificationMethod = checkBoxList.ModifyData;
			return FormItem.Create( label,
			                        checkBoxList,
			                        cellSpan: cellSpan,
			                        textAlignment: textAlignment,
			                        validationGetter: control => new Validation( ( pbv, validator ) => {
				                        if( validationPredicate != null && !validationPredicate() )
					                        return;
				                        control.Validate( pbv );
			                        },
			                                                                     validationList ?? EwfPage.Instance.PostBackDataModification ) );
		}

		[ Obsolete( "Guaranteed through 30 June 2013." ) ]
		public static ChangeBasedCheckBoxList<ItemIdType> Create<ItemIdType>( IEnumerable<ChangeBasedListItem<ItemIdType>> items,
		                                                                      IEnumerable<ItemIdType> selectedItemIds, string caption = "",
		                                                                      bool includeSelectAndDeselectAllButtons = false, byte numberOfColumns = 1,
		                                                                      IEnumerable<ItemIdType> uiSelectedItemIds = null ) {
			return new ChangeBasedCheckBoxList<ItemIdType>( items,
			                                                selectedItemIds,
			                                                caption,
			                                                includeSelectAndDeselectAllButtons,
			                                                numberOfColumns,
			                                                uiSelectedItemIds ?? selectedItemIds );
		}

		[ Obsolete( "Guaranteed through 30 June 2013." ) ]
		public static ChangeBasedCheckBoxList<ItemIdType> Create<ItemIdType>( IEnumerable<ChangeBasedListItemWithSelectionState<ItemIdType>> items,
		                                                                      string caption = "", bool includeSelectAndDeselectAllButtons = false,
		                                                                      byte numberOfColumns = 1 ) {
			var itemArray = items.ToArray();
			var selectedItemIds = itemArray.Where( i => i.IsSelected ).Select( i => i.Item.Item.Id );
			var uiSelectedItemIds = itemArray.Where( i => i.IsSelectedInUi ).Select( i => i.Item.Item.Id );
			return new ChangeBasedCheckBoxList<ItemIdType>( itemArray.Select( i => i.Item ),
			                                                selectedItemIds,
			                                                caption,
			                                                includeSelectAndDeselectAllButtons,
			                                                numberOfColumns,
			                                                uiSelectedItemIds );
		}
	}

	[ Obsolete( "Guaranteed through 30 June 2013." ) ]
	public class ChangeBasedCheckBoxList<ItemIdType>: WebControl, ControlTreeDataLoader, ControlWithCustomFocusLogic {
		private readonly IEnumerable<ChangeBasedListItem<ItemIdType>> items;
		private readonly IEnumerable<ItemIdType> selectedItemIds;
		private readonly string caption;
		private readonly bool includeSelectAndDeselectAllButtons;
		private readonly byte numberOfColumns;
		private readonly IEnumerable<ItemIdType> uiSelectedItemIds;

		private EwfCheckBoxList<ItemIdType> checkBoxList;
		private IEnumerable<ItemIdType> selectedItemIdsInPostBack;

		internal ChangeBasedCheckBoxList( IEnumerable<ChangeBasedListItem<ItemIdType>> items, IEnumerable<ItemIdType> selectedItemIds, string caption,
		                                  bool includeSelectAndDeselectAllButtons, byte numberOfColumns, IEnumerable<ItemIdType> uiSelectedItemIds ) {
			this.items = items.ToArray();
			this.selectedItemIds = selectedItemIds.ToArray();
			this.caption = caption;
			this.includeSelectAndDeselectAllButtons = includeSelectAndDeselectAllButtons;
			this.numberOfColumns = numberOfColumns;
			this.uiSelectedItemIds = uiSelectedItemIds.ToArray();
		}

		void ControlTreeDataLoader.LoadData() {
			Controls.Add(
				checkBoxList =
				new EwfCheckBoxList<ItemIdType>( items.Select( i => i.Item ),
				                                 uiSelectedItemIds,
				                                 caption: caption,
				                                 includeSelectAndDeselectAllButtons: includeSelectAndDeselectAllButtons,
				                                 numberOfColumns: numberOfColumns ) );
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			( checkBoxList as ControlWithCustomFocusLogic ).SetFocus();
		}

		[ Obsolete( "Guaranteed through 30 June 2013." ) ]
		public void Validate( PostBackValueDictionary postBackValues ) {
			selectedItemIdsInPostBack = checkBoxList.GetSelectedItemIdsInPostBack( postBackValues );
		}

		[ Obsolete( "Guaranteed through 30 June 2013." ) ]
		public void ModifyData() {
			if( selectedItemIdsInPostBack == null )
				return;
			var changedItemIds = selectedItemIdsInPostBack.Except( selectedItemIds ).Union( selectedItemIds.Except( selectedItemIdsInPostBack ) ).ToArray();
			foreach( var i in items.Where( i => changedItemIds.Contains( i.Item.Id ) ) )
				i.ChangeHandler( selectedItemIdsInPostBack.Contains( i.Item.Id ) );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}