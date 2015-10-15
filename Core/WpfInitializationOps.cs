using System.Windows.Threading;

namespace EnterpriseWebLibrary {
	public static class WpfInitializationOps {
		private static bool exceptionHandlerEnabled;
		private static SystemInitializer appInitializer;

		/// <summary>
		/// Call this from Application_Startup. Besides this call, there should be no other code in the method.
		/// </summary>
		/// <param name="globalInitializer">The system's global initializer. Do not pass null.</param>
		/// <param name="appInitializer">The application initializer, which performs application-specific initialization and cleanup. If you have one of these you
		/// should name the class AppInitializer.</param>
		public static void InitStatics( SystemInitializer globalInitializer, SystemInitializer appInitializer = null ) {
			GlobalInitializationOps.InitStatics( globalInitializer, "Application", false );
			if( GlobalInitializationOps.SecondaryInitFailed ) {
				shutDown();
				return;
			}
			exceptionHandlerEnabled = true;

			WpfInitializationOps.appInitializer = appInitializer;
			if( appInitializer != null )
				appInitializer.InitStatics();
		}

		/// <summary>
		/// Call this from Application_Exit. Besides this call, there should be no other code in the method.
		/// </summary>
		public static void CleanUpStatics() {
			exceptionHandlerEnabled = false;
			if( !GlobalInitializationOps.SecondaryInitFailed ) {
				TelemetryStatics.ExecuteBlockWithStandardExceptionHandling(
					() => {
						if( appInitializer != null )
							appInitializer.CleanUpStatics();
					} );
			}
			GlobalInitializationOps.CleanUpStatics();
		}

		/// <summary>
		/// Call this from Application_DispatcherUnhandledException. Besides this call, there should be no other code in the method.
		/// </summary>
		public static void HandleException( DispatcherUnhandledExceptionEventArgs e ) {
			if( !exceptionHandlerEnabled )
				return;
			exceptionHandlerEnabled = false;

			e.Handled = true;

			try {
				TelemetryStatics.ReportError( e.Exception );
			}
			finally {
				shutDown();
			}
		}

		private static void shutDown() {
			System.Windows.Application.Current.Shutdown( 1 );
		}
	}
}