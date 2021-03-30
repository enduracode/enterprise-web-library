using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public static class EwfUiStatics {
		private static Func<AppEwfUiProvider> providerGetter;

		internal static void Init( Func<AppEwfUiProvider> providerGetter ) {
			EwfUiStatics.providerGetter = providerGetter;
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static AppEwfUiProvider AppProvider => providerGetter();
	}
}