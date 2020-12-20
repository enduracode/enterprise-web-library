using Tewl.Tools;

// Parameter: string dummy

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class UnhandledException: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource => true;
			protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
		}

		protected override PageContent getContent() {
			var content = new ErrorPageContent( new Paragraph( Translation.AnErrorHasOccurred.ToComponents() ).ToCollection() );

			Response.StatusCode = 500;
			Response.TrySkipIisCustomErrors = true;

			return content;
		}
	}
}