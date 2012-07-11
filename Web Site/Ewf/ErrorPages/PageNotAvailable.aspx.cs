using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.WebSessionState;

// Parameter: bool showHomeLink

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.ErrorPages {
	public partial class PageNotAvailable: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			protected override bool IsIntermediateInstallationPublicPage { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void LoadData( DBConnection cn ) {
			pageNotAvailable.Text = Translation.ThePageYouRequestedIsNotAvailable;
			homeLit.Text = " " + Translation.YouWillBeSentToTheHomePage;
			// NOTE: We can't set this code right now because it makes EwfApp.handleEndRequest think that this page couldn't be found.
			//Response.StatusCode = 404;

			Response.TrySkipIisCustomErrors = true;

			if( info.ShowHomeLink )
				StandardLibrarySessionState.Instance.SetTimedClientSideRedirect( NetTools.HomeUrl, 5 );
			else
				homeLit.Visible = false;
		}
	}
}