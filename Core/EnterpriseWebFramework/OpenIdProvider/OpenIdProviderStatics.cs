#nullable disable
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.WellKnownUrlHandling;
using EnterpriseWebLibrary.ExternalFunctionality;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.OpenIdProvider;

internal static class OpenIdProviderStatics {
	internal const string CertificatePassword = "password";

	private static SystemProviderReference<AppOpenIdProviderProvider> provider;

	private static ( Func<string> getter, Action<string> updater )? certificateMethods;

	internal static void Init( SystemProviderReference<AppOpenIdProviderProvider> provider ) {
		OpenIdProviderStatics.provider = provider;
	}

	internal static bool OpenIdProviderEnabled => provider.GetProvider( returnNullIfNotFound: true ) is not null;

	internal static AppOpenIdProviderProvider AppProvider => provider.GetProvider();

	internal static void InitAppSpecificLogicDependencies() {
		if( !OpenIdProviderEnabled )
			return;
		certificateMethods = AppProvider.GetCertificateMethods();
	}

	internal static string GetCertificate() =>
		certificateMethods.HasValue ? certificateMethods.Value.getter() : throw new ApplicationException( "Self-signed certificate methods not available." );

	internal static void UpdateCertificate( string certificate ) {
		if( !certificateMethods.HasValue )
			throw new ApplicationException( "Self-signed certificate methods not available." );
		certificateMethods.Value.updater( certificate );
		if( ExternalFunctionalityStatics.OpenIdConnectFunctionalityEnabled )
			ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.RefreshConfiguration();
	}

	internal static IEnumerable<WellKnownUrl> GetWellKnownUrls() =>
		OpenIdProviderEnabled
			? new WellKnownUrl(
					"openid-configuration",
					() => new EwfSafeResponseWriter( EwfResponse.CreateFromAspNetMvcAction( ExternalFunctionalityStatics.ExternalOpenIdConnectProvider.WriteMetadata ) ) )
				.ToCollection()
			: Enumerable.Empty<WellKnownUrl>();
}