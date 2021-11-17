using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ComponentSpace.SAML2;
using ComponentSpace.SAML2.Configuration;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement.IdentityProviders;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Saml {
	public class SamlProvider: ExternalSamlProvider {
		private static string samlConfigurationName;
		private static Func<string> certificateGetter;
		private static string certificatePassword;

		private static SystemProviderReference<AppSamlProvider> provider;
		private static Func<IReadOnlyCollection<SamlIdentityProvider>> samlIdentityProviderGetter;

		void ExternalSamlProvider.InitStatics( Func<string> certificateGetter, string certificatePassword ) {
			samlConfigurationName = EwlStatics.EwlInitialism.EnglishToCamel() + "UserManagement";
			SamlProvider.certificateGetter = certificateGetter;
			SamlProvider.certificatePassword = certificatePassword;
		}

		void ExternalSamlProvider.InitAppStatics(
			SystemProviderGetter providerGetter, Func<IReadOnlyCollection<SamlIdentityProvider>> samlIdentityProviderGetter ) {
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
						Name = SamlMetadata.GetInfo().GetUrl(),
						AssertionConsumerServiceUrl = SamlLogIn.GetInfo().GetUrl(),
						LocalCertificates = new List<CertificateConfiguration>
							{
								new CertificateConfiguration { String = certificateGetter(), Password = certificatePassword }
							},
						ResolveToHttps = false
					};
				foreach( var identityProvider in samlIdentityProviders ) {
				}
				configurations.AddConfiguration( userManagementConfiguration );
			}

			var appProvider = provider.GetProvider( returnNullIfNotFound: true );
			if( appProvider != null )
				foreach( var i in appProvider.GetCustomConfigurations() )
					configurations.AddConfiguration( i );

			SAMLController.Configurations = configurations;
		}

		XmlElement ExternalSamlProvider.GetMetadataElement() {
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
	}
}