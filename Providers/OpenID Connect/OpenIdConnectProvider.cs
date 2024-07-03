using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading.Tasks;
using ComponentSpace.OpenID;
using ComponentSpace.OpenID.Configuration;
using ComponentSpace.OpenID.Exceptions;
using ComponentSpace.OpenID.Messages;
using EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;
using EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider.Resources;
using EnterpriseWebLibrary.ExternalFunctionality;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseWebLibrary.OpenIdConnect;

public class OpenIdConnectProvider: ExternalOpenIdConnectProvider {
	private static Func<IServiceProvider>? currentServicesGetter;
	private static string? issuerIdentifier;
	private static Func<string>? certificateGetter;
	private static string? certificatePassword;
	private static Func<IEnumerable<OpenIdClient>>? clientGetter;

	void ExternalOpenIdConnectProvider.RegisterDependencyInjectionServices( IServiceCollection services ) {
		services.AddOpenIDProvider();
	}

	void ExternalOpenIdConnectProvider.InitAppStatics(
		Func<IServiceProvider> currentServicesGetter, string issuerIdentifier, Func<string> certificateGetter, string certificatePassword,
		Func<IEnumerable<OpenIdClient>> clientGetter ) {
		OpenIdConnectProvider.currentServicesGetter = currentServicesGetter;
		OpenIdConnectProvider.issuerIdentifier = issuerIdentifier;
		OpenIdConnectProvider.certificateGetter = certificateGetter;
		OpenIdConnectProvider.certificatePassword = certificatePassword;
		OpenIdConnectProvider.clientGetter = clientGetter;
	}

	void ExternalOpenIdConnectProvider.InitAppSpecificLogicDependencies() {
		configureOpenIdProvider();
	}

	void ExternalOpenIdConnectProvider.RefreshConfiguration() {
		configureOpenIdProvider();
	}

	private void configureOpenIdProvider() {
		var entitySetup = new EntitySetup();
		currentServicesGetter!().GetRequiredService<IOptionsMonitor<OpenIDConfigurations>>().CurrentValue.Configurations = new OpenIDConfiguration[]
			{
				new()
					{
						ProviderConfiguration = new ProviderConfiguration
							{
								ProviderMetadata = new ProviderMetadata
									{
										Issuer = issuerIdentifier,
										AuthorizationEndpoint = new Authenticate( entitySetup ).GetUrl( disableAuthorizationCheck: true ),
										TokenEndpoint = new Token( entitySetup ).GetUrl(),
										UserinfoEndpoint = new UserInfo( entitySetup ).GetUrl(),
										JwksUri = new Keys( entitySetup ).GetUrl(),
										ScopesSupported = new[] { "openid", "profile", "email" }
									},
								ProviderCertificates = new Certificate[] { new() { String = certificateGetter!(), Password = certificatePassword } }
							},
						ClientConfigurations = clientGetter!()
							.Select(
								i => new ClientConfiguration { Description = i.ClientName, ClientID = i.ClientIdentifier, RedirectUris = i.RedirectionUrls.ToArray() } )
							.ToArray()
					}
			};
	}

	Task<IActionResult> ExternalOpenIdConnectProvider.WriteMetadata() {
		var openIdProvider = currentServicesGetter!().GetRequiredService<IOpenIDProvider>();
		return openIdProvider.GetMetadataAsync();
	}

	Task<IActionResult> ExternalOpenIdConnectProvider.WriteKeys() {
		var openIdProvider = currentServicesGetter!().GetRequiredService<IOpenIDProvider>();
		return openIdProvider.GetKeysAsync();
	}

	bool ExternalOpenIdConnectProvider.ReadAuthenticationRequest( out string clientIdentifier ) {
		var openIdProvider = currentServicesGetter!().GetRequiredService<IOpenIDProvider>();

		AuthenticationRequest? request = null;
		Task.Run(
				async () => {
					try {
						request = await openIdProvider.ReceiveAuthnRequestAsync();
					}
					catch( OpenIDException ) {}
				} )
			.Wait();
		if( request is null ) {
			clientIdentifier = "";
			return false;
		}

		clientIdentifier = request.ClientID!;
		return true;
	}

	async Task<IActionResult> ExternalOpenIdConnectProvider.WriteAuthenticationResponse(
		string clientIdentifier, string subjectIdentifier, IEnumerable<( string name, string value )> additionalClaims ) {
		var openIdProvider = currentServicesGetter!().GetRequiredService<IOpenIDProvider>();
		return await openIdProvider.SendAuthnResponseAsync(
			       subjectIdentifier,
			       null,
			       accessToken: await openIdProvider.CreateJwtAccessTokenAsync(
				                    clientIdentifier,
				                    null!,
				                    subjectIdentifier,
				                    null,
				                    claims: additionalClaims.Select( i => new Claim( i.name, i.value ) ).ToImmutableArray() ) );
	}

	Task<IActionResult> ExternalOpenIdConnectProvider.WriteAuthenticationErrorResponse() {
		var openIdProvider = currentServicesGetter!().GetRequiredService<IOpenIDProvider>();
		return openIdProvider.SendAuthnErrorResponseAsync( OpenIDConstants.ErrorCodes.InvalidRequest );
	}

	Task<IActionResult> ExternalOpenIdConnectProvider.WriteTokenResponse() {
		var openIdProvider = currentServicesGetter!().GetRequiredService<IOpenIDProvider>();
		return openIdProvider.GetTokensAsync();
	}

	Task<IActionResult> ExternalOpenIdConnectProvider.WriteUserInfoResponse() {
		var openIdProvider = currentServicesGetter!().GetRequiredService<IOpenIDProvider>();
		return openIdProvider.GetUserInfoAsync();
	}
}