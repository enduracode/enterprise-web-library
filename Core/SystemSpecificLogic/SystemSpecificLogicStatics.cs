using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.SystemSpecificLogic;

public static class SystemSpecificLogicStatics {
	/// <summary>
	/// EWL Core and Development Utility use only.
	/// </summary>
	public const string ProvidersFolderAndNamespaceName = "Providers";

	private static Type globalInitializerType { get; set; } = null!;
	internal static SystemGeneralProvider GeneralProvider { get; private set; } = null!;

	internal static void Init( Type globalInitializerType ) {
		SystemSpecificLogicStatics.globalInitializerType = globalInitializerType;
		GeneralProvider = GetLibraryProvider<SystemGeneralProvider>( "General" ).GetProvider()!;
	}

	/// <summary>
	/// Gets the display name of the system.
	/// </summary>
	public static string SystemDisplayName =>
		GeneralProvider.SystemDisplayName.Length > 0 ? GeneralProvider.SystemDisplayName : ConfigurationStatics.InstallationConfiguration.SystemName;

	internal static SystemProviderReference<ProviderType> GetLibraryProvider<ProviderType>( string providerName ) where ProviderType: class =>
		new SystemProviderGetter(
			globalInitializerType.Assembly,
			globalInitializerType.Namespace + ".Configuration." + ProvidersFolderAndNamespaceName,
			getProviderNotFoundErrorMessage ).GetProvider<ProviderType>( providerName );

	private static string getProviderNotFoundErrorMessage( string providerName ) =>
		providerName + " provider not found in system. To implement, create a class named " + providerName + @" in Library\Configuration\" +
		ProvidersFolderAndNamespaceName + " and implement the System" + providerName + "Provider interface.";
}