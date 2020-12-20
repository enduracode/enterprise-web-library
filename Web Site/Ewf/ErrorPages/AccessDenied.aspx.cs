using System.Linq;
using Tewl.Tools;

// Parameter: bool showHomeLink

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class AccessDenied: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource => true;
			protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
		}

		protected override PageContent getContent() {
			var content = new ErrorPageContent(
				new Paragraph( Translation.AccessIsDenied.ToComponents() ).Concat(
						info.ShowHomeLink
							? new Paragraph(
									new EwfHyperlink(
										new ExternalResource( NetTools.HomeUrl ),
										new StandardHyperlinkStyle( Translation.ClickHereToGoToHomePage ) ).ToCollection() )
								.ToCollection()
							: Enumerable.Empty<FlowComponent>() )
					.Materialize() );

			Response.StatusCode = 403;
			Response.TrySkipIisCustomErrors = true;

			return content;
		}
	}
}