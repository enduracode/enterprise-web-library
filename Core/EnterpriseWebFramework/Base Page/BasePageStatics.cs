using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal static class BasePageStatics {
		private static SystemProviderReference<AppStandardPageLogicProvider> provider;

		internal static void Init( SystemProviderReference<AppStandardPageLogicProvider> provider ) {
			BasePageStatics.provider = provider;
		}

		internal static AppStandardPageLogicProvider AppProvider => provider.GetProvider();
	}
}