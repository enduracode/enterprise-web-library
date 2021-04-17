using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	partial class UnhandledException {
		protected internal override bool IsIntermediateInstallationPublicResource => true;
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override PageContent getContent() {
			var content = new ErrorPageContent( new Paragraph( Translation.AnErrorHasOccurred.ToComponents() ).ToCollection() );

			Response.StatusCode = 500;
			Response.TrySkipIisCustomErrors = true;

			return content;
		}
	}
}