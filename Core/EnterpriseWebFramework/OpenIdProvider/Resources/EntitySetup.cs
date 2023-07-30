#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider.Resources;

partial class EntitySetup {
	protected override void init() {
		if( !OpenIdProviderStatics.OpenIdProviderEnabled )
			throw new Exception( "The OpenID Provider is not enabled in this system." );
	}

	protected override ResourceParent createParent() => null;

	protected override string getEntitySetupName() => "OpenID Provider";

	public override ResourceBase DefaultResource => throw new NotSupportedException();

	protected override IEnumerable<ResourceGroup> createListedResources() => Enumerable.Empty<ResourceGroup>();

	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override UrlHandler getRequestHandler() => null;

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
		Keys.UrlPatterns.Literal( this, "jwks" )
			.ToCollection()
			.Append( Authenticate.UrlPatterns.Literal( this, "authenticate" ) )
			.Append( Token.UrlPatterns.Literal( this, "token" ) )
			.Append( UserInfo.UrlPatterns.Literal( this, "userinfo" ) );
}