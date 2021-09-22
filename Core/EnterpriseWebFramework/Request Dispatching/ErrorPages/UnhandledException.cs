using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	partial class UnhandledException {
		protected internal override bool IsIntermediateInstallationPublicResource => true;
		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override PageContent getContent() => new ErrorPageContent( new Paragraph( Translation.AnErrorHasOccurred.ToComponents() ).ToCollection() );
	}
}