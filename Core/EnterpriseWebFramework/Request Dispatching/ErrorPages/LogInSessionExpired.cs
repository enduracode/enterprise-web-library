// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages;

partial class LogInSessionExpired {
	protected internal override bool IsIntermediateInstallationPublicResource => true;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() =>
		new ErrorPageContent( new Paragraph( "Your log-in attempt took too long. Please try logging in again.".ToComponents() ).ToCollection() );
}