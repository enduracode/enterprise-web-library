using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.ExternalFunctionality;

internal static class ExternalFunctionalityStatics {
	internal const string ProviderName = "ExternalFunctionality";
	private static SystemProviderReference<SystemExternalFunctionalityProvider>? provider;

	private static ExternalMySqlProvider? mySqlProvider;
	private static ExternalOpenIdConnectProvider? openIdConnectProvider;
	private static ExternalSamlProvider? samlProvider;

	internal static void Init() {
		provider = ConfigurationStatics.GetSystemLibraryProvider<SystemExternalFunctionalityProvider>( ProviderName );

		mySqlProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetMySqlProvider();

		openIdConnectProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetOpenIdConnectProvider();

		samlProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetSamlProvider();
		samlProvider?.InitStatics( UserManagementStatics.GetCertificate, UserManagementStatics.CertificatePassword );
	}

	internal static ExternalMySqlProvider ExternalMySqlProvider {
		get {
			ensureProviderExists();
			return mySqlProvider ?? throw new ApplicationException( "External MySQL provider not available." );
		}
	}

	internal static bool OpenIdConnectFunctionalityEnabled => openIdConnectProvider != null;

	internal static ExternalOpenIdConnectProvider ExternalOpenIdConnectProvider {
		get {
			ensureProviderExists();
			return openIdConnectProvider ?? throw new ApplicationException( "External OpenID Connect provider not available." );
		}
	}

	internal static bool SamlFunctionalityEnabled => samlProvider != null;

	internal static ExternalSamlProvider ExternalSamlProvider {
		get {
			ensureProviderExists();
			return samlProvider ?? throw new ApplicationException( "External SAML provider not available." );
		}
	}

	private static void ensureProviderExists() {
		provider!.GetProvider();
	}
}