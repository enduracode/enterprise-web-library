using EnterpriseWebLibrary.ExternalFunctionality;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider.Resources;

partial class UserInfo {
	protected internal override bool IsIntermediateInstallationPublicResource => true;

	protected override EwfSafeRequestHandler getOrHead() =>
		new EwfSafeResponseWriter( EwfResponse.CreateFromAspNetMvcAction( ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.WriteUserInfoResponse ) );
}