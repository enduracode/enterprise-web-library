using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DatabaseSpecification;

namespace EnterpriseWebLibrary.DataAccess {
	internal static class DataAccessStatics {
		private const string providerName = "DataAccess";
		private static SystemProviderReference<SystemDataAccessProvider> provider;
		private static IEnumerable<DatabaseInfo> disabledAutomaticTransactionSecondaryDatabases;

		internal static void Init() {
			provider = ConfigurationStatics.GetSystemLibraryProvider<SystemDataAccessProvider>( providerName );

			disabledAutomaticTransactionSecondaryDatabases =
				provider.GetProvider( returnNullIfNotFound: true ) is AutomaticTransactionDisablingProvider automaticTransactionDisablingProvider
					? automaticTransactionDisablingProvider.GetDisabledAutomaticTransactionSecondaryDatabaseNames()
						.Select( i => ConfigurationStatics.InstallationConfiguration.GetSecondaryDatabaseInfo( i ) )
						.ToArray()
					: new DatabaseInfo[ 0 ];
		}

		internal static SystemDataAccessProvider SystemProvider => provider.GetProvider();

		internal static bool DatabaseShouldHaveAutomaticTransactions( DatabaseInfo databaseInfo ) =>
			disabledAutomaticTransactionSecondaryDatabases.All( i => i.SecondaryDatabaseName != databaseInfo.SecondaryDatabaseName );
	}
}