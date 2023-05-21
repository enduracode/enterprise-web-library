using ComponentSpace.OpenID.Configuration;
using EnterpriseWebLibrary.ExternalFunctionality;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnterpriseWebLibrary.OpenIdConnect;

public class OpenIdConnectProvider: ExternalOpenIdConnectProvider {
	private static Func<IServiceProvider> currentServicesGetter;
	private static string issuerIdentifier;
	private static Func<string> certificateGetter;
	private static string certificatePassword;

	void ExternalOpenIdConnectProvider.RegisterDependencyInjectionServices( IServiceCollection services ) {
		services.AddOpenIDProvider();
	}

	void ExternalOpenIdConnectProvider.InitAppStatics(
		Func<IServiceProvider> currentServicesGetter, string issuerIdentifier, Func<string> certificateGetter, string certificatePassword ) {
		OpenIdConnectProvider.currentServicesGetter = currentServicesGetter;
		OpenIdConnectProvider.issuerIdentifier = issuerIdentifier;
		OpenIdConnectProvider.certificateGetter = certificateGetter;
		OpenIdConnectProvider.certificatePassword = certificatePassword;
	}

	void ExternalOpenIdConnectProvider.InitAppSpecificLogicDependencies() {
		configureOpenIdProvider();
	}

	void ExternalOpenIdConnectProvider.RefreshConfiguration() {
		configureOpenIdProvider();
	}

	private void configureOpenIdProvider() {
		currentServicesGetter().GetRequiredService<IOptionsMonitor<OpenIDConfigurations>>().CurrentValue.Configurations = new OpenIDConfiguration[]
			{
				new()
					{
						ProviderConfiguration = new ProviderConfiguration
							{
								ProviderMetadata = new ProviderMetadata { Issuer = issuerIdentifier, ScopesSupported = new[] { "openid", "profile", "email" } },
								ProviderCertificates = new Certificate[] { new() { String = certificateGetter(), Password = certificatePassword } }
							},
						ClientConfigurations = new ClientConfiguration[] {}
					}
			};
	}
}