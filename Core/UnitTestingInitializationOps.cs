using System;

namespace EnterpriseWebLibrary {
	public static class UnitTestingInitializationOps {
		private static SystemInitializer appInitializer;

		/// <summary>
		/// Call this from the SetUp method in your NUnit initializer. Besides this call, there should be no other code in the method.
		/// </summary>
		/// <param name="globalInitializer">The system's global initializer. Do not pass null.</param>
		/// <param name="appInitializer">The application initializer, which performs unit-testing-specific initialization and cleanup. If you have one of these you
		/// should name the class AppInitializer.</param>
		public static void InitStatics( SystemInitializer globalInitializer, SystemInitializer appInitializer = null ) {
			GlobalInitializationOps.InitStatics( globalInitializer, "Tests", false, useRelativeInstallationPath: true );
			try {
				if( GlobalInitializationOps.SecondaryInitFailed ) {
					throw new ApplicationException(
						"An exception occurred during application initialization. Details should be available in an error email and the installation's error log." );
				}

				UnitTestingInitializationOps.appInitializer = appInitializer;
				if( appInitializer != null )
					appInitializer.InitStatics();
			}
			catch {
				CleanUpStatics();
				throw;
			}
		}

		/// <summary>
		/// Call this from the TearDown method in your NUnit initializer. Besides this call, there should be no other code in the method.
		/// </summary>
		public static void CleanUpStatics() {
			if( appInitializer != null )
				appInitializer.CleanUpStatics();

			GlobalInitializationOps.CleanUpStatics();
		}
	}
}