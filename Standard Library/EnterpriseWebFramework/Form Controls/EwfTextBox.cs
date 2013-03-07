using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using AjaxControlToolkit;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Options for the AutoFill feature that will modify the behavior of the ewfTextBox to post-back when different events occur.
	/// </summary>
	public enum AutoFillOptions {
		/// <summary>
		/// This option allows the text to be changed and an item to be selected causing a post-back.
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
	public class EwfTextBox: WebControl, ControlTreeDataLoader, IPostBackEventHandler, INamingContainer, FormControl<string>, ControlWithJsInitLogic,
	                         ControlWithCustomFocusLogic {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CompletionListCssClass = "autocomplete_completionListElement";
			internal const string CompletionListItemSelectedStateClass = "autocomplete_highlightedListItem";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[]
					{
						new CssElement( "TextBoxCompletionList", "ul." + CompletionListCssClass ),
						new CssElement( "TextBoxCompletionListItemAllStates", "li", "li." + CompletionListItemSelectedStateClass ),
						new CssElement( "TextBoxCompletionListItemSelectedState", "li." + CompletionListItemSelectedStateClass )
					};
			}
		}

		private readonly string durableValue;
		private readonly TextBox textBox = new TextBox();
		private bool masksCharacters;
		private WebMethodDefinition webMethodDefinition;
		private AutoFillOptions autoFillOption;
		private PostBackButton defaultSubmitButton;
		private string watermarkText = "";
		private readonly Action<string> postBackHandler;
		private readonly bool preventAutoComplete;
		// NOTE: Make this readonly when this is added to form item generation.
		private bool? suggestSpellCheck;

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		internal string TextBoxClientId { get { return textBox.ClientID; } }

		/// <summary>
		/// Gets or sets the maximum number of characters that can be entered in this text box.
		/// </summary>
		public int MaxCharacters { get { return textBox.MaxLength; } set { textBox.MaxLength = value; } }

		/// <summary>
		/// Creates a text box. Do not pass null for value.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="postBackHandler">The handler that will be executed when the user hits Enter on the text box or selects an autocomplete item. The parameter
		/// is the post back value.</param>
		/// <param name="preventAutoComplete">If true, prevents the browser from displaying values the user previously entered.</param>
		/// <param name="suggestSpellCheck">By default, Firefox does not spell check single-line text boxes. By default, Firefox
		/// does spell check multi-line text boxes. Setting this parameter to a value will set the spellcheck attribute on the
		/// text box to enable/disable spell checking, if the user agent supports it.</param>
		public EwfTextBox( string value, Action<string> postBackHandler = null, bool preventAutoComplete = false, bool? suggestSpellCheck = null ) {
			durableValue = value;
			textBox.ID = "theTextBox";
			base.Controls.Add( textBox );
			Rows = 1;
			this.postBackHandler = postBackHandler;
			this.preventAutoComplete = preventAutoComplete;
			this.suggestSpellCheck = suggestSpellCheck;
		}

		string FormControl<string>.DurableValue { get { return durableValue; } }
		string FormControl.DurableValueAsString { get { return durableValue; } }

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public string Value { get { return GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ); } }

		/// <summary>
		/// Sets whether an automatic postback occurs when the text box loses focus.
		/// </summary>
		public bool AutoPostBack { set { textBox.AutoPostBack = value; } }

		/// <summary>
		/// Sets whether the contents of the text box can be changed.
		/// </summary>
		public bool ReadOnly { set { textBox.ReadOnly = value; } }

		/// <summary>
		/// By default, Firefox does not spell check single-line text boxes. By default, Firefox
		/// does spell check multi-line text boxes. Setting this parameter to a value will set the spellcheck attribute on the
		/// text box to enable/disable spell checking, if the user agent supports it.
		/// </summary>
		public bool SuggestSpellCheck { set { suggestSpellCheck = value; } }

		/// <summary>
		/// Gets or sets the number of rows in the text box.
		/// </summary>
		public int Rows {
			get { return textBox.Rows; }
			set {
				textBox.Rows = value;
				textBox.TextMode = value > 1 ? TextBoxMode.MultiLine : ( masksCharacters ? TextBoxMode.Password : TextBoxMode.SingleLine );
			}
		}

		/// <summary>
		/// Gets or sets whether characters entered in the text box are masked. Has no effect when there is more than one row in the text box.
		/// </summary>
		public bool MasksCharacters {
			get { return masksCharacters; }
			set {
				masksCharacters = value;
				if( Rows == 1 )
					textBox.TextMode = masksCharacters ? TextBoxMode.Password : TextBoxMode.SingleLine;
			}
		}

		/// <summary>
		/// Sets this text box up for AJAX autofilling.
		/// </summary>
		public void SetupAutoFill( WebMethodDefinition webMethodDefinition, AutoFillOptions option ) {
			this.webMethodDefinition = webMethodDefinition;
			autoFillOption = option;
		}

		/// <summary>
		/// Allows for adding custom JavaScript event scripts to the text box.
		/// </summary>
		public void AddJavaScriptEventScript( string jsEventConstant, string script ) {
			textBox.AddJavaScriptEventScript( jsEventConstant, script );
		}

		/// <summary>
		/// Assigns this EwfTextBox to submit the given PostBackButton. This will disable the button's submit behavior. Do not pass null. This method will have no
		/// effect if there is a post back handler for this text box.
		/// NOTE: This should probably eventually work with other controls as an extension method.
		/// </summary>
		public void SetDefaultSubmitButton( PostBackButton pbb ) {
			defaultSubmitButton = pbb;
			// NOTE: Would it make sense to have support for ControlTreeDataLoader.AddLoadDataAction( theCodeThatJustGotMoved) so that the code appears in the method
			// it supports instead of getting moved to a big switch statement in LoadData?
			// Yes it would. I think we should make ControlTreeDataLoader an abstract class with an AddLoadDataAction method, a member containing all of the actions, and then have
			// the place currently calling load data call all of the load data methods (or have ControlTreeDataLoader somehow handle it for it).
			// This will eliminate a lot of member variables in controls and a lot of switch statements in LoadData methods.
		}

		/// <summary>
		/// The given text will be shown by default and will vanish when the text box gains focus. If focus is lost again, the text will reappear. This method has
		/// no effect if the text box has a non empty value. Do not pass null for text.
		/// </summary>
		public void SetWatermarkText( string text ) {
			watermarkText = text;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			var value = AppRequestState.Instance.EwfPageRequestState.PostBackValues.GetValue( this );
			textBox.Text = watermarkText.Any() && !value.Any() ? watermarkText : value;

			if( watermarkText.Any() ) {
				textBox.AddJavaScriptEventScript( JsWritingMethods.onfocus, "if( value == '" + watermarkText + "' ) value = ''" );
				textBox.AddJavaScriptEventScript( JsWritingMethods.onblur, "if( value == '' ) value = '" + watermarkText + "'" );
				EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( GetType(),
				                                                         UniqueID + "watermark",
				                                                         "$( '#" + TextBoxClientId + "' ).filter( function() { return this.value == '" + watermarkText +
				                                                         "'; } ).val( '' )" );
			}

			if( postBackHandler != null || defaultSubmitButton != null )
				EwfPage.Instance.MakeControlPostBackOnEnter( this, postBackHandler != null ? this as Control : defaultSubmitButton );

			if( webMethodDefinition != null ) {
				var autoCompleteExtender = new AutoCompleteExtender();
				EwfPage.Instance.DisableAutofillOnForm();
				autoCompleteExtender.TargetControlID = textBox.ID;
				autoCompleteExtender.ServicePath = webMethodDefinition.WebServicePath;
				autoCompleteExtender.ServiceMethod = webMethodDefinition.WebMethodName;
				autoCompleteExtender.CompletionListCssClass = CssElementCreator.CompletionListCssClass;
				autoCompleteExtender.CompletionListItemCssClass = "autocomplete_listItem";
				autoCompleteExtender.CompletionListHighlightedItemCssClass = CssElementCreator.CompletionListItemSelectedStateClass;
				autoCompleteExtender.CompletionSetCount = 10;
				autoCompleteExtender.MinimumPrefixLength = 2;
				autoCompleteExtender.CompletionInterval = 250;

				if( autoFillOption == AutoFillOptions.PostBackOnTextChangeAndItemSelect || autoFillOption == AutoFillOptions.PostBackOnItemSelect ) {
					if( autoFillOption == AutoFillOptions.PostBackOnTextChangeAndItemSelect )
						textBox.AutoPostBack = true;
					autoCompleteExtender.OnClientItemSelected = "function() {" + PostBackButton.GetPostBackScript( this, postBackHandler != null ) + "; }";
				}

				Controls.Add( autoCompleteExtender );
			}

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), textBox );

			if( preventAutoComplete )
				textBox.Attributes.Add( "autocomplete", "off" );

			if( suggestSpellCheck.HasValue )
				textBox.Attributes.Add( "spellcheck", suggestSpellCheck.Value.ToString().ToLower() );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			if( !watermarkText.Any() )
				return "";

			// NOTE: When we remove the ScriptManager, browsers will start using the bfcache for our pages. At this point we should move this logic to the
			// onpagehide event.
			return "$( '#" + TextBoxClientId + "' ).filter( function() { return this.value == ''; } ).val( '" + watermarkText + "' );";
		}

		void FormControl.AddPostBackValueToDictionary( PostBackValueDictionary postBackValues ) {
			postBackValues.Add( this, textBox.Text );
		}

		/// <summary>
		/// Gets the post back value.
		/// </summary>
		public string GetPostBackValue( PostBackValueDictionary postBackValues ) {
			return postBackValues.GetValue( this );
		}

		/// <summary>
		/// Returns true if the value changed on this post back.
		/// </summary>
		public bool ValueChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return postBackValues.ValueChangedOnPostBack( this );
		}

		void IPostBackEventHandler.RaisePostBackEvent( string eventArgument ) {
			if( postBackHandler != null )
				postBackHandler( GetPostBackValue( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) );
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