using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A control for editing an HTML block.
	/// </summary>
	public class HtmlBlockEditor: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfHtmlBlockEditor";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "HtmlBlockEditor", "div." + CssClass ) };
			}
		}

		private readonly string ckEditorConfiguration;
		private readonly HtmlBlockEditorModification mod;
		private WysiwygHtmlEditor wysiwygEditor;

		/// <summary>
		/// Creates an HTML block editor.
		/// </summary>
		/// <param name="htmlBlockId"></param>
		/// <param name="idSetter"></param>
		/// <param name="mod"></param>
		/// <param name="ckEditorConfiguration">Do not pass null.</param>
		public HtmlBlockEditor( int? htmlBlockId, Action<int> idSetter, out HtmlBlockEditorModification mod, string ckEditorConfiguration = "" ) {
			this.ckEditorConfiguration = ckEditorConfiguration;
			this.mod = mod = new HtmlBlockEditorModification( htmlBlockId, htmlBlockId.HasValue ? HtmlBlockStatics.GetHtml( htmlBlockId.Value ) : "", idSetter );
		}

		/// <summary>
		/// Gets whether this HTML block has HTML (i.e. is not empty).
		/// </summary>
		public bool HasHtml { get { return mod.Html.Any(); } }

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			this.AddControlsReturnThis( wysiwygEditor = new WysiwygHtmlEditor( mod.Html, ckEditorConfiguration: ckEditorConfiguration ) );
		}

		/// <summary>
		/// Validates the HTML.
		/// </summary>
		public void Validate( PostBackValueDictionary postBackValues, Validator validator, ValidationErrorHandler errorHandler ) {
			mod.Html = validator.GetString( errorHandler, wysiwygEditor.GetPostBackValue( postBackValues ), true );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}