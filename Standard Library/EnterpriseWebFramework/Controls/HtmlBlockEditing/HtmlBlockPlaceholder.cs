using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control for displaying an HTML block on a page.
	/// </summary>
	public class HtmlBlockPlaceholder: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfHtmlBlock";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "HtmlBlock", "div." + CssClass ) };
			}
		}

		/// <summary>
		/// Does nothing. Overriding this method forces Visual Studio to respect white space around the control when it is used in markup.
		/// </summary>
		protected override void AddParsedSubObject( object obj ) {}

		/// <summary>
		/// Call this during LoadData. Returns true if there is content.
		/// </summary>
		public bool LoadData( DBConnection cn, int htmlBlockId ) {
			return LoadData( HtmlBlockStatics.GetHtml( cn, htmlBlockId ) );
		}

		/// <summary>
		/// Call this during LoadData. Returns true if there is content. This overload is useful when you've already loaded the HTML.
		/// </summary>
		public bool LoadData( string html ) {
			Controls.Add( new Literal { Text = html } );
			return !html.IsNullOrWhiteSpace();
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}