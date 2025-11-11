using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.SystemSpecificLogic;
using EnterpriseWebLibrary.UserManagement;

namespace EnterpriseWebLibrary.ExternalFunctionality;

public static class ExternalFunctionalityStatics {
	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public const string ProviderName = "ExternalFunctionality";

	private static SystemProviderReference<SystemExternalFunctionalityProvider>? provider;

	private static ExternalMySqlProvider? mySqlProvider;
	private static ExternalOracleDatabaseProvider? oracleDatabaseProvider;
	private static ExternalOpenIdConnectProvider? openIdConnectProvider;
	private static ExternalSamlProvider? samlProvider;
	private static ExternalPdfProvider? pdfProvider;
	private static ExternalWordProvider? wordProvider;

	internal static void Init( SpecifiedValue<SystemExternalFunctionalityProvider?>? specifiedProvider ) {
		provider = SystemSpecificLogicStatics.GetLibraryProvider( ProviderName, specifiedProvider: specifiedProvider );

		mySqlProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetMySqlProvider();

		oracleDatabaseProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetOracleDatabaseProvider();

		openIdConnectProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetOpenIdConnectProvider();

		samlProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetSamlProvider();
		samlProvider?.InitStatics( UserManagementStatics.GetCertificate, UserManagementStatics.CertificatePassword );

		pdfProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetPdfProvider();
		var asposePdfLicensePath = EwlStatics.CombinePaths( ConfigurationStatics.InstallationConfiguration.AsposeLicenseFolderPath, "Aspose.Pdf.lic" );
		if( File.Exists( asposePdfLicensePath ) )
			pdfProvider?.InitStatics( asposePdfLicensePath );

		wordProvider = provider.GetProvider( returnNullIfNotFound: true )?.GetWordProvider();
		var asposeWordsLicensePath = EwlStatics.CombinePaths( ConfigurationStatics.InstallationConfiguration.AsposeLicenseFolderPath, "Aspose.Words.lic" );
		if( File.Exists( asposeWordsLicensePath ) )
			wordProvider?.InitStatics( asposeWordsLicensePath );
	}

	internal static ExternalMySqlProvider ExternalMySqlProvider {
		get {
			ensureProviderExists();
			return mySqlProvider ?? throw new Exception( "External MySQL provider not available." );
		}
	}

	internal static ExternalOracleDatabaseProvider ExternalOracleDatabaseProvider {
		get {
			ensureProviderExists();
			return oracleDatabaseProvider ?? throw new Exception( "External Oracle Database provider not available." );
		}
	}

	internal static bool OpenIdConnectFunctionalityEnabled => openIdConnectProvider != null;

	internal static ExternalOpenIdConnectProvider ExternalOpenIdConnectProvider {
		get {
			ensureProviderExists();
			return openIdConnectProvider ?? throw new Exception( "External OpenID Connect provider not available." );
		}
	}

	internal static bool SamlFunctionalityEnabled => samlProvider != null;

	internal static ExternalSamlProvider ExternalSamlProvider {
		get {
			ensureProviderExists();
			return samlProvider ?? throw new Exception( "External SAML provider not available." );
		}
	}

	internal static ExternalPdfProvider ExternalPdfProvider {
		get {
			ensureProviderExists();
			return pdfProvider ?? throw new Exception( "External PDF provider not available." );
		}
	}

	private static void ensureProviderExists() {
		provider!.GetProvider();
	}
}