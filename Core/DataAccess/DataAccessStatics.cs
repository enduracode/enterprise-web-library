using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.SystemSpecificLogic;

namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// Development Utility and internal use only.
/// </summary>
public static class DataAccessStatics {
	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public const string ProviderName = "DataAccess";

	private static SystemProviderReference<SystemDataAccessProvider> provider = null!;
	private static IReadOnlyCollection<DatabaseInfo> disabledAutomaticTransactionSecondaryDatabases = null!;

	internal static void Init() {
		provider = SystemSpecificLogicStatics.GetLibraryProvider<SystemDataAccessProvider>( ProviderName );

		disabledAutomaticTransactionSecondaryDatabases =
			provider.GetProvider( returnNullIfNotFound: true ) is AutomaticTransactionDisablingProvider automaticTransactionDisablingProvider
				? automaticTransactionDisablingProvider.GetDisabledAutomaticTransactionSecondaryDatabaseNames()
					.Select( i => ConfigurationStatics.InstallationConfiguration.GetSecondaryDatabaseInfo( i ) )
					.Materialize()
				: Enumerable.Empty<DatabaseInfo>().Materialize();
	}

	internal static SystemDataAccessProvider SystemProvider => provider.GetProvider()!;

	internal static void InitRetrievalCaches() {
		if( ConfigurationStatics.IsClientSideApp )
			return;
		provider.GetProvider( returnNullIfNotFound: true )?.InitRetrievalCaches();
	}

	internal static bool DatabaseShouldHaveAutomaticTransactions( DatabaseInfo databaseInfo ) =>
		disabledAutomaticTransactionSecondaryDatabases.All( i => i.SecondaryDatabaseName != databaseInfo.SecondaryDatabaseName );
}