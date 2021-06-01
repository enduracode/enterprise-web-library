using System.Linq;
using System.Web;
using EnterpriseWebLibrary.WebSessionState;
using Tewl.Tools;

// EwlPage
// Parameter: bool showHomeLink

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	partial class ResourceNotAvailable {
		protected internal override bool IsIntermediateInstallationPublicResource => true;
		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override PageContent getContent() {
			var content = new ErrorPageContent(
				new Paragraph(
					Translation.ThePageYouRequestedIsNotAvailable.ToComponents()
						.Concat( ShowHomeLink ? " ".ToComponents().Concat( Translation.YouWillBeSentToTheHomePage.ToComponents() ) : Enumerable.Empty<PhrasingComponent>() )
						.Materialize() ).ToCollection() );

			HttpContext.Current.Response.StatusCode = 404;
			HttpContext.Current.Response.TrySkipIisCustomErrors = true;

			if( ShowHomeLink )
				StandardLibrarySessionState.Instance.SetTimedClientSideNavigation(
					EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ),
					5 );

			return content;
		}
	}
}