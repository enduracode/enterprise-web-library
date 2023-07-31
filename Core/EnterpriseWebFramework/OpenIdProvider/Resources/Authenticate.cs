using EnterpriseWebLibrary.ExternalFunctionality;

// EwlResource

namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider.Resources;

partial class Authenticate {
	private string clientIdentifier = null!;
	private Lazy<OpenIdAuthenticationResult?> result = null!;

	protected override void init() {
		result = new Lazy<OpenIdAuthenticationResult?>(
			() => ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.ReadAuthenticationRequest( out clientIdentifier )
				      ? OpenIdProviderStatics.AppProvider.AuthenticateUser( clientIdentifier )
				      : null );
	}

	protected override bool userCanAccess => result.Value is null || result.Value.ResponseWriter is not null;

	public override ResourceBase? LogInPage => result.Value?.LogInPage;

	protected override bool disablesUrlNormalization => true;

	protected override EwfSafeRequestHandler getOrHead() =>
		new EwfSafeResponseWriter(
			EwfResponse.CreateFromAspNetMvcAction(
				result.Value is null
					? ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.WriteAuthenticationErrorResponse
					: () => result.Value.ResponseWriter( clientIdentifier ) ) );
}