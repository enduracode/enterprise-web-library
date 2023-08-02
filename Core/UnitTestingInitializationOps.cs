using NUnit.Framework;

namespace EnterpriseWebLibrary;

public static class UnitTestingInitializationOps {
	private static SystemInitializer? appInitializer;

	/// <summary>
	/// Call this from the OneTimeSetUp method in your NUnit initializer. Besides this call, there should be no other code in the method.
	/// </summary>
	/// <param name="globalInitializer">The system's global initializer. Do not pass null.</param>
	/// <param name="appInitializer">The application initializer, which performs unit-testing-specific initialization and cleanup. If you have one of these you
	/// should name the class AppInitializer.</param>
	public static void InitStatics( SystemInitializer globalInitializer, SystemInitializer? appInitializer = null ) {
		GlobalInitializationOps.InitStatics( globalInitializer, "Tests", false, assemblyFolderPath: TestContext.CurrentContext.TestDirectory );
		try {
			if( GlobalInitializationOps.SecondaryInitFailed )
				throw new ApplicationException(
					"An exception occurred during application initialization. Details should be available in an error email and the installation's error log." );

			UnitTestingInitializationOps.appInitializer = appInitializer;
			appInitializer?.InitStatics();
		}
		catch {
			CleanUpStatics();
			throw;
		}
	}

	/// <summary>
	/// Call this from the OneTimeTearDown method in your NUnit initializer. Besides this call, there should be no other code in the method.
	/// </summary>
	public static void CleanUpStatics() {
		appInitializer?.CleanUpStatics();

		GlobalInitializationOps.CleanUpStatics();
	}
}