using System;
using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.DatabaseSpecification;

namespace RedStapler.StandardLibrary.DataAccess {
	public class DataAccessStatics {
		private const string providerName = "DataAccess";
		private static SystemDataAccessProvider provider;
		private static IEnumerable<DatabaseInfo> disabledAutomaticTransactionSecondaryDatabases;

		internal static void Init( Type systemLogicType ) {
			provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogicType, providerName ) as SystemDataAccessProvider;

			var automaticTransactionDisablingProvider = provider as AutomaticTransactionDisablingProvider;
			disabledAutomaticTransactionSecondaryDatabases = automaticTransactionDisablingProvider != null
				                                                 ? automaticTransactionDisablingProvider.GetDisabledAutomaticTransactionSecondaryDatabaseNames()
				                                                                                        .Select(
					                                                                                        i =>
					                                                                                        AppTools.InstallationConfiguration.GetSecondaryDatabaseInfo( i ) )
				                                                                                        .ToArray()
				                                                 : new DatabaseInfo[ 0 ];
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static SystemDataAccessProvider SystemProvider {
			get {
				if( provider == null )
					throw StandardLibraryMethods.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		internal static bool DatabaseShouldHaveAutomaticTransactions( DatabaseInfo databaseInfo ) {
			return disabledAutomaticTransactionSecondaryDatabases.All( i => i.SecondaryDatabaseName != databaseInfo.SecondaryDatabaseName );
		}
	}
}