using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

// Parameter: bool showHomeLink

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	public partial class AccessDenied: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			protected override bool IsIntermediateInstallationPublicPage { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void LoadData( DBConnection cn ) {
			accessDenied.InnerText = Translation.AccessIsDenied;

			if( info.ShowHomeLink ) {
				ph.AddControlsReturnThis(
					new Paragraph( EwfLink.Create( new ExternalPageInfo( NetTools.HomeUrl ), new TextActionControlStyle( Translation.ClickHereToGoToHomePage ) ) ) );
			}

			Response.StatusCode = 403;
			Response.TrySkipIisCustomErrors = true;
		}
	}
}