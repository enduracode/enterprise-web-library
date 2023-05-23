namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider.Resources;

partial class EntitySetup {
	protected override ResourceParent createParent() => null;

	protected override string getEntitySetupName() => "OpenID Provider";

	protected override AlternativeResourceMode createAlternativeMode() =>
		OpenIdProviderStatics.OpenIdProviderEnabled ? null : new DisabledResourceMode( "The OpenID Provider is not enabled in this system." );

	public override ResourceBase DefaultResource => throw new NotSupportedException();

	protected override IEnumerable<ResourceGroup> createListedResources() => Enumerable.Empty<ResourceGroup>();

	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override UrlHandler getRequestHandler() => null;

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() => Keys.UrlPatterns.Literal( this, "jwks" ).ToCollection();
}