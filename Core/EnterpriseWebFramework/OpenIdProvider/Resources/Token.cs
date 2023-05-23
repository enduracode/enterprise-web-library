using EnterpriseWebLibrary.ExternalFunctionality;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider.Resources;

partial class Token {
	protected internal override bool IsIntermediateInstallationPublicResource => true;

	protected override EwfSafeRequestHandler getOrHead() =>
		new EwfSafeResponseWriter( EwfResponse.CreateFromAspNetMvcAction( ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.WriteTokenResponse ) );
}