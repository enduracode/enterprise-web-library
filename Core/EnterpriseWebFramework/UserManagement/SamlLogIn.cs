using System;
using System.Linq;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

// EwlResource
// Parameter: string provider
// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement {
	partial class SamlLogIn {
		private SamlIdentityProvider identityProvider;

		protected override void init() {
			identityProvider = AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals( i.EntityId, Provider, StringComparison.Ordinal ) );
		}

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override EwfSafeRequestHandler getOrHead() =>
			new EwfSafeResponseWriter(
				EwfResponse.CreateFromAspNetResponse(
					aspNetResponse => ExternalFunctionalityStatics.ExternalSamlProvider.WriteLogInResponse( aspNetResponse, identityProvider.EntityId, ReturnUrl ) ) );
	}
}