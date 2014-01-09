using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	// This control should never support custom-text scenarios. An essential element of SelectList is that each item has both a label and an ID, and custom text
	// cannot meet this requirement. EwfTextBox would be a more appropriate place to implement custom-text "combo boxes".

	/// <summary>
	/// A drop-down list or radio button list.
	/// </summary>
	public static class SelectList {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string DropDownCssClass = "ewfDropDown";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "DropDownList", "div." + DropDownCssClass + " > select", "div." + DropDownCssClass + " > .select2-container" ) };
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
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		/// <param name="postBack">The post-back that will occur when the user hits Enter on a radio button.</param>
		/// <param name="autoPostBack">Pass true if you want a post-back to occur when the selection changes.</param>
		public static SelectList<ItemIdType> CreateRadioList<ItemIdType>( IEnumerable<EwfListItem<ItemIdType>> items, ItemIdType selectedItemId,
		                                                                  bool useHorizontalLayout = false, string defaultValueItemLabel = "",
		                                                                  bool disableSingleButtonDetection = false, PostBack postBack = null,
		                                                                  bool autoPostBack = false ) {
			return new SelectList<ItemIdType>( useHorizontalLayout,
			                                   null,
			                                   defaultValueItemLabel,
			                                   null,
			                                   null,
			                                   items,
			                                   disableSingleButtonDetection,
			                                   selectedItemId,
			                                   postBack,
			                                   autoPostBack );
		}

		/// <summary>
		/// Creates a drop-down list.
		/// </summary>
		/// <param name="items">The items in the list. There must be at least one.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type.</param>
		/// <param name="width">The width of the list. This overrides any value that may be specified via CSS. If no width is specified via CSS and you pass null
		/// for this parameter, the list will be just wide enough to show the selected item and will resize whenever the selected item is changed.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear.</param>
		/// <param name="placeholderIsValid">Pass true if you would like the list to include a default-value placeholder that is considered a valid selection.
		/// This will only be included if none of the list items have an ID with the default value and the default-value item label is the empty string. If you pass
		/// false, the list will still include a default-value placeholder if the selected item ID has the default value and none of the list items do, but in this
		/// case the placeholder will not be considered a valid selection.</param>
		/// <param name="placeholderText">The default-value placeholder's text. Do not pass null.</param>
		/// <param name="postBack">The post-back that will occur when the user hits Enter on the drop-down list.</param>
		/// <param name="autoPostBack">Pass true if you want a post-back to occur when the selection changes.</param>
		public static SelectList<ItemIdType> CreateDropDown<ItemIdType>( IEnumerable<EwfListItem<ItemIdType>> items, ItemIdType selectedItemId, Unit? width = null,
		                                                                 string defaultValueItemLabel = "", bool placeholderIsValid = false,
		                                                                 string placeholderText = "Please select", PostBack postBack = null, bool autoPostBack = false ) {
			return new SelectList<ItemIdType>( null,
			                                   width,
			                                   defaultValueItemLabel,
			                                   placeholderIsValid,
			                                   placeholderText,
			                                   items,
			                                   null,
			                                   selectedItemId,
			                                   postBack,
			                                   autoPostBack );
		}
	}

	/// <summary>
	/// A drop-down list or radio button list.
	/// </summary>
	public class SelectList<ItemIdType>: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, FormControl, ControlWithCustomFocusLogic, DisplayLink {
		private readonly bool? useHorizontalRadioLayout;
		private readonly Unit? width;
		private readonly IEnumerable<SelectListItem<ItemIdType>> items;
		private readonly Dictionary<string, EwfListItem<ItemIdType>> itemsByStringId;
		private readonly bool? disableSingleRadioButtonDetection;
		private readonly ItemIdType selectedItemId;
		private readonly PostBack postBack;
		private readonly bool autoPostBack;

		private readonly List<Tuple<IEnumerable<ItemIdType>, bool, IEnumerable<WebControl>>> displayLinks =
			new List<Tuple<IEnumerable<ItemIdType>, bool, IEnumerable<WebControl>>>();

		private FreeFormRadioList<ItemIdType> radioList;
		private EwfCheckBox firstRadioButton;
		private FormValue<ItemIdType> formValue;
		private WebControl selectControl;

		internal SelectList( bool? useHorizontalRadioLayout, Unit? width, string defaultValueItemLabel, bool? placeholderIsValid, string placeholderText,
		                     IEnumerable<EwfListItem<ItemIdType>> listItems, bool? disableSingleRadioButtonDetection, ItemIdType selectedItemId, PostBack postBack,
		                     bool autoPostBack ) {
			this.useHorizontalRadioLayout = useHorizontalRadioLayout;
			this.width = width;

			items = listItems.Select( i => new SelectListItem<ItemIdType>( i, true, false ) ).ToArray();
			this.selectedItemId = selectedItemId;
			items = getInitialItem( defaultValueItemLabel, placeholderIsValid, placeholderText ).Concat( items ).ToArray();
			if( items.All( i => !i.IsValid ) )
				throw new ApplicationException( "There must be at least one valid selection in the list." );

			try {
				itemsByStringId = items.ToDictionary( i => i.StringId, i => i.Item );
			}
			catch( ArgumentException ) {
				throw new ApplicationException( "Item IDs, when converted to strings, must be unique." );
			}

			this.disableSingleRadioButtonDetection = disableSingleRadioButtonDetection;

			if( !items.Any( i => StandardLibraryMethods.AreEqual( i.Item.Id, selectedItemId ) ) )
				throw new ApplicationException( "The selected item ID must either match a list item or be the default value of the type." );

			this.postBack = postBack;
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

		public void AddDisplayLink( IEnumerable<ItemIdType> itemIds, bool controlsVisibleOnMatch, IEnumerable<WebControl> controls ) {
			displayLinks.Add( Tuple.Create( itemIds, controlsVisibleOnMatch, controls.ToArray() as IEnumerable<WebControl> ) );
		}

		void ControlTreeDataLoader.LoadData() {
			if( useHorizontalRadioLayout.HasValue ) {
				radioList = FreeFormRadioList.Create( items.Any( i => !i.IsValid ), selectedItemId, disableSingleButtonDetection: disableSingleRadioButtonDetection.Value );
				foreach( var i in displayLinks )
					radioList.AddDisplayLink( i.Item1, i.Item2, i.Item3 );

				var radioButtons =
					items.Where( i => i.IsValid )
					     .Select( i => radioList.CreateInlineRadioButton( i.Item.Id, label: i.Item.Label, postBack: postBack, autoPostBack: autoPostBack ) )
					     .ToArray();
				firstRadioButton = radioButtons.First();

				var radioButtonsAsControls = radioButtons.Select( i => i as Control ).ToArray();
				Controls.Add( useHorizontalRadioLayout.Value
					              ? new ControlLine( radioButtonsAsControls ) as Control
					              : ControlStack.CreateWithControls( true, radioButtonsAsControls ) );
			}
			else {
				formValue = new FormValue<ItemIdType>( () => selectedItemId,
				                                       () => UniqueID,
				                                       v => v.ObjectToString( true ),
				                                       rawValue =>
				                                       rawValue != null && itemsByStringId.ContainsKey( rawValue )
					                                       ? PostBackValueValidationResult<ItemIdType>.CreateValidWithValue( itemsByStringId[ rawValue ].Id )
					                                       : PostBackValueValidationResult<ItemIdType>.CreateInvalid() );
				if( postBack != null || autoPostBack )
					EwfPage.Instance.AddPostBack( postBack ?? EwfPage.Instance.DataUpdatePostBack );

				PreRender += delegate { PostBackButton.MakeControlPostBackOnEnter( this, postBack ); };
				CssClass = CssClass.ConcatenateWithSpace( SelectList.CssElementCreator.DropDownCssClass );

				selectControl = new WebControl( HtmlTextWriterTag.Select ) { Width = width ?? Unit.Empty };
				selectControl.Attributes.Add( "name", UniqueID );
				if( autoPostBack ) {
					PreRender +=
						delegate {
							selectControl.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onchange,
							                                        PostBackButton.GetPostBackScript( postBack ?? EwfPage.Instance.DataUpdatePostBack ) );
						};
				}

				var placeholderItem = items.SingleOrDefault( i => i.IsPlaceholder );
				if( placeholderItem != null ) {
					// Don't let the attribute value be the empty string since this seems to confuse Select2.
					selectControl.Attributes.Add( "data-placeholder", placeholderItem.Item.Label.Any() ? placeholderItem.Item.Label : " " );
				}

				PreRender += delegate {
					foreach( var i in items )
						selectControl.Controls.Add( getOption( i.StringId, i.Item.Id, i.IsPlaceholder ? "" : i.Item.Label ) );
				};

				Controls.Add( selectControl );

				EwfPage.Instance.AddDisplayLink( this );
			}
		}

		private Control getOption( string value, ItemIdType id, string label ) {
			return new Literal
				{
					Text =
						"<option value=\"" + value + "\"" +
						( StandardLibraryMethods.AreEqual( id, formValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) ) ? " selected" : "" ) + ">" +
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
				              select
					              "setElementDisplay( '" + control.ClientID + "', [ " +
					              StringTools.ConcatenateWithDelimiter( ", ", displayLink.Item1.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ) +
					              " ].indexOf( $( '#" + selectControl.ClientID + "' ).val() ) " + ( displayLink.Item2 ? "!" : "=" ) + "= -1 )";
				selectControl.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onchange, StringTools.ConcatenateWithDelimiter( "; ", scripts.ToArray() ) );
			}
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( useHorizontalRadioLayout.HasValue )
				return "";

			var placeholderItem = items.SingleOrDefault( i => i.IsPlaceholder );
			var select2Statement = "$( '#" + selectControl.ClientID + "' ).select2( { width: 'copy', " +
			                       ( placeholderItem != null && placeholderItem.IsValid ? "allowClear: true, " : "" ) +
			                       "openOnEnter: false, sortResults: select2ResultSort } );";
			var touchStatement = placeholderItem != null
				                     ? "$( '#" + selectControl.ClientID + "' ).children().first().text( $( '#" + selectControl.ClientID +
				                       "' ).attr( 'data-placeholder' ) );"
				                     : "";

			// We previously used "!Modernizr.touch" as the condition. One reason this didn't work: Windows 8 always identifies as "touch" even if it's desktop.
			return "if( true ) " + select2Statement + touchStatement.PrependDelimiter( " else " );
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			if( useHorizontalRadioLayout.HasValue )
				( firstRadioButton as ControlWithCustomFocusLogic ).SetFocus();
			else
				Page.SetFocus( this );
		}

		FormValue FormControl.FormValue { get { return formValue; } }

		/// <summary>
		/// Validates and returns the selected item ID in the post back. The default value of the item ID type will be considered valid only if it matches a
		/// specified list item or the default-value item label is not the empty string or the default-value placeholder (drop-downs only) was specified to be
		/// valid.
		/// </summary>
		public ItemIdType ValidateAndGetSelectedItemIdInPostBack( PostBackValueDictionary postBackValues, Validator validator ) {
			// Both radioList and formValue will be null if this SelectList is never added to the page.
			var selectedItemIdInPostBack = radioList != null
				                               ? radioList.GetSelectedItemIdInPostBack( postBackValues )
				                               : formValue != null ? formValue.GetValue( postBackValues ) : selectedItemId;

			if( !items.Single( i => StandardLibraryMethods.AreEqual( i.Item.Id, selectedItemIdInPostBack ) ).IsValid )
				validator.NoteErrorAndAddMessage( "Please make a selection." );
			return selectedItemIdInPostBack;
		}

		/// <summary>
		/// Returns true if the selection changed on this post back.
		/// </summary>
		public bool SelectionChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			// Both radioList and formValue will be null if this SelectList is never added to the page.
			return radioList != null ? radioList.SelectionChangedOnPostBack( postBackValues ) : formValue != null && formValue.ValueChangedOnPostBack( postBackValues );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey {
			get {
				// Drop-down lists need a wrapping div to allow Select2 to be shown and hidden with display linking and to make the enter key submit the form.
				return HtmlTextWriterTag.Div;
			}
		}
	}
}