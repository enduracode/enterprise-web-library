using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	// This control should never support custom-text scenarios. An essential element of SelectList is that each item has both a label and an ID, and custom text
	// cannot meet this requirement. EwfTextBox would be a more appropriate place to implement custom-text "combo boxes".

	/// <summary>
	/// A drop-down list or radio button list.
	/// </summary>
	public static class SelectList {
		internal class CssElementCreator: ControlCssElementCreator {
			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "DropDownList", "select" ) };
			}
		}

		public static IEnumerable<EwfListItem<bool?>> GetYesNoItems() {
			return GetTrueFalseItems( "Yes", "No" );
		}

		public static IEnumerable<EwfListItem<bool?>> GetTrueFalseItems( string trueLabel, string falseLabel ) {
			return new[] { EwfListItem.Create<bool?>( true, trueLabel ), EwfListItem.Create<bool?>( false, falseLabel ) };
		}

		/// <summary>
		/// Creates a radio button list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type.</param>
		/// <param name="useHorizontalLayout">Pass true if you want the radio buttons to be laid out horizontally instead of vertically.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear and therefore none of the radio buttons will be
		/// selected if the selected item ID has the default value and none of the list items do.</param>
		/// <param name="autoPostBack">Pass true if you want a post back to occur when the selection changes.</param>
		public static SelectList<ItemIdType> CreateRadioList<ItemIdType>( IEnumerable<EwfListItem<ItemIdType>> items, ItemIdType selectedItemId,
		                                                                  bool useHorizontalLayout = false, string defaultValueItemLabel = "",
		                                                                  bool autoPostBack = false ) {
			return new SelectList<ItemIdType>( useHorizontalLayout, defaultValueItemLabel, null, null, items, selectedItemId, autoPostBack );
		}

		/// <summary>
		/// Creates a drop-down list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear.</param>
		/// <param name="placeholderIsValid">Pass true if you would like the list to include a default-value placeholder that is considered a valid selection.
		/// This will only be included if none of the list items have an ID with the default value and the default-value item label is the empty string. If you pass
		/// false, the list will still include a default-value placeholder if the selected item ID has the default value and none of the list items do, but in this
		/// case the placeholder will not be considered a valid selection.</param>
		/// <param name="placeholderText">The default-value placeholder's text. Do not pass null.</param>
		/// <param name="autoPostBack">Pass true if you want a post back to occur when the selection changes.</param>
		public static SelectList<ItemIdType> CreateDropDown<ItemIdType>( IEnumerable<EwfListItem<ItemIdType>> items, ItemIdType selectedItemId,
		                                                                 string defaultValueItemLabel = "", bool placeholderIsValid = false,
		                                                                 string placeholderText = "Please select", bool autoPostBack = false ) {
			return new SelectList<ItemIdType>( null, defaultValueItemLabel, placeholderIsValid, placeholderText, items, selectedItemId, autoPostBack );
		}
	}

	/// <summary>
	/// A drop-down list or radio button list.
	/// </summary>
	public class SelectList<ItemIdType>: WebControl, IPostBackDataHandler, ControlTreeDataLoader, ControlWithJsInitLogic, FormControl<ItemIdType> {
		private readonly bool? useHorizontalRadioLayout;
		private readonly IEnumerable<SelectListItem<ItemIdType>> items;
		private readonly Dictionary<string, EwfListItem<ItemIdType>> itemsByStringId;
		private readonly ItemIdType selectedItemId;
		private readonly bool autoPostBack;
		private FreeFormRadioList<ItemIdType> radioList;
		private string postValue;

		internal SelectList( bool? useHorizontalRadioLayout, string defaultValueItemLabel, bool? placeholderIsValid, string placeholderText,
		                     IEnumerable<EwfListItem<ItemIdType>> listItems, ItemIdType selectedItemId, bool autoPostBack ) {
			this.useHorizontalRadioLayout = useHorizontalRadioLayout;

			items = listItems.Select( i => new SelectListItem<ItemIdType>( i, true, false ) ).ToArray();
			items = getInitialItem( defaultValueItemLabel, placeholderIsValid, placeholderText ).Concat( items ).ToArray();
			if( items.All( i => !i.IsValid ) )
				throw new ApplicationException( "There must be at least one valid selection in the list." );

			// This check is only strictly necessary for drop-down lists.
			try {
				itemsByStringId = items.ToDictionary( i => i.StringId, i => i.Item );
			}
			catch( ArgumentException ) {
				throw new ApplicationException( "Item IDs, when converted to strings, must be unique." );
			}

			if( !items.Any( i => StandardLibraryMethods.AreEqual( i.Item.Id, selectedItemId ) ) )
				throw new ApplicationException( "The selected item ID must either match a list item or be the default value of the type." );
			this.selectedItemId = selectedItemId;

			this.autoPostBack = autoPostBack;
		}

		private IEnumerable<SelectListItem<ItemIdType>> getInitialItem( string defaultValueItemLabel, bool? placeholderIsValid, string placeholderText ) {
			var itemIdDefaultValue = StandardLibraryMethods.GetDefaultValue<ItemIdType>( true );
			if( items.Any( i => StandardLibraryMethods.AreEqual( i.Item.Id, itemIdDefaultValue ) ) )
				yield break;

			var selectedItemIdHasDefaultValue = StandardLibraryMethods.AreEqual( selectedItemId, itemIdDefaultValue );
			var includeDefaultValueItemOrValidPlaceholder = defaultValueItemLabel.Any() || ( !useHorizontalRadioLayout.HasValue && placeholderIsValid.Value );
			if( !selectedItemIdHasDefaultValue && !includeDefaultValueItemOrValidPlaceholder )
				yield break;

			var isPlaceholder = !useHorizontalRadioLayout.HasValue && !defaultValueItemLabel.Any();
			yield return
				new SelectListItem<ItemIdType>( EwfListItem.Create( itemIdDefaultValue, isPlaceholder ? placeholderText : defaultValueItemLabel ),
				                                includeDefaultValueItemOrValidPlaceholder,
				                                isPlaceholder );
		}

		ItemIdType FormControl<ItemIdType>.DurableValue { get { return selectedItemId; } }
		string FormControl.DurableValueAsString { get { return selectedItemId.ToString(); } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			if( useHorizontalRadioLayout.HasValue ) {
				radioList = FreeFormRadioList.Create( UniqueID, items.Any( i => !i.IsValid ), AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this ) );
				var radioButtons = from i in items
				                   where i.IsValid
				                   select radioList.CreateInlineRadioButton( i.Item.Id, label: i.Item.Label, autoPostBack: autoPostBack ) as Control;
				Controls.Add( useHorizontalRadioLayout.Value
					              ? new ControlLine( radioButtons.ToArray() ) as Control
					              : ControlStack.CreateWithControls( true, radioButtons.ToArray() ) );
			}
			else {
				Attributes.Add( "name", UniqueID );
				if( autoPostBack )
					this.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onchange, PostBackButton.GetPostBackScript( this, false ) );

				var placeholderItem = items.SingleOrDefault( i => i.IsPlaceholder );
				if( placeholderItem != null )
					Attributes.Add( "data-placeholder", placeholderItem.Item.Label );

				foreach( var i in items )
					Controls.Add( getOption( i.StringId, i.Item.Id, i.IsPlaceholder ? "" : i.Item.Label ) );
			}
		}

		private Control getOption( string value, ItemIdType id, string label ) {
			return new Literal
				{
					Text =
						"<option value=\"" + value + "\"" +
						( StandardLibraryMethods.AreEqual( id, AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this ) ) ? " selected" : "" ) + ">" +
						label.GetTextAsEncodedHtml( returnNonBreakingSpaceIfEmpty: false ) + "</option>"
				};
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( useHorizontalRadioLayout.HasValue )
				return "";
			var placeholderItem = items.SingleOrDefault( i => i.IsPlaceholder );
			return "$( '#" + ClientID + "' ).chosen(" + ( placeholderItem != null && placeholderItem.IsValid ? " { allow_single_deselect: true } " : "" ) + ");";
		}

		bool IPostBackDataHandler.LoadPostData( string postDataKey, NameValueCollection postCollection ) {
			if( !useHorizontalRadioLayout.HasValue )
				postValue = postCollection[ postDataKey ];
			return false;
		}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			if( !useHorizontalRadioLayout.HasValue )
				postBackValues.Add( this, itemsByStringId.ContainsKey( postValue ) ? itemsByStringId[ postValue ].Id : items.First().Item.Id );
		}

		/// <summary>
		/// Validates and returns the selected item ID in the post back. The default value of the item ID type will be considered valid only if it matches a
		/// specified list item or the default-value item label is not the empty string or the default-value placeholder (drop-downs only) was specified to be
		/// valid.
		/// </summary>
		public ItemIdType ValidateAndGetSelectedItemIdInPostBack( PostBackValueDictionary postBackValues, Validator validator ) {
			var selectedItemIdInPostBack = useHorizontalRadioLayout.HasValue ? radioList.GetSelectedItemIdInPostBack( postBackValues ) : postBackValues.GetValue( this );
			if( !items.Single( i => StandardLibraryMethods.AreEqual( i.Item.Id, selectedItemIdInPostBack ) ).IsValid )
				validator.NoteErrorAndAddMessage( "Please make a selection." );
			return selectedItemIdInPostBack;
		}

		/// <summary>
		/// Returns true if the selection changed on this post back.
		/// </summary>
		public bool SelectionChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.ValueChangedOnPostBack( this );
		}

		bool FormControl.ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return SelectionChangedOnPostBack( postBackValues );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return useHorizontalRadioLayout.HasValue ? HtmlTextWriterTag.Div : HtmlTextWriterTag.Select; } }

		void IPostBackDataHandler.RaisePostDataChangedEvent() {}
	}
}