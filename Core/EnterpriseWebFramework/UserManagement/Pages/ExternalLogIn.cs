using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;

// EwlPage
// Parameter: string provider
// Parameter: ? returnUrl
partial class ExternalLogIn {
	private IdentityProvider identityProvider = null!;

	protected override void init() {
		identityProvider = AuthenticationStatics.CustomIdentityProviders.Single( i => string.Equals( i.Identifier, Provider, StringComparison.Ordinal ) );
	}

	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() =>
		new UiPageContent(
			pageLoadPostBack: PostBack.CreateFull(
				actionGetter: () => new PostBackAction(
					new TrustedExternalResource(
						new ExternalResource(
							( (CustomIdentityProvider)identityProvider ).GetLogInPage( new AuthenticateWithExternalCode( Provider, ReturnUrl ).GetUrl() ) ) ) ) ) );
}