using System.Linq;
using EnterpriseWebLibrary.WebSessionState;
using Tewl.Tools;

// Parameter: bool showHomeLink

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class PageNotAvailable: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource => true;
			protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
		}

		protected override PageContent getContent() {
			var content = new ErrorPageContent(
				new Paragraph(
					Translation.ThePageYouRequestedIsNotAvailable.ToComponents()
						.Concat(
							info.ShowHomeLink ? " ".ToComponents().Concat( Translation.YouWillBeSentToTheHomePage.ToComponents() ) : Enumerable.Empty<PhrasingComponent>() )
						.Materialize() ).ToCollection() );

			// NOTE: We can't set this code right now because it makes EwfApp.handleEndRequest think that this page couldn't be found.
			//Response.StatusCode = 404;

			Response.TrySkipIisCustomErrors = true;

			if( info.ShowHomeLink )
				StandardLibrarySessionState.Instance.SetTimedClientSideNavigation( NetTools.HomeUrl, 5 );

			return content;
		}
	}
}