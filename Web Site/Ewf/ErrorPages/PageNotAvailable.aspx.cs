using RedStapler.StandardLibrary.WebSessionState;

// Parameter: bool showHomeLink

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class PageNotAvailable: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource { get { return true; } }
			protected override ConnectionSecurity ConnectionSecurity { get { return ConnectionSecurity.MatchingCurrentRequest; } }
		}

		protected override void loadData() {
			pageNotAvailable.Text = Translation.ThePageYouRequestedIsNotAvailable;
			homeLit.Text = " " + Translation.YouWillBeSentToTheHomePage;
			// NOTE: We can't set this code right now because it makes EwfApp.handleEndRequest think that this page couldn't be found.
			//Response.StatusCode = 404;

			Response.TrySkipIisCustomErrors = true;

			if( info.ShowHomeLink )
				StandardLibrarySessionState.Instance.SetTimedClientSideNavigation( NetTools.HomeUrl, 5 );
			else
				homeLit.Visible = false;
		}
	}
}