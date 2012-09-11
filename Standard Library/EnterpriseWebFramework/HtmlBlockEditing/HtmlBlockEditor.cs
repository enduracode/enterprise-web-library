using System;
using System.Linq;
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

		private readonly HtmlBlockEditorModification mod;
		private WysiwygHtmlEditor wysiwygEditor;
		private EwfTextBox textBox;
		private EwfListControl editModes;

		/// <summary>
		/// Creates an HTML block editor.
		/// </summary>
		public HtmlBlockEditor( int? htmlBlockId, Action<int> idSetter, out HtmlBlockEditorModification mod ) {
			this.mod =
				mod =
				new HtmlBlockEditorModification( htmlBlockId,
				                                 htmlBlockId.HasValue ? HtmlBlockStatics.GetHtml( AppRequestState.PrimaryDatabaseConnection, htmlBlockId.Value ) : "",
				                                 idSetter );
		}

		/// <summary>
		/// Gets whether this HTML block has HTML (i.e. is not empty).
		/// </summary>
		public bool HasHtml { get { return mod.Html.Any(); } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );

			wysiwygEditor = new WysiwygHtmlEditor( mod.Html );
			textBox = new EwfTextBox( mod.Html ) { Rows = 20 };

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
		/// Validates the HTML.
		/// </summary>
		public void Validate( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler ) {
			mod.Html = validator.GetString( errorHandler,
			                                editModes.Value == internalId ? wysiwygEditor.GetPostBackValue( postBackValues ) : textBox.GetPostBackValue( postBackValues ),
			                                true );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}