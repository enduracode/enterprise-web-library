using System;

namespace RedStapler.StandardLibrary.DataAccess {
	public class DataAccessStatics {
		private const string providerName = "DataAccess";
		private static SystemDataAccessProvider provider;

		internal static void Init( Type systemLogicType ) {
			provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogicType, providerName ) as SystemDataAccessProvider;
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
	}
}