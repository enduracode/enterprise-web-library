using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;
using ComponentSpace.Saml2;
using ComponentSpace.Saml2.Certificates;
using ComponentSpace.Saml2.Configuration;
using ComponentSpace.Saml2.Exceptions;
using ComponentSpace.Saml2.Metadata;
using ComponentSpace.Saml2.Metadata.Export;
using ComponentSpace.Saml2.Metadata.Import;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.SystemSpecificLogic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseWebLibrary.Saml;

public class SamlProvider: ExternalSamlProvider {
	private static string? samlConfigurationName;
	private static Func<string>? certificateGetter;
	private static string? certificatePassword;

	private static Func<IServiceProvider>? currentServicesGetter;
	private static SystemProviderReference<AppSamlProvider>? provider;
	private static Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>>? samlIdentityProviderGetter;

	private class CustomizedCertificateManager: ICertificateManager {
		private class CustomizedOptions: IOptionsMonitor<CertificateValidationOptions> {
			private readonly CertificateValidationOptions options;

			internal CustomizedOptions( CertificateValidationOptions options ) {
				this.options = new CertificateValidationOptions
					{
						EnableNotBeforeCheck = options.EnableNotBeforeCheck,
						EnableNotAfterCheck = false, // allow expired certificates when they are self-signed or chain checking is disabled
						EnableChainCheck = options.EnableChainCheck,
						RevocationMode = options.RevocationMode,
						RevocationFlag = options.RevocationFlag,
						VerificationFlags = options.VerificationFlags,
						UrlRetrievalTimeout = options.UrlRetrievalTimeout
					};
			}

			CertificateValidationOptions IOptionsMonitor<CertificateValidationOptions>.CurrentValue => options;

			CertificateValidationOptions IOptionsMonitor<CertificateValidationOptions>.Get( string? name ) => throw new NotImplementedException();

			IDisposable IOptionsMonitor<CertificateValidationOptions>.OnChange( Action<CertificateValidationOptions, string?> listener ) =>
				throw new NotImplementedException();
		}

		private readonly CertificateManager standardManager;
		private readonly CertificateManager customizedManager;

		public CustomizedCertificateManager(
			ICertificateLoader certificateLoader, CertificateValidators certificateValidators, ILoggerFactory loggerFactory,
			IEnumerable<ICertificateValidator> validators, IOptionsMonitor<CertificateValidationOptions> options ) {
			standardManager = new CertificateManager( certificateLoader, certificateValidators, loggerFactory );
			customizedManager = new CertificateManager(
				certificateLoader,
				new CertificateValidators(
					validators.Select( i =>
							i.GetType() == typeof( CertificateValidator ) ? new CertificateValidator( new CustomizedOptions( options.CurrentValue ), loggerFactory ) : i )
						.Materialize() ),
				loggerFactory );
		}

		Task<IList<X509Certificate2>> ICertificateManager.GetLocalIdentityProviderSignatureCertificatesAsync(
			LocalIdentityProviderConfiguration localIdentityProviderConfiguration, PartnerServiceProviderConfiguration partnerServiceProviderConfiguration ) =>
			standardManager.GetLocalIdentityProviderSignatureCertificatesAsync( localIdentityProviderConfiguration, partnerServiceProviderConfiguration );

		Task<IList<X509Certificate2>> ICertificateManager.GetLocalServiceProviderSignatureCertificatesAsync(
			LocalServiceProviderConfiguration localServiceProviderConfiguration, PartnerIdentityProviderConfiguration partnerIdentityProviderConfiguration ) =>
			standardManager.GetLocalServiceProviderSignatureCertificatesAsync( localServiceProviderConfiguration, partnerIdentityProviderConfiguration );

		Task<IList<X509Certificate2>> ICertificateManager.GetLocalIdentityProviderEncryptionCertificatesAsync(
			LocalIdentityProviderConfiguration localIdentityProviderConfiguration, PartnerServiceProviderConfiguration partnerServiceProviderConfiguration ) =>
			standardManager.GetLocalIdentityProviderEncryptionCertificatesAsync( localIdentityProviderConfiguration, partnerServiceProviderConfiguration );

		Task<IList<X509Certificate2>> ICertificateManager.GetLocalServiceProviderEncryptionCertificatesAsync(
			LocalServiceProviderConfiguration localServiceProviderConfiguration, PartnerIdentityProviderConfiguration partnerIdentityProviderConfiguration ) =>
			standardManager.GetLocalServiceProviderEncryptionCertificatesAsync( localServiceProviderConfiguration, partnerIdentityProviderConfiguration );

		Task<IList<X509Certificate2>> ICertificateManager.GetPartnerIdentityProviderSignatureCertificatesAsync(
			PartnerIdentityProviderConfiguration partnerIdentityProviderConfiguration ) =>
			customizedManager.GetPartnerIdentityProviderSignatureCertificatesAsync( partnerIdentityProviderConfiguration );

		Task<IList<X509Certificate2>> ICertificateManager.GetPartnerServiceProviderSignatureCertificatesAsync(
			PartnerServiceProviderConfiguration partnerServiceProviderConfiguration ) =>
			standardManager.GetPartnerServiceProviderSignatureCertificatesAsync( partnerServiceProviderConfiguration );

		Task<IList<X509Certificate2>> ICertificateManager.GetPartnerIdentityProviderEncryptionCertificatesAsync(
			PartnerIdentityProviderConfiguration partnerIdentityProviderConfiguration ) =>
			standardManager.GetPartnerIdentityProviderEncryptionCertificatesAsync( partnerIdentityProviderConfiguration );

		Task<IList<X509Certificate2>> ICertificateManager.GetPartnerServiceProviderEncryptionCertificatesAsync(
			PartnerServiceProviderConfiguration partnerServiceProviderConfiguration ) =>
			standardManager.GetPartnerServiceProviderEncryptionCertificatesAsync( partnerServiceProviderConfiguration );
	}

	void ExternalSamlProvider.InitStatics( Func<string> certificateGetter, string certificatePassword ) {
		samlConfigurationName = EwlStatics.EwlInitialism.EnglishToCamel() + "UserManagement";
		SamlProvider.certificateGetter = certificateGetter;
		SamlProvider.certificatePassword = certificatePassword;
	}

	void ExternalSamlProvider.RegisterDependencyInjectionServices( IServiceCollection services ) {
		services.AddSaml();
		services.AddTransient<ICertificateImporter, CertificateStringImporter>();
		services.AddTransient<ICertificateManager, CustomizedCertificateManager>();
	}

	void ExternalSamlProvider.InitAppStatics(
		Func<IServiceProvider> currentServicesGetter, SystemProviderGetter providerGetter,
		Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>> samlIdentityProviderGetter ) {
		SamlProvider.currentServicesGetter = currentServicesGetter;
		provider = providerGetter.GetProvider<AppSamlProvider>( "Saml" );
		SamlProvider.samlIdentityProviderGetter = samlIdentityProviderGetter;
	}

	void ExternalSamlProvider.InitAppSpecificLogicDependencies() {
		configureSaml();
	}

	void ExternalSamlProvider.RefreshConfiguration() {
		configureSaml();
	}

	private void configureSaml() {
		var configurations = new List<SamlConfiguration>();

		var samlIdentityProviders = samlIdentityProviderGetter!();
		if( samlIdentityProviders.Any() ) {
			var metadataImporter = currentServicesGetter!().GetRequiredService<IMetadataToConfiguration>();
			configurations.Add(
				new SamlConfiguration
					{
						Name = samlConfigurationName,
						LocalServiceProviderConfiguration =
							new LocalServiceProviderConfiguration
								{
									Name = EnterpriseWebFramework.UserManagement.SamlResources.Metadata.GetInfo().GetUrl( disableAuthorizationCheck: true ),
									AssertionConsumerServiceUrl = EnterpriseWebFramework.UserManagement.SamlResources.Assertions.GetInfo().GetUrl(),
									LocalCertificates = new List<Certificate> { new() { String = certificateGetter!(), Password = certificatePassword } },
									ResolveToHttps = false
								},
						PartnerIdentityProviderConfigurations = samlIdentityProviders.Select( identityProvider =>
								( EntitiesDescriptor.IsValid( identityProvider.metadata )
									  ? metadataImporter.Import( new EntitiesDescriptor( identityProvider.metadata ) )
									  : metadataImporter.Import( new EntityDescriptor( identityProvider.metadata ) ) ).PartnerIdentityProviderConfigurations.Single( i =>
									string.Equals( i.Name, identityProvider.entityId, StringComparison.Ordinal ) ) )
							.ToImmutableArray()
					} );
		}

		var appProvider = provider!.GetProvider( returnNullIfNotFound: true );
		if( appProvider != null )
			configurations.AddRange( appProvider.GetCustomConfigurations() );

		currentServicesGetter!().GetRequiredService<IOptionsMonitor<SamlConfigurations>>().CurrentValue.Configurations = configurations;
	}

	async Task<XmlElement> ExternalSamlProvider.GetMetadata() =>
		( await currentServicesGetter!().GetRequiredService<IConfigurationToMetadata>().ExportAsync( configurationName: samlConfigurationName ) ).ToXml();

	async Task ExternalSamlProvider.WriteLogInResponse( string identityProvider, bool forceReauthentication, string returnUrl ) {
		var samlServiceProvider = currentServicesGetter!().GetRequiredService<ISamlServiceProvider>();
		await samlServiceProvider.SetConfigurationNameAsync( samlConfigurationName );
		await samlServiceProvider.InitiateSsoAsync(
			partnerName: identityProvider,
			relayState: returnUrl,
			ssoOptions: new SsoOptions { ForceAuthn = forceReauthentication } );
	}

	async Task<( string identityProvider, string userName, IReadOnlyDictionary<string, string> attributes, string returnUrl )?> ExternalSamlProvider.
		ReadAssertion() {
		var samlServiceProvider = currentServicesGetter!().GetRequiredService<ISamlServiceProvider>();
		await samlServiceProvider.SetConfigurationNameAsync( samlConfigurationName );

		ISpSsoResult result;
		try {
			result = await samlServiceProvider.ReceiveSsoAsync();
		}
		catch( SamlProtocolException e ) when( e.Message.Contains( "An SP-initiated SAML response from", StringComparison.Ordinal ) &&
		                                       e.Message.Contains( "was received unexpectedly", StringComparison.Ordinal ) ) {
			return null;
		}

		return ( result.PartnerName, result.UserID,
			       result.Attributes.Where( i => i.AttributeValues.Any() && i.AttributeValues.First().Data != null )
				       .ToImmutableDictionary( i => i.Name, i => i.AttributeValues.First().ToString() ), result.RelayState );
	}
}