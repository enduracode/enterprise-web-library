using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.JavaScriptWriting;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	// This control should never support custom-text scenarios. An essential element of SelectList is that each item has both a label and an ID, and custom text
	// cannot meet this requirement. TextControl would be a more appropriate place to implement custom-text "combo boxes".

	/// <summary>
	/// A drop-down or radio-button list.
	/// </summary>
	public static class SelectList {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string DropDownCssClass = "ewfDropDown";

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "DropDownList", "div." + DropDownCssClass + " > select", "div." + DropDownCssClass + " > .chosen-container" ) };
			}
		}

		public static IEnumerable<SelectListItem<bool?>> GetYesNoItems() {
			return GetTrueFalseItems( "Yes", "No" );
		}

		public static IEnumerable<SelectListItem<bool?>> GetTrueFalseItems( string trueLabel, string falseLabel ) {
			return new[] { SelectListItem.Create<bool?>( true, trueLabel ), SelectListItem.Create<bool?>( false, falseLabel ) };
		}

		/// <summary>
		/// Creates a radio-button list.
		/// </summary>
		/// <param name="setup">The setup object for the radio list.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type, unless an unlisted
		/// selected item label getter is passed.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear and therefore none of the radio buttons will be
		/// selected if the selected item ID has the default value and none of the list items do.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this radio-button list for page modification.</param>
		public static SelectList<ItemIdType> CreateRadioList<ItemIdType>(
			RadioListSetup<ItemIdType> setup, ItemIdType selectedItemId, string defaultValueItemLabel = "", Action<ItemIdType, Validator> validationMethod = null ) =>
			new SelectList<ItemIdType>(
				setup.UseHorizontalLayout,
				null,
				setup.UnlistedSelectedItemLabelGetter,
				defaultValueItemLabel,
				null,
				null,
				setup.Items,
				setup.FreeFormSetup.DisableSingleButtonDetection,
				selectedItemId,
				setup.Action,
				setup.FreeFormSetup.SelectionChangedAction,
				setup.FreeFormSetup.ItemIdPageModificationValue,
				setup.FreeFormSetup.ItemMatchPageModificationSetups,
				validationMethod );

		/// <summary>
		/// Creates a drop-down list.
		/// </summary>
		/// <param name="setup">The setup object for the drop-down.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type, unless an unlisted
		/// selected item label getter is passed.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear.</param>
		/// <param name="placeholderIsValid">Pass true if you would like the list to include a default-value placeholder that is considered a valid selection.
		/// This will only be included if none of the list items have an ID with the default value and the default-value item label is the empty string. If you pass
		/// false, the list will still include a default-value placeholder if the selected item ID has the default value and none of the list items do, but in this
		/// case the placeholder will not be considered a valid selection.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public static SelectList<ItemIdType> CreateDropDown<ItemIdType>(
			DropDownSetup<ItemIdType> setup, ItemIdType selectedItemId, string defaultValueItemLabel = "", bool placeholderIsValid = false,
			Action<ItemIdType, Validator> validationMethod = null ) =>
			new SelectList<ItemIdType>(
				null,
				setup.Width,
				setup.UnlistedSelectedItemLabelGetter,
				defaultValueItemLabel,
				placeholderIsValid,
				setup.PlaceholderText,
				setup.Items,
				null,
				selectedItemId,
				setup.Action,
				setup.SelectionChangedAction,
				setup.ItemIdPageModificationValue,
				setup.ItemMatchPageModificationSetups,
				validationMethod );
	}

	/// <summary>
	/// A drop-down or radio-button list.
	/// </summary>
	public class SelectList<ItemIdType>: System.Web.UI.WebControls.WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, FormValueControl {
		private class ListItem {
			private readonly SelectListItem<ItemIdType> item;
			private readonly bool isValid;
			private readonly bool isPlaceholder;

			internal ListItem( SelectListItem<ItemIdType> item, bool isValid, bool isPlaceholder ) {
				this.item = item;
				this.isValid = isValid;
				this.isPlaceholder = isPlaceholder;
			}

			internal SelectListItem<ItemIdType> Item { get { return item; } }

			internal string StringId {
				get {
					// Represent the default value with the empty string to support drop-down list placeholders. The HTML spec states that the "placeholder label option"
					// must have a value of the empty string. See https://html.spec.whatwg.org/multipage/forms.html#the-select-element.
					return EwlStatics.AreEqual( item.Id, EwlStatics.GetDefaultValue<ItemIdType>( false ) ) ? "" : item.Id.ToString();
				}
			}

			internal bool IsValid { get { return isValid; } }
			internal bool IsPlaceholder { get { return isPlaceholder; } }
		}

		private readonly bool? useHorizontalRadioLayout;
		private readonly ContentBasedLength width;
		private readonly ImmutableArray<ListItem> items;
		private readonly Dictionary<string, SelectListItem<ItemIdType>> itemsByStringId;
		private readonly bool? disableSingleRadioButtonDetection;
		private readonly ItemIdType selectedItemId;
		private readonly FormAction action;
		private readonly FormAction selectionChangedAction;
		private readonly PageModificationValue<ItemIdType> itemIdPageModificationValue;
		private readonly IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups;

		private LegacyFreeFormRadioList<ItemIdType> radioList;
		private EwfCheckBox firstRadioButton;
		private FormValue<ItemIdType> formValue;
		private System.Web.UI.WebControls.WebControl selectControl;

		internal SelectList(
			bool? useHorizontalRadioLayout, ContentBasedLength width, Func<ItemIdType, string> unlistedSelectedItemLabelGetter, string defaultValueItemLabel,
			bool? placeholderIsValid, string placeholderText, IEnumerable<SelectListItem<ItemIdType>> listItems, bool? disableSingleRadioButtonDetection,
			ItemIdType selectedItemId, FormAction action, FormAction selectionChangedAction, PageModificationValue<ItemIdType> itemIdPageModificationValue,
			IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups, Action<ItemIdType, Validator> validationMethod ) {
			this.useHorizontalRadioLayout = useHorizontalRadioLayout;
			this.width = width;

			items = listItems.Select( i => new ListItem( i, true, false ) ).ToImmutableArray();
			this.selectedItemId = selectedItemId;
			items = getInitialItems( unlistedSelectedItemLabelGetter, defaultValueItemLabel, placeholderIsValid, placeholderText ).Concat( items ).ToImmutableArray();
			if( items.All( i => !i.IsValid ) )
				throw new ApplicationException( "There must be at least one valid selection in the list." );

			try {
				itemsByStringId = items.ToDictionary( i => i.StringId, i => i.Item );
			}
			catch( ArgumentException ) {
				throw new ApplicationException( "Item IDs, when converted to strings, must be unique." );
			}

			this.disableSingleRadioButtonDetection = disableSingleRadioButtonDetection;
			this.action = action ?? FormState.Current.DefaultAction;
			this.selectionChangedAction = selectionChangedAction;
			this.itemIdPageModificationValue = itemIdPageModificationValue;
			this.itemMatchPageModificationSetups = itemMatchPageModificationSetups;
		}

		private IEnumerable<ListItem> getInitialItems(
			Func<ItemIdType, string> unlistedSelectedItemLabelGetter, string defaultValueItemLabel, bool? placeholderIsValid, string placeholderText ) {
			var itemIdDefaultValue = EwlStatics.GetDefaultValue<ItemIdType>( true );
			var selectedItemIdHasDefaultValue = EwlStatics.AreEqual( selectedItemId, itemIdDefaultValue );

			if( !items.Any( i => EwlStatics.AreEqual( i.Item.Id, selectedItemId ) ) && !selectedItemIdHasDefaultValue ) {
				if( unlistedSelectedItemLabelGetter == null )
					throw new ApplicationException( "The selected item ID must either match a list item or be the default value of the type." );
				yield return new ListItem( SelectListItem.Create( selectedItemId, unlistedSelectedItemLabelGetter( selectedItemId ) + " (invalid)" ), true, false );
			}

			if( items.Any( i => EwlStatics.AreEqual( i.Item.Id, itemIdDefaultValue ) ) )
				yield break;

			var includeDefaultValueItemOrValidPlaceholder = defaultValueItemLabel.Any() || ( !useHorizontalRadioLayout.HasValue && placeholderIsValid.Value );
			if( !selectedItemIdHasDefaultValue && !includeDefaultValueItemOrValidPlaceholder )
				yield break;

			var isPlaceholder = !useHorizontalRadioLayout.HasValue && !defaultValueItemLabel.Any();
			yield return new ListItem(
				SelectListItem.Create( itemIdDefaultValue, isPlaceholder ? placeholderText : defaultValueItemLabel ),
				includeDefaultValueItemOrValidPlaceholder,
				isPlaceholder );
		}

		void ControlTreeDataLoader.LoadData() {
			if( useHorizontalRadioLayout.HasValue ) {
				radioList = LegacyFreeFormRadioList.Create(
					items.Any( i => !i.IsValid ),
					selectedItemId,
					disableSingleButtonDetection: disableSingleRadioButtonDetection.Value,
					itemIdPageModificationValue: itemIdPageModificationValue,
					itemMatchPageModificationSetups: itemMatchPageModificationSetups );

				var radioButtons = items.Where( i => i.IsValid )
					.Select( i => radioList.CreateInlineRadioButton( i.Item.Id, label: i.Item.Label, action: action, autoPostBack: selectionChangedAction != null ) )
					.ToArray();
				firstRadioButton = radioButtons.First();

				var radioButtonsAsControls = radioButtons.Select( i => i as Control ).ToArray();
				Controls.Add(
					useHorizontalRadioLayout.Value
						? new ControlLine( radioButtonsAsControls ) as Control
						: ControlStack.CreateWithControls( true, radioButtonsAsControls ) );
			}
			else {
				formValue = new FormValue<ItemIdType>(
					() => selectedItemId,
					() => UniqueID,
					v => v.ObjectToString( true ),
					rawValue => rawValue != null && itemsByStringId.ContainsKey( rawValue )
						            ? PostBackValueValidationResult<ItemIdType>.CreateValid( itemsByStringId[ rawValue ].Id )
						            : PostBackValueValidationResult<ItemIdType>.CreateInvalid() );
				action.AddToPageIfNecessary();

				PreRender += delegate {
					var implicitSubmissionStatements = SubmitButton.GetImplicitSubmissionKeyPressStatements( action, false, legacy: true );
					if( implicitSubmissionStatements.Any() )
						this.AddJavaScriptEventScript( JsWritingMethods.onkeypress, implicitSubmissionStatements );
				};
				CssClass = CssClass.ConcatenateWithSpace( SelectList.CssElementCreator.DropDownCssClass );

				selectControl = new System.Web.UI.WebControls.WebControl( HtmlTextWriterTag.Select )
					{
						Width = width != null ? new System.Web.UI.WebControls.Unit( ( (CssLength)width ).Value ) : System.Web.UI.WebControls.Unit.Empty
					};
				selectControl.Attributes.Add( "name", UniqueID );
				PreRender += delegate {
					var changeHandler = "";
					if( itemIdPageModificationValue != null )
						changeHandler += itemIdPageModificationValue.GetJsModificationStatements( "$( '#{0}' ).val()".FormatWith( selectControl.ClientID ) );
					foreach( var setup in itemMatchPageModificationSetups ) {
						changeHandler += setup.PageModificationValue.GetJsModificationStatements(
							"[ {0} ].indexOf( $( '#{1}' ).val() ) != -1".FormatWith(
								StringTools.ConcatenateWithDelimiter( ", ", setup.ItemIds.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ),
								selectControl.ClientID ) );
					}
					if( selectionChangedAction != null )
						changeHandler += selectionChangedAction.GetJsStatements() + " return false";
					if( changeHandler.Any() )
						selectControl.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onchange, changeHandler );
				};

				PreRender += delegate {
					foreach( var i in items )
						selectControl.Controls.Add( getOption( i.StringId, i.Item.Id, i.IsPlaceholder ? "" : i.Item.Label ) );
				};

				Controls.Add( selectControl );

				if( itemIdPageModificationValue != null )
					formValue.AddPageModificationValue( itemIdPageModificationValue, v => v );
				foreach( var setup in itemMatchPageModificationSetups )
					formValue.AddPageModificationValue( setup.PageModificationValue, id => setup.ItemIds.Contains( id ) );
			}
		}

		private Control getOption( string value, ItemIdType id, string label ) {
			return new System.Web.UI.WebControls.Literal
				{
					Text = "<option value=\"" + value + "\"" +
					       ( EwlStatics.AreEqual( id, formValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) ) ? " selected" : "" ) + ">" +
					       label.GetTextAsEncodedHtml( returnNonBreakingSpaceIfEmpty: false ) + "</option>"
				};
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( useHorizontalRadioLayout.HasValue )
				return "";

			var placeholderItem = items.SingleOrDefault( i => i.IsPlaceholder );

			// Chosen’s allow_single_deselect only works if the placeholder is the first item.
			var chosenLogic = placeholderItem == null || placeholderItem == items.First()
				                  ? ".chosen( {{ {0} }} )".FormatWith(
					                  StringTools.ConcatenateWithDelimiter(
						                  ", ",
						                  placeholderItem != null && placeholderItem.IsValid ? "allow_single_deselect: true" : "",
						                  placeholderItem != null
							                  // Don't let the placeholder value be the empty string since this seems to confuse Chosen.
							                  ? "placeholder_text_single: '{0}'".FormatWith(
								                  placeholderItem.Item.Label.Any() ? HttpUtility.JavaScriptStringEncode( placeholderItem.Item.Label ) : " " )
							                  : "",
						                  "search_contains: true",
						                  "width: '{0}'".FormatWith( width != null ? ( (CssLength)width ).Value : "" ) ) )
				                  : "";

			// Do this after .chosen since we only want it to affect the native select.
			var placeholderTextLogic = placeholderItem != null
				                           ? ".children().eq( {0} ).text( '{1}' )".FormatWith(
					                           items.IndexOf( placeholderItem ),
					                           HttpUtility.JavaScriptStringEncode( placeholderItem.Item.Label ) )
				                           : "";

			return ( chosenLogic + placeholderTextLogic ).Surround( "$( '#{0}' )".FormatWith( selectControl.ClientID ), ";" );
		}

		FormValue FormValueControl.FormValue { get { return formValue; } }

		/// <summary>
		/// Validates and returns the selected item ID in the post back. The default value of the item ID type will be considered valid only if it matches a
		/// specified list item or the default-value item label is not the empty string or the default-value placeholder (drop-downs only) was specified to be
		/// valid.
		/// </summary>
		public ItemIdType ValidateAndGetSelectedItemIdInPostBack( PostBackValueDictionary postBackValues, Validator validator ) {
			// Both radioList and formValue will be null if this SelectList is never added to the page.
			var selectedItemIdInPostBack = radioList != null ? radioList.GetSelectedItemIdInPostBack( postBackValues ) :
			                               formValue != null ? formValue.GetValue( postBackValues ) : selectedItemId;

			if( !items.Single( i => EwlStatics.AreEqual( i.Item.Id, selectedItemIdInPostBack ) ).IsValid )
				validator.NoteErrorAndAddMessage( "Please make a selection." );
			return selectedItemIdInPostBack;
		}

		/// <summary>
		/// Returns true if the selection changed on this post back.
		/// </summary>
		public bool SelectionChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			// Both radioList and formValue will be null if this SelectList is never added to the page.
			return radioList != null
				       ? radioList.SelectionChangedOnPostBack( postBackValues )
				       : formValue != null && formValue.ValueChangedOnPostBack( postBackValues );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey {
			get {
				// Drop-down lists need a wrapping div to allow Chosen to be shown and hidden with display linking and to make the enter key submit the form.
				return HtmlTextWriterTag.Div;
			}
		}
	}
}