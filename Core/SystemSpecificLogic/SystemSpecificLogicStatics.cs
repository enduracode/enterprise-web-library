using EnterpriseWebLibrary.Configuration;
using NodaTime;

namespace EnterpriseWebLibrary.SystemSpecificLogic;

public static class SystemSpecificLogicStatics {
	private const string providersFolderAndNamespaceName = "Providers";

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

	// The duration here should probably come from the provider.
	internal static LocalDate BestEffortCutoffDate => Clock.TransactionTime.Minus( Duration.FromDays( 14 ) ).InUtc().Date;

	internal static SystemProviderReference<ProviderType> GetLibraryProvider<ProviderType>( string providerName ) where ProviderType: class =>
		new SystemProviderGetter(
			globalInitializerType.Assembly,
			$"{globalInitializerType.Namespace}.{providersFolderAndNamespaceName}",
			getProviderNotFoundErrorMessage ).GetProvider<ProviderType>( providerName );

	private static string getProviderNotFoundErrorMessage( string providerName ) =>
		"""{0} provider not found in system. To implement, create a class named {0} in Library\{1} and implement the System{0}Provider interface.""".FormatWith(
			providerName,
			providersFolderAndNamespaceName );
}