using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control for editing an HTML block either by using a browser-based WYSIWYG HTML editor or downloading it as a file, modifying it, and uploading it back
	/// into the system.
	/// </summary>
	public class HtmlBlockEditor: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfHtmlBlockEditor";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "HtmlBlockEditor", "div." + CssClass ) };
			}
		}

		private const string internalId = "internal";
		private const string simpleId = "simple";

		private int? htmlBlockId;
		private WysiwygHtmlEditor wysiwygEditor;
		private EwfTextBox textBox;
		private EwfListControl editModes;
		private string html = "";

		/// <summary>
		/// Creates an HTML block editor.
		/// </summary>
		public HtmlBlockEditor( int? htmlBlockId ) {
			this.htmlBlockId = htmlBlockId;
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public HtmlBlockEditor(): this( null ) {}

		/// <summary>
		/// Call this during LoadData.  Returns true if this html block has html (not empty).
		/// AbsoluteCssUrl is the URL of the style sheet that can be used by the HTML block.
		/// </summary>
		public bool LoadData( DBConnection cn ) {
			if( htmlBlockId.HasValue )
				html = HtmlBlockStatics.GetHtml( cn, htmlBlockId.Value );
			return html.Length > 0;
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public bool LoadData( DBConnection cn, int? htmlBlockId ) {
			this.htmlBlockId = htmlBlockId;
			return LoadData( cn );
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			wysiwygEditor = new WysiwygHtmlEditor( html );
			textBox = new EwfTextBox( html ) { Rows = 20 };

			// This block control is necessary because the CKEditor hides the textarea and creates a sibling control immediately after it. We want display linking to
			// affect both the textarea and the sibling.
			// NOTE: It might be better for the block control to be part of the WYSIWYG editor since right now, when the editor is used on its own, display linking
			// may not work.
			var wysiwygEditorBlock = new Block( wysiwygEditor );

			editModes = new EwfListControl();
			editModes.Type = EwfListControl.ListControlType.HorizontalRadioButton;
			editModes.AddItem( "The built-in editor", internalId );
			editModes.AddItem( "A simple text box", simpleId );
			editModes.AddDisplayLink( internalId, true, wysiwygEditorBlock );
			editModes.AddDisplayLink( simpleId, true, textBox );

			var modeSelection = new Panel().AddControlsReturnThis( new LiteralControl( "View and edit HTML with:&nbsp;&nbsp;" ), editModes );
			modeSelection.Style.Add( "margin-bottom", "4px" );

			this.AddControlsReturnThis( modeSelection, wysiwygEditorBlock, textBox );
		}

		/// <summary>
		/// Call this during ValidateFormValues. Passing true for destroy HTML will ignore any text or uploaded file and use the empty string for html.
		/// For the extra validation method, the string passed will be the html contents and the html must be returned.
		/// Returns the validated HTML.
		/// </summary>
		public string ValidateFormValues( PostBackValueDictionary postBackValues, Validator validator, bool destroyHtml = false,
		                                  Func<Validator, string, string> extraValidation = null ) {
			if( destroyHtml )
				html = "";
			else if( editModes.Value == internalId )
				html = wysiwygEditor.GetPostBackValue( postBackValues );
			else if( editModes.Value == simpleId )
				html = textBox.GetPostBackValue( postBackValues );

			html = validator.GetString( new ValidationErrorHandler( "html" ), html, true );
			if( extraValidation != null )
				html = extraValidation( validator, html );

			// Do this after extraValidation so that extraValidation doesn't get confused by our app-relative URL prefix "merge fields". We have seen a system run
			// into problems while using extraValidation to verify that all words preceded by @@ were valid system-specific merge fields; it was mistakenly picking up
			// our app-relative prefixes, thinking that they were merge fields, and complaining that they were not valid.
			html = HtmlBlockStatics.EncodeIntraSiteUris( html );

			return html;
		}

		/// <summary>
		/// Do not use.
		/// </summary>
		public string ValidateFormValues( Validator validator, bool destroyHtml = false, Func<Validator, string, string> extraValidation = null ) {
			return ValidateFormValues( AppRequestState.Instance.EwfPageRequestState.PostBackValues, validator, destroyHtml, extraValidation );
		}

		/// <summary>
		/// Call this during ModifyData. Returns ID of the created or modified HTML block.
		/// </summary>
		public int ModifyData( DBConnection cn ) {
			var setup = EwfApp.Instance as HtmlBlockEditingSetup;
			if( htmlBlockId == null )
				htmlBlockId = setup.InsertHtmlBlock( cn, html );
			else
				setup.UpdateHtml( cn, htmlBlockId.Value, html );
			return htmlBlockId.Value;
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}