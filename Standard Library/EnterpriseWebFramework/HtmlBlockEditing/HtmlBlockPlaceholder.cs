using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
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

		private readonly string html;

		/// <summary>
		/// Creates an HTML block placeholder.
		/// </summary>
		public HtmlBlockPlaceholder( int htmlBlockId ): this( HtmlBlockStatics.GetHtml( htmlBlockId ) ) {}

		/// <summary>
		/// Creates an HTML block placeholder. Do not pass null for HTML. This overload is useful when you've already loaded the HTML.
		/// </summary>
		public HtmlBlockPlaceholder( string html ) {
			this.html = html;
		}

		/// <summary>
		/// Gets whether the HTML block has HTML (i.e. is not empty).
		/// </summary>
		public bool HasHtml { get { return html.Any(); } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			Controls.Add( new Literal { Text = html } );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}