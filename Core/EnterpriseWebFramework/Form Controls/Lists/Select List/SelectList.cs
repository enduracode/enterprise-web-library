using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayLinking;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.JavaScriptWriting;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	// This control should never support custom-text scenarios. An essential element of SelectList is that each item has both a label and an ID, and custom text
	// cannot meet this requirement. EwfTextBox would be a more appropriate place to implement custom-text "combo boxes".

	/// <summary>
	/// A drop-down list or radio button list.
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
		/// Creates a radio button list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type, unless an unlisted
		/// selected item label getter is passed.</param>
		/// <param name="useHorizontalLayout">Pass true if you want the radio buttons to be laid out horizontally instead of vertically.</param>
		/// <param name="unlistedSelectedItemLabelGetter">A function that will be called if the selected item ID does not match any list item and is not the default
		/// value of the type. The function takes the selected item ID and returns the label of the unlisted selected item, which will appear before all other
		/// items in the list. The string " (invalid)" will be appended to the label.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear and therefore none of the radio buttons will be
		/// selected if the selected item ID has the default value and none of the list items do.</param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		/// <param name="action">The action that will occur when the user hits Enter on a radio button.</param>
		/// <param name="autoPostBack">Pass true if you want an action to occur when the selection changes.</param>
		/// <param name="itemIdPageModificationValue"></param>
		/// <param name="itemMatchPageModificationSetups"></param>
		public static SelectList<ItemIdType> CreateRadioList<ItemIdType>(
			IEnumerable<SelectListItem<ItemIdType>> items, ItemIdType selectedItemId, bool useHorizontalLayout = false,
			Func<ItemIdType, string> unlistedSelectedItemLabelGetter = null, string defaultValueItemLabel = "", bool disableSingleButtonDetection = false,
			FormAction action = null, bool autoPostBack = false, PageModificationValue<ItemIdType> itemIdPageModificationValue = null,
			IEnumerable<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups = null ) {
			return new SelectList<ItemIdType>(
				useHorizontalLayout,
				null,
				unlistedSelectedItemLabelGetter,
				defaultValueItemLabel,
				null,
				null,
				items,
				disableSingleButtonDetection,
				selectedItemId,
				action,
				autoPostBack,
				itemIdPageModificationValue,
				itemMatchPageModificationSetups );
		}

		/// <summary>
		/// Creates a drop-down list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type, unless an unlisted
		/// selected item label getter is passed.</param>
		/// <param name="width">The width of the list. This overrides any value that may be specified via CSS. If no width is specified via CSS and you pass null
		/// for this parameter, the list will be just wide enough to show the selected item and will resize whenever the selected item is changed.</param>
		/// <param name="unlistedSelectedItemLabelGetter">A function that will be called if the selected item ID does not match any list item and is not the default
		/// value of the type. The function takes the selected item ID and returns the label of the unlisted selected item, which will appear before all other
		/// items in the list. The string " (invalid)" will be appended to the label.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear.</param>
		/// <param name="placeholderIsValid">Pass true if you would like the list to include a default-value placeholder that is considered a valid selection.
		/// This will only be included if none of the list items have an ID with the default value and the default-value item label is the empty string. If you pass
		/// false, the list will still include a default-value placeholder if the selected item ID has the default value and none of the list items do, but in this
		/// case the placeholder will not be considered a valid selection.</param>
		/// <param name="placeholderText">The default-value placeholder's text. Do not pass null.</param>
		/// <param name="action">The action that will occur when the user hits Enter on the drop-down list.</param>
		/// <param name="autoPostBack">Pass true if you want an action to occur when the selection changes.</param>
		/// <param name="itemIdPageModificationValue"></param>
		/// <param name="itemMatchPageModificationSetups"></param>
		public static SelectList<ItemIdType> CreateDropDown<ItemIdType>(
			IEnumerable<SelectListItem<ItemIdType>> items, ItemIdType selectedItemId, System.Web.UI.WebControls.Unit? width = null,
			Func<ItemIdType, string> unlistedSelectedItemLabelGetter = null, string defaultValueItemLabel = "", bool placeholderIsValid = false,
			string placeholderText = "Please select", FormAction action = null, bool autoPostBack = false,
			PageModificationValue<ItemIdType> itemIdPageModificationValue = null,
			IEnumerable<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups = null ) {
			return new SelectList<ItemIdType>(
				null,
				width,
				unlistedSelectedItemLabelGetter,
				defaultValueItemLabel,
				placeholderIsValid,
				placeholderText,
				items,
				null,
				selectedItemId,
				action,
				autoPostBack,
				itemIdPageModificationValue,
				itemMatchPageModificationSetups ?? ImmutableArray<ListItemMatchPageModificationSetup<ItemIdType>>.Empty );
		}
	}

	/// <summary>
	/// A drop-down list or radio button list.
	/// </summary>
	public class SelectList<ItemIdType>: System.Web.UI.WebControls.WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, FormValueControl, DisplayLink {
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
		private readonly System.Web.UI.WebControls.Unit? width;
		private readonly ImmutableArray<ListItem> items;
		private readonly Dictionary<string, SelectListItem<ItemIdType>> itemsByStringId;
		private readonly bool? disableSingleRadioButtonDetection;
		private readonly ItemIdType selectedItemId;
		private readonly FormAction action;
		private readonly bool autoPostBack;
		private readonly PageModificationValue<ItemIdType> itemIdPageModificationValue;
		private readonly IEnumerable<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups;

		private readonly List<Tuple<IEnumerable<ItemIdType>, bool, IEnumerable<System.Web.UI.WebControls.WebControl>>> displayLinks =
			new List<Tuple<IEnumerable<ItemIdType>, bool, IEnumerable<System.Web.UI.WebControls.WebControl>>>();

		private LegacyFreeFormRadioList<ItemIdType> radioList;
		private EwfCheckBox firstRadioButton;
		private FormValue<ItemIdType> formValue;
		private System.Web.UI.WebControls.WebControl selectControl;

		internal SelectList(
			bool? useHorizontalRadioLayout, System.Web.UI.WebControls.Unit? width, Func<ItemIdType, string> unlistedSelectedItemLabelGetter,
			string defaultValueItemLabel, bool? placeholderIsValid, string placeholderText, IEnumerable<SelectListItem<ItemIdType>> listItems,
			bool? disableSingleRadioButtonDetection, ItemIdType selectedItemId, FormAction action, bool autoPostBack,
			PageModificationValue<ItemIdType> itemIdPageModificationValue,
			IEnumerable<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups ) {
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
			this.autoPostBack = autoPostBack;
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

		public void AddDisplayLink( IEnumerable<ItemIdType> itemIds, bool controlsVisibleOnMatch, IEnumerable<System.Web.UI.WebControls.WebControl> controls ) {
			displayLinks.Add( Tuple.Create( itemIds, controlsVisibleOnMatch, controls.ToArray() as IEnumerable<System.Web.UI.WebControls.WebControl> ) );
		}

		void ControlTreeDataLoader.LoadData() {
			if( useHorizontalRadioLayout.HasValue ) {
				radioList = LegacyFreeFormRadioList.Create(
					items.Any( i => !i.IsValid ),
					selectedItemId,
					disableSingleButtonDetection: disableSingleRadioButtonDetection.Value,
					itemIdPageModificationValue: itemIdPageModificationValue,
					itemMatchPageModificationSetups: itemMatchPageModificationSetups );
				foreach( var i in displayLinks )
					radioList.AddDisplayLink( i.Item1, i.Item2, i.Item3 );

				var radioButtons = items.Where( i => i.IsValid )
					.Select( i => radioList.CreateInlineRadioButton( i.Item.Id, label: i.Item.Label, action: action, autoPostBack: autoPostBack ) )
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

				selectControl = new System.Web.UI.WebControls.WebControl( HtmlTextWriterTag.Select ) { Width = width ?? System.Web.UI.WebControls.Unit.Empty };
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
					if( autoPostBack )
						changeHandler += action.GetJsStatements() + " return false";
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

				EwfPage.Instance.AddDisplayLink( this );
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

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			foreach( var displayLink in displayLinks ) {
				var match = displayLink.Item1.Contains( formValue.GetValue( formControlValues ) );
				var visible = ( displayLink.Item2 && match ) || ( !displayLink.Item2 && !match );
				foreach( var control in displayLink.Item3 )
					DisplayLinkingOps.SetControlDisplay( control, visible );
			}
		}

		void DisplayLink.AddJavaScript() {
			foreach( var displayLink in displayLinks ) {
				var scripts = from control in displayLink.Item3
				              select "setElementDisplay( '" + control.ClientID + "', [ " +
				                     StringTools.ConcatenateWithDelimiter( ", ", displayLink.Item1.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ) +
				                     " ].indexOf( $( '#" + selectControl.ClientID + "' ).val() ) " + ( displayLink.Item2 ? "!" : "=" ) + "= -1 )";
				selectControl.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onchange, StringTools.ConcatenateWithDelimiter( "; ", scripts.ToArray() ) );
			}
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( useHorizontalRadioLayout.HasValue )
				return "";

			var placeholderItem = items.SingleOrDefault( i => i.IsPlaceholder );
			return "$( '#{0}' ).chosen( {{ {1} }} ){2};".FormatWith(
				selectControl.ClientID,
				StringTools.ConcatenateWithDelimiter(
					", ",
					placeholderItem != null && placeholderItem.IsValid ? "allow_single_deselect: true" : "",
					placeholderItem != null
						// Don't let the placeholder value be the empty string since this seems to confuse Chosen.
						? "placeholder_text_single: '{0}'".FormatWith(
							placeholderItem.Item.Label.Any() ? HttpUtility.JavaScriptStringEncode( placeholderItem.Item.Label ) : " " )
						: "",
					"search_contains: true",
					"width: '{0}'".FormatWith( width.HasValue ? width.Value.ToString() : "" ) ),
				// Do this after .chosen since we only want it to affect the native select.
				placeholderItem != null
					? ".children().eq( {0} ).text( '{1}' )".FormatWith(
						items.IndexOf( placeholderItem ),
						HttpUtility.JavaScriptStringEncode( placeholderItem.Item.Label ) )
					: "" );
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