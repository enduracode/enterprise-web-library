using System;
using System.Linq;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

namespace EnterpriseWebLibrary {
	public static class GlobalInitializationOps {
		private static bool initialized;
		private static SystemInitializer globalInitializer;
		private static bool secondaryInitFailed;

		/// <summary>
		/// Initializes the system. This includes loading application settings from the configuration file. The application name should be scoped within the system.
		/// For non web applications, this method must be called directly from the main executable assembly and not from a supporting library.
		/// 
		/// To debug this method, create a folder called C:\AnyoneFullControl and give Everyone full control. A file will appear in that folder explaining how far
		/// it got in init.
		/// </summary>
		/// <param name="globalInitializer">The system's global initializer. Do not pass null.</param>
		/// <param name="appName"></param>
		/// <param name="isClientSideProgram"></param>
		/// <param name="useRelativeInstallationPath">Pass true to use a relative path for the installation folder. This means that the folder will be located using
		/// the working directory rather than the assembly path. Use with caution.</param>
		/// <param name="mainDataAccessStateGetter">A method that returns the current main data-access state whenever it is requested, including during this
		/// InitStatics call. Do not allow multiple threads to use the same state at the same time. If you pass null, the data-access subsystem will not be
		/// available in the application.</param>
		public static void InitStatics(
			SystemInitializer globalInitializer, string appName, bool isClientSideProgram, bool useRelativeInstallationPath = false,
			Func<DataAccessState> mainDataAccessStateGetter = null ) {
			var initializationLog = "Starting init";
			try {
				if( initialized )
					throw new ApplicationException( "This class can only be initialized once." );

				if( globalInitializer == null )
					throw new ApplicationException( "The system must have a global initializer." );

				// Initialize ConfigurationStatics, including the general provider, before the exception handling block below because it's reasonable for the exception
				// handling to depend on this.
				ConfigurationStatics.Init( useRelativeInstallationPath, globalInitializer.GetType(), appName, isClientSideProgram, ref initializationLog );

				// Setting the initialized flag to true must be done before executing the secondary init block below so that exception handling works.
				initialized = true;
				initializationLog += Environment.NewLine + "Succeeded in primary init.";
			}
			catch( Exception e ) {
				initializationLog += Environment.NewLine + e;
				EwlStatics.EmergencyLog( "Initialization log", initializationLog );
				throw;
			}

			try {
				var asposeLicense = ConfigurationStatics.SystemGeneralProvider.AsposeLicenseName;
				if( asposeLicense.Any() ) {
					new Aspose.Pdf.License().SetLicense( asposeLicense );
					new Aspose.Words.License().SetLicense( asposeLicense );
				}

				AppMemoryCache.Init();
				BlobFileOps.Init();
				DataAccessStatics.Init();
				DataAccessState.Init( mainDataAccessStateGetter );
				EmailStatics.Init();
				EncryptionOps.Init();
				HtmlBlockStatics.Init();
				InstallationSupportUtility.ConfigurationLogic.Init1();
				UserManagementStatics.Init();

				GlobalInitializationOps.globalInitializer = globalInitializer;
				globalInitializer.InitStatics();
			}
			catch( Exception e ) {
				secondaryInitFailed = true;

				// Suppress all exceptions since they would prevent apps from knowing that primary initialization succeeded. EWF apps need to know this in order to
				// automatically restart themselves. Other apps could find this knowledge useful as well.
				try {
					TelemetryStatics.ReportError( "An exception occurred during application initialization:", e );
				}
				catch {}
			}
		}

		internal static bool SecondaryInitFailed { get { return secondaryInitFailed; } }

		/// <summary>
		/// Performs cleanup activities so the application can be shut down.
		/// </summary>
		public static void CleanUpStatics() {
			try {
				if( globalInitializer != null )
					globalInitializer.CleanUpStatics();

				AppMemoryCache.CleanUp();
			}
			catch( Exception e ) {
				TelemetryStatics.ReportError( "An exception occurred during application cleanup:", e );
			}
		}

		/// <summary>
		/// Executes the specified method. Returns 0 if it is successful. If an exception occurs, this method returns 1 and details about the exception are emailed
		/// to the developers and logged. This should only be used at the root level of a console application because it checks to ensure the system logic has
		/// initialized properly and its return code is designed to be useful from the command line of such an application. Throw a DoNotEmailOrLogException to
		/// cause this method to return 1 without emailing or logging the exception.
		/// </summary>
		public static int ExecuteAppWithStandardExceptionHandling( Action method ) {
			if( secondaryInitFailed )
				return 1;
			try {
				method();
			}
			catch( Exception e ) {
				if( !( e is DoNotEmailOrLogException ) )
					TelemetryStatics.ReportError( e );
				return 1;
			}
			return 0;
		}
	}
}