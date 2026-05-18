using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;

// EwlResource
// Parameter: string provider
// Parameter: ? returnUrl
partial class AuthenticateWithExternalCode {
	private IdentityProvider identityProvider = null!;

	protected override void init() {
		identityProvider = AuthenticationStatics.CustomIdentityProviders.Single( i => string.Equals( i.Identifier, Provider, StringComparison.Ordinal ) );
	}

	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override bool disablesUrlNormalization => true;

	protected override ExternalRedirect getRedirect() {
		bool? authenticationSuccessful = null;
		ExecuteDataModificationMethod( () => {
			var user = ( (CustomIdentityProvider)identityProvider ).LogInUser( EwfRequest.Current!.AspNetRequest.Query );
			authenticationSuccessful = user is not null;
			if( authenticationSuccessful.Value )
				AuthenticationStatics.SetFormsAuthCookieAndUser( user!, identityProvider: identityProvider );
			else
				AuthenticationStatics.SetUserLastIdentityProvider( identityProvider );

			AuthenticationStatics.SetTestCookie();
		} );

		return new ExternalRedirect(
			new ExternalResource(
				new VerifyClientFunctionality(
					ReturnUrl?.TryGetResource( out _ ) == true
						? ReturnUrl
						: ( authenticationSuccessful!.Value
							    ? AuthenticationStatics.AppProvider.GetAuthenticatedUserHomeResource()
							    : AuthenticationStatics.GetDefaultLogInPage( null ) ).ToTrustedUrl() ).GetUrl() ),
			false );
	}
}