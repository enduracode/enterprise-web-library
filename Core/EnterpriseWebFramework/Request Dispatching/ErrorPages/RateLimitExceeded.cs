namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages;

// EwlPage
// Parameter: string requestSource
partial class RateLimitExceeded {
	protected internal override bool IsIntermediateInstallationPublicResource => true;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

	protected override PageContent getContent() =>
		new ErrorPageContent( new Paragraph( $"The rate limit was exceeded for {RequestSource}.".ToComponents() ).ToCollection() );
}