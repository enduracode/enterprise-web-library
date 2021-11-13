using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public static class EwfUiStatics {
		private static SystemProviderReference<AppEwfUiProvider> provider;

		internal static void Init( SystemProviderReference<AppEwfUiProvider> provider ) {
			EwfUiStatics.provider = provider;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static AppEwfUiProvider AppProvider => provider.GetProvider();
	}
}