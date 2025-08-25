using System.Threading.Tasks;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.SamlResources;

// EwlResource
// Parameter: string provider
// Parameter: ? returnUrl
partial class LogIn {
	private SamlIdentityProvider identityProvider = null!;

	protected override void init() {
		identityProvider = AuthenticationStatics.SamlIdentityProviders.Single( i => string.Equals( i.EntityId, Provider, StringComparison.Ordinal ) );
	}

	protected override UrlHandler getUrlParent() => new Metadata();

	protected override EwfSafeRequestHandler getOrHead() =>
		new EwfSafeResponseWriter(
			EwfResponse.CreateFromAspNetResponse( _ => Task.Run( async () => await ExternalFunctionalityStatics.ExternalSamlProvider.WriteLogInResponse(
				                                                                 identityProvider.EntityId,
				                                                                 identityProvider == AuthenticationStatics.GetUserLastIdentityProvider(),
				                                                                 ReturnUrl is null
					                                                                 ? ""
					                                                                 : TrustedUrl.Serialize(
						                                                                 ReturnUrl,
						                                                                 EwfConfigurationStatics.AppConfiguration.PublicId ) ) )
				.Wait() ) );
}