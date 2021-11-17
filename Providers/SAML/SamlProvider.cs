using System;
using System.Collections.Generic;
using System.Linq;
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
		private static Func<string> certificateGetter;
		private static string certificatePassword;

		private static SystemProviderReference<AppSamlProvider> provider;
		private static Func<IReadOnlyCollection<SamlIdentityProvider>> samlIdentityProviderGetter;

		void ExternalSamlProvider.InitStatics( Func<string> certificateGetter, string certificatePassword ) {
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
				var userManagementConfiguration = new SAMLConfiguration { Name = EwlStatics.EwlInitialism.EnglishToCamel() + "UserManagement" };
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
	}
}