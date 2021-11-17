using System;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.ExternalFunctionality {
	internal static class ExternalFunctionalityStatics {
		internal const string ProviderName = "ExternalFunctionality";
		private static SystemProviderReference<SystemExternalFunctionalityProvider> provider;

		private static ExternalSamlProvider samlProvider;

		internal static void Init() {
			provider = ConfigurationStatics.GetSystemLibraryProvider<SystemExternalFunctionalityProvider>( ProviderName );

			samlProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetSamlProvider();
			samlProvider?.InitStatics( UserManagementStatics.GetCertificate, UserManagementStatics.CertificatePassword );
		}

		internal static bool SamlFunctionalityEnabled => samlProvider != null;

		internal static ExternalSamlProvider ExternalSamlProvider {
			get {
				ensureProviderExists();
				return samlProvider ?? throw new ApplicationException( "External SAML provider not available." );
			}
		}

		private static void ensureProviderExists() {
			provider.GetProvider();
		}
	}
}