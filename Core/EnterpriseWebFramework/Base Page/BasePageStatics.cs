#nullable disable
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.WebSessionState;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal static class BasePageStatics {
	internal const string FormSelector = "form#" + PageBase.FormId;

	private static SystemProviderReference<AppStandardPageLogicProvider> provider;

	internal static void Init( SystemProviderReference<AppStandardPageLogicProvider> provider ) {
		BasePageStatics.provider = provider;
	}

	internal static AppStandardPageLogicProvider AppProvider => provider.GetProvider();

	internal static bool StatusMessagesDisplayAsNotification() =>
		PageBase.Current.StatusMessages.All( i => i.Item1 == StatusMessageType.Info ) && PageBase.Current.StatusMessages.Count() <= 3;
}