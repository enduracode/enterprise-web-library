namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages;

// EwlPage
// Parameter: bool showHomeLink
partial class ResourceNotAvailable {
	protected internal override bool IsIntermediateInstallationPublicResource => true;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

	protected override PageContent getContent() =>
		new ErrorPageContent(
			new Paragraph( Translation.ThePageYouRequestedIsNotAvailable.ToComponents() ).Concat(
					ShowHomeLink
						? new Paragraph(
							new EwfHyperlink(
								EwfConfigurationStatics.GetDefaultBaseResource().ToHyperlinkDefaultBehavior( disableAuthorizationCheck: true ),
								new StandardHyperlinkStyle( Translation.ClickHereToGoToHomePage ) ).ToCollection() ).ToCollection()
						: Enumerable.Empty<FlowComponent>() )
				.Materialize() );
}