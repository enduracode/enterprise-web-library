using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Options for the AutoFill feature that will modify the behavior of the EwfTextBox to post-back when different events occur.
	/// </summary>
	public enum AutoCompleteOption {
		/// <summary>
		/// This option allows the text to be changed and an item to be selected without causing a post-back.
		/// </summary>
		NoPostBack,

		/// <summary>
		/// This option posts-back when an item is selected but will not post-back due to text change.
		/// </summary>
		PostBackOnItemSelect,

		/// <summary>
		/// This option will post-back when the text has changed or an item is selected.
		/// </summary>
		PostBackOnTextChangeAndItemSelect
	}

	/// <summary>
	/// A text box automatically placed in a wrapper so that it is styled consistently across all browsers.
	/// If the width is set in pixels, this control automatically adjusts it, subtracting 6, to make the final resultant width be
	/// the given value. Widths less than 6 pixels are not supported.
	/// </summary>
	public class EwfTextBox: WebControl, ControlTreeDataLoader, INamingContainer, FormControl, ControlWithJsInitLogic, ControlWithCustomFocusLogic {
		internal static void AddTextareaValue( Control textarea, string value ) {
			// The initial NewLine is here because of http://haacked.com/archive/2008/11/18/new-line-quirk-with-html-textarea.aspx and because this is what Microsoft
			// does in their System.Web.UI.WebControls.TextBox implementation.
			textarea.Controls.Add( new Literal { Text = HttpUtility.HtmlEncode( Environment.NewLine + value ) } );
		}

		private int rows;
		private readonly bool masksCharacters;
		private int? maxLength;
		private readonly bool readOnly;
		private readonly bool disableBrowserAutoComplete;
		private readonly bool? suggestSpellCheck;
		private readonly FormValue<string> formValue;
		private PostBack postBack;
		private readonly bool autoPostBack;
		private PageInfo autoCompleteService;
		private AutoCompleteOption autoCompleteOption;
		private string watermarkText = "";

		private WebControl textBox;

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		internal string TextBoxClientId { get { return textBox.ClientID; } }

		[ Obsolete( "Guaranteed through 31 January 2014. Please specify via constructor." ) ]
		public int MaxCharacters { get { return maxLength ?? 0; } set { maxLength = value; } }

		/// <summary>
		/// Creates a text box.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="rows">The number of rows in the text box.</param>
		/// <param name="masksCharacters">Pass true to mask characters entered in the text box. Has no effect when there is more than one row in the text box.
		/// </param>
		/// <param name="maxLength">The maximum number of characters that can be entered in this text box.</param>
		/// <param name="readOnly">Pass true to prevent the contents of the text box from being changed.</param>
		/// <param name="disableBrowserAutoComplete">If true, prevents the browser from displaying values the user previously entered.</param>
		/// <param name="suggestSpellCheck">By default, Firefox does not spell check single-line text boxes. By default, Firefox does spell check multi-line text
		/// boxes. Setting this parameter to a value will set the spellcheck attribute on the text box to enable/disable spell checking, if the user agent supports
		/// it.</param>
		/// <param name="postBack">The post-back that will be performed when the user hits Enter on the text box or selects an auto-complete item.</param>
		/// <param name="autoPostBack">Pass true to cause an automatic post-back when the text box loses focus.</param>
		public EwfTextBox( string value, int rows = 1, bool masksCharacters = false, int? maxLength = null, bool readOnly = false,
		                   bool disableBrowserAutoComplete = false, bool? suggestSpellCheck = null, PostBack postBack = null, bool autoPostBack = false ) {
			this.rows = rows;
			this.masksCharacters = masksCharacters;
			this.maxLength = maxLength;
			this.readOnly = readOnly;
			this.disableBrowserAutoComplete = disableBrowserAutoComplete;
			this.suggestSpellCheck = suggestSpellCheck;

			if( value == null )
				throw new ApplicationException( "You cannot create a text box with a null value. Please use the empty string instead." );
			formValue = new FormValue<string>( () => value,
			                                   () => this.IsOnPage() ? UniqueID : "",
			                                   v => v,
			                                   rawValue =>
			                                   rawValue != null
				                                   ? PostBackValueValidationResult<string>.CreateValidWithValue( rawValue )
				                                   : PostBackValueValidationResult<string>.CreateInvalid() );

			this.postBack = postBack;
			this.autoPostBack = autoPostBack;
		}

		[ Obsolete( "Guaranteed through 31 January 2014. Please specify via constructor." ) ]
		public int Rows { get { return rows; } set { rows = value; } }

		/// <summary>
		/// Sets this text box up for AJAX auto-complete.
		/// </summary>
		public void SetupAutoComplete( PageInfo service, AutoCompleteOption option ) {
			autoCompleteService = service;
			autoCompleteOption = option;
		}

		/// <summary>
		/// Allows for adding custom JavaScript event scripts to the text box.
		/// </summary>
		public void AddJavaScriptEventScript( string jsEventConstant, string script ) {
			PreRender += delegate { textBox.AddJavaScriptEventScript( jsEventConstant, script ); };
		}

		/// <summary>
		/// The given text will be shown by default and will vanish when the text box gains focus. If focus is lost again, the text will reappear. This method has
		/// no effect if the text box has a non empty value. Do not pass null for text.
		/// </summary>
		public void SetWatermarkText( string text ) {
			watermarkText = text;
		}

		void ControlTreeDataLoader.LoadData() {
			var isTextarea = rows > 1;
			textBox = new WebControl( isTextarea ? HtmlTextWriterTag.Textarea : HtmlTextWriterTag.Input );
			PreRender += delegate {
				if( !isTextarea )
					textBox.Attributes.Add( "type", masksCharacters ? "password" : "text" );
				textBox.Attributes.Add( "name", UniqueID );
				if( isTextarea )
					textBox.Attributes.Add( "rows", rows.ToString() );
				if( maxLength.HasValue )
					textBox.Attributes.Add( "maxlength", maxLength.Value.ToString() );
				if( readOnly )
					textBox.Attributes.Add( "readonly", "readonly" );
				if( disableBrowserAutoComplete || autoCompleteService != null )
					textBox.Attributes.Add( "autocomplete", "off" );
				if( suggestSpellCheck.HasValue )
					textBox.Attributes.Add( "spellcheck", suggestSpellCheck.Value.ToString().ToLower() );

				var value = formValue.GetValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues );
				var valueOrWatermark = watermarkText.Any() && !value.Any() ? watermarkText : value;
				if( isTextarea )
					AddTextareaValue( textBox, valueOrWatermark );
				else if( !masksCharacters )
					textBox.Attributes.Add( "value", valueOrWatermark );
			};
			Controls.Add( textBox );

			if( watermarkText.Any() ) {
				textBox.AddJavaScriptEventScript( JsWritingMethods.onfocus, "if( value == '" + watermarkText + "' ) value = ''" );
				textBox.AddJavaScriptEventScript( JsWritingMethods.onblur, "if( value == '' ) value = '" + watermarkText + "'" );
				EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( GetType(),
				                                                         UniqueID + "watermark",
				                                                         "$( '#" + textBox.ClientID + "' ).filter( function() { return this.value == '" + watermarkText +
				                                                         "'; } ).val( '' )" );
			}

			var postBackOnEnter = postBack != null || autoPostBack ||
			                      ( autoCompleteService != null && autoCompleteOption == AutoCompleteOption.PostBackOnTextChangeAndItemSelect );
			if( postBack == null && ( autoPostBack || ( autoCompleteService != null && autoCompleteOption != AutoCompleteOption.NoPostBack ) ) )
				postBack = EwfPage.Instance.DataUpdatePostBack;

			if( postBack != null ) {
				EwfPage.Instance.AddPostBack( postBack );
				PreRender += delegate {
					if( postBackOnEnter )
						PostBackButton.MakeControlPostBackOnEnter( this, postBack );
					if( autoPostBack || ( autoCompleteService != null && autoCompleteOption == AutoCompleteOption.PostBackOnTextChangeAndItemSelect ) ) {
						// Use setTimeout to prevent keypress and change from *both* triggering post-backs at the same time when Enter is pressed after a text change.
						textBox.AddJavaScriptEventScript( JsWritingMethods.onchange,
						                                  "setTimeout( function() { " + PostBackButton.GetPostBackScript( postBack, includeReturnFalse: false ) + "; }, 0 )" );
					}
				};
			}

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), textBox );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			var script = new StringBuilder();

			if( watermarkText.Any() ) {
				var restorationStatement = "$( '#" + textBox.ClientID + "' ).filter( function() { return this.value == ''; } ).val( '" + watermarkText + "' );";

				// The first line is for bfcache browsers; the second is for all others. See http://stackoverflow.com/q/1195440/35349.
				script.Append( "$( window ).on( 'pagehide', function() { " + restorationStatement + " } );" );
				script.Append( restorationStatement );
			}

			if( autoCompleteService != null ) {
				const int delay = 250; // Default delay is 300 ms.
				const int minCharacters = 3;

				var autocompleteOptions = new List<Tuple<string, string>>();
				autocompleteOptions.Add( Tuple.Create( "delay", delay.ToString() ) );
				autocompleteOptions.Add( Tuple.Create( "minLength", minCharacters.ToString() ) );
				autocompleteOptions.Add( Tuple.Create( "source", "'" + autoCompleteService.GetUrl() + "'" ) );

				if( autoCompleteOption != AutoCompleteOption.NoPostBack ) {
					var handler = "function( event, ui ) {{ $( '#{0}' ).val( ui.item.value ); {1}; }}".FormatWith( textBox.ClientID,
					                                                                                               PostBackButton.GetPostBackScript( postBack ) );
					autocompleteOptions.Add( Tuple.Create( "select", handler ) );
				}

				script.Append( @"$( '#" + textBox.ClientID +
				               "' ).autocomplete( {{ {0} }} );".FormatWith(
					               autocompleteOptions.Select( o => "{0}: {1}".FormatWith( o.Item1, o.Item2 ) ).GetCommaDelimitedStringFromCollection() ) );
			}

			return script.ToString();
		}

		FormValue FormControl.FormValue { get { return formValue; } }

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		public string GetPostBackValue( PostBackValueDictionary postBackValues ) {
			return formValue.GetValue( postBackValues );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return formValue.ValueChangedOnPostBack( postBackValues );
		}

		void ControlWithCustomFocusLogic.SetFocus() {
			Page.SetFocus( textBox );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }

		/// <summary>
		/// Renders this control after applying the appropriate CSS classes.
		/// </summary>
		protected override void Render( HtmlTextWriter writer ) {
			CssClass = CssClass.ConcatenateWithSpace( "textBoxWrapper" );
			if( Width.Type == UnitType.Pixel && Width.Value > 0 ) // Only modify width if it has been explicitly set in pixels.
				Width = (int)Width.Value - 6;
			base.Render( writer );
		}
	}
}