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

		private readonly PageName pageName;

		/// <summary>
		/// Creates a page path control.
		/// </summary>
		public PagePath( PagePathCurrentPageBehavior currentPageBehavior = PagePathCurrentPageBehavior.IncludeCurrentPage ) {
			if( currentPageBehavior != PagePathCurrentPageBehavior.ExcludeCurrentPage ) {
				pageName = new PageName
					{
						ExcludesPageNameIfEntitySetupExists = currentPageBehavior == PagePathCurrentPageBehavior.IncludeCurrentPageAndExcludePageNameIfEntitySetupExists
					};
			}
		}

		/// <summary>
		/// Returns true if this control will not display any content.
		/// </summary>
		public bool IsEmpty { get { return EwfPage.Instance.InfoAsBaseType.PagePath.Count() == 1 && ( pageName == null || pageName.IsEmpty ); } }

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.CssClass );
			var pagePath = EwfPage.Instance.InfoAsBaseType.PagePath;
			foreach( var page in pagePath.Take( pagePath.Count - 1 ) ) {
				Controls.Add( EwfLink.Create( page, new ButtonActionControlStyle( page.PageFullName, buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) ) );
				Controls.Add( PageInfo.PagePathSeparator.GetLiteralControl() );
			}
			if( pageName != null )
				Controls.Add( pageName );
			else if( Controls.Count > 0 )
				Controls.RemoveAt( Controls.Count - 1 );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}