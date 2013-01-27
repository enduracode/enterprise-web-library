using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A current page behavior for a page path.
	/// </summary>
	public enum PagePathCurrentPageBehavior {
		/// <summary>
		/// Excludes the current page.
		/// </summary>
		ExcludeCurrentPage,

		/// <summary>
		/// Includes the current page.
		/// </summary>
		IncludeCurrentPage,

		/// <summary>
		/// Includes the current page, but excludes the page name if an entity setup exists.
		/// </summary>
		IncludeCurrentPageAndExcludePageNameIfEntitySetupExists
	}

	/// <summary>
	/// A block that displays the full path to the current page, optionally including the page's name as a first-level heading.
	/// </summary>
	public class PagePath: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfPagePath";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "PagePath", "div." + CssClass ) };
			}
		}

		/// <summary>
		/// Gets or sets the current page behavior.
		/// </summary>
		public PagePathCurrentPageBehavior CurrentPageBehavior { get; set; }

		/// <summary>
		/// Creates a page path control.
		/// </summary>
		public PagePath() {
			CurrentPageBehavior = PagePathCurrentPageBehavior.IncludeCurrentPage;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			var pagePath = EwfPage.Instance.InfoAsBaseType.PagePath;
			foreach( var page in pagePath.Take( pagePath.Count - 1 ) ) {
				Controls.Add( EwfLink.Create( page, new ButtonActionControlStyle( page.PageFullName, buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) ) );
				Controls.Add( PageInfo.PagePathSeparator.GetLiteralControl() );
			}
			if( CurrentPageBehavior != PagePathCurrentPageBehavior.ExcludeCurrentPage ) {
				Controls.Add( new PageName
					{
						ExcludesPageNameIfEntitySetupExists = CurrentPageBehavior == PagePathCurrentPageBehavior.IncludeCurrentPageAndExcludePageNameIfEntitySetupExists
					} );
			}
			else if( Controls.Count > 0 )
				Controls.RemoveAt( Controls.Count - 1 );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}