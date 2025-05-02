using EnterpriseWebLibrary.SystemSpecificLogic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

internal static class EwfUiStatics {
	private static SystemProviderReference<AppEwfUiProvider>? provider;
	internal static Func<IReadOnlyCollection<FlowComponent>>? UserInfoComponentGetter { get; private set; }

	internal static void Init( SystemProviderReference<AppEwfUiProvider> provider, Func<IReadOnlyCollection<FlowComponent>> userInfoComponentGetter ) {
		EwfUiStatics.provider = provider;
		UserInfoComponentGetter = userInfoComponentGetter;
	}

	internal static AppEwfUiProvider AppProvider => provider!.GetProvider()!;
}