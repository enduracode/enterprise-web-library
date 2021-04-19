using System.Linq;
using System.Web;
using Tewl.Tools;

// EwlPage
// Parameter: bool showHomeLink

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	partial class AccessDenied {
		protected internal override bool IsIntermediateInstallationPublicResource => true;
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override PageContent getContent() {
			var content = new ErrorPageContent(
				new Paragraph( Translation.AccessIsDenied.ToComponents() ).Concat(
						ShowHomeLink
							? new Paragraph(
									new EwfHyperlink(
										new ExternalResource( NetTools.HomeUrl ),
										new StandardHyperlinkStyle( Translation.ClickHereToGoToHomePage ) ).ToCollection() )
								.ToCollection()
							: Enumerable.Empty<FlowComponent>() )
					.Materialize() );

			HttpContext.Current.Response.StatusCode = 403;
			HttpContext.Current.Response.TrySkipIisCustomErrors = true;

			return content;
		}
	}
}