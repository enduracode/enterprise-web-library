using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal static class BasePageStatics {
		private static Func<AppStandardPageLogicProvider> providerGetter;

		internal static void Init( Func<AppStandardPageLogicProvider> providerGetter ) {
			BasePageStatics.providerGetter = providerGetter;
		}

		internal static AppStandardPageLogicProvider AppProvider => providerGetter();
	}
}