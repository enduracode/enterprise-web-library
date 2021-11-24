using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using System.Xml;
using ComponentSpace.SAML2;
using ComponentSpace.SAML2.Assertions;
using ComponentSpace.SAML2.Configuration;
using ComponentSpace.SAML2.Metadata;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.ExternalFunctionality;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Saml {
	public class SamlProvider: ExternalSamlProvider {
		private static string samlConfigurationName;
		private static Func<string> certificateGetter;
		private static string certificatePassword;

		private static SystemProviderReference<AppSamlProvider> provider;
		private static Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>> samlIdentityProviderGetter;

		void ExternalSamlProvider.InitStatics( Func<string> certificateGetter, string certificatePassword ) {
			samlConfigurationName = EwlStatics.EwlInitialism.EnglishToCamel() + "UserManagement";
			SamlProvider.certificateGetter = certificateGetter;
			SamlProvider.certificatePassword = certificatePassword;
		}

		void ExternalSamlProvider.InitAppStatics(
			SystemProviderGetter providerGetter, Func<IReadOnlyCollection<( XmlElement metadata, string entityId )>> samlIdentityProviderGetter ) {
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
			var configurations = new SAMLConfigurations();

			var samlIdentityProviders = samlIdentityProviderGetter();
			if( samlIdentityProviders.Any() ) {
				var userManagementConfiguration = new SAMLConfiguration { Name = samlConfigurationName };
				userManagementConfiguration.LocalServiceProviderConfiguration = new LocalServiceProviderConfiguration
					{
						Name = EnterpriseWebFramework.UserManagement.SamlResources.Metadata.GetInfo().GetUrl(),
						AssertionConsumerServiceUrl = EnterpriseWebFramework.UserManagement.SamlResources.Assertions.GetInfo().GetUrl(),
						LocalCertificates = new List<CertificateConfiguration>
							{
								new CertificateConfiguration { String = certificateGetter(), Password = certificatePassword }
							},
						ResolveToHttps = false
					};
				foreach( var identityProvider in samlIdentityProviders )
					userManagementConfiguration.AddPartnerIdentityProvider(
						( EntitiesDescriptor.IsValid( identityProvider.metadata )
							  ? MetadataImporter.ImportIdentityProviders( new EntitiesDescriptor( identityProvider.metadata ), null )
							  : MetadataImporter.ImportIdentityProviders( new EntityDescriptor( identityProvider.metadata ), null ) ).Single(
							i => string.Equals( i.Name, identityProvider.entityId, StringComparison.Ordinal ) ) );
				configurations.AddConfiguration( userManagementConfiguration );
			}

			var appProvider = provider.GetProvider( returnNullIfNotFound: true );
			if( appProvider != null )
				foreach( var i in appProvider.GetCustomConfigurations() )
					configurations.AddConfiguration( i );

			SAMLController.Configurations = configurations;
		}

		XmlElement ExternalSamlProvider.GetMetadata() {
			var spConfiguration = SAMLController.Configurations.GetConfiguration( samlConfigurationName ).LocalServiceProviderConfiguration;
			return MetadataExporter.Export(
					spConfiguration,
					SAMLController.CertificateManager.GetLocalServiceProviderSignatureCertificates( spConfiguration, null ),
					SAMLController.CertificateManager.GetLocalServiceProviderEncryptionCertificates( spConfiguration, null ),
					spConfiguration.AssertionConsumerServiceUrl,
					null,
					null )
				.ToXml();
		}

		void ExternalSamlProvider.WriteLogInResponse( HttpResponseBase response, string identityProvider, bool forceReauthentication, string returnUrl ) {
			SAMLController.ConfigurationName = samlConfigurationName;
			SAMLServiceProvider.InitiateSSO( response, returnUrl, identityProvider, new SSOOptions { ForceAuthn = forceReauthentication } );
		}

		( string identityProvider, string userName, IReadOnlyDictionary<string, string> attributes, string returnUrl ) ExternalSamlProvider.ReadAssertion(
			HttpRequest request ) {
			SAMLController.ConfigurationName = samlConfigurationName;
			SAMLServiceProvider.ReceiveSSO( request, out _, out var identityProvider, out _, out var userName, out SAMLAttribute[] attributes, out var returnUrl );
			return ( identityProvider, userName,
				       attributes.Where( i => i.Values.Any() && i.Values.First().Data != null )
					       .ToImmutableDictionary( i => i.Name, i => i.Values.First().Data.ToString() ), returnUrl );
		}
	}
}