using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.BlobStorage;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.UserManagement;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace EnterpriseWebLibrary;

public static class GlobalInitializationOps {
	internal static readonly Duration MachineStartupDelay = Duration.FromMinutes( 5 );

	private static bool initialized;
	private static SystemInitializer? globalInitializer;
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
	/// <param name="isClientSideApp"></param>
	/// <param name="assemblyFolderPath">Pass a nonempty string to override the assembly folder path, which is used to locate the installation folder. Use with
	/// caution.</param>
	/// <param name="telemetryAppErrorContextWriter"></param>
	/// <param name="mainDataAccessStateGetter">A method that returns the current main data-access state whenever it is requested, including during this
	/// InitStatics call. If you pass null, the data-access subsystem will not be available in the application.</param>
	/// <param name="useLongDatabaseTimeouts">Pass true if the application is a background process that can tolerate slow database access.</param>
	/// <param name="currentDatabaseConnectionManagerGetter">A method that returns the current automatic database-connection manager.</param>
	/// <param name="currentUserGetter">A method that returns the current authenticated user. If you pass null, the authenticated user will not be available in
	/// the application.</param>
	public static void InitStatics(
		SystemInitializer globalInitializer, string appName, bool isClientSideApp, string assemblyFolderPath = "",
		Action<TextWriter>? telemetryAppErrorContextWriter = null, Func<DataAccessState>? mainDataAccessStateGetter = null, bool useLongDatabaseTimeouts = false,
		Func<AutomaticDatabaseConnectionManager>? currentDatabaseConnectionManagerGetter = null, Func<SystemUser?>? currentUserGetter = null ) {
		var initializationLog = "Starting init";
		try {
			if( initialized )
				throw new ApplicationException( "This class can only be initialized once." );

			if( globalInitializer == null )
				throw new ApplicationException( "The system must have a global initializer." );

			// Initialize these before the exception handling block below because it's reasonable for the exception handling to depend on them.
			ConfigurationStatics.Init( assemblyFolderPath, globalInitializer.GetType(), appName, isClientSideApp, ref initializationLog );
			EmailStatics.Init();
			TelemetryStatics.Init( telemetryAppErrorContextWriter );

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
			CultureInfo.DefaultThreadCurrentCulture = Cultures.EnglishUnitedStates;

			JsonConvert.DefaultSettings = () => new JsonSerializerSettings().ConfigureForNodaTime( DateTimeZoneProviders.Tzdb );

			var asposePdfLicensePath = EwlStatics.CombinePaths( ConfigurationStatics.InstallationConfiguration.AsposeLicenseFolderPath, "Aspose.Pdf.lic" );
			if( File.Exists( asposePdfLicensePath ) )
				new Aspose.Pdf.License().SetLicense( asposePdfLicensePath );
			var asposeWordsLicensePath = EwlStatics.CombinePaths( ConfigurationStatics.InstallationConfiguration.AsposeLicenseFolderPath, "Aspose.Words.lic" );
			if( File.Exists( asposeWordsLicensePath ) )
				new Aspose.Words.License().SetLicense( asposeWordsLicensePath );

			AppMemoryCache.Init();
			EncryptionOps.Init();

			ExternalFunctionalityStatics.Init();

			// data access
			MySqlInfo.Init( () => ExternalFunctionalityStatics.ExternalMySqlProvider );
			OracleInfo.Init( () => ExternalFunctionalityStatics.ExternalOracleDatabaseProvider );
			DataAccessStatics.Init();
			DataAccessState.Init( mainDataAccessStateGetter, useLongDatabaseTimeouts );
			AutomaticDatabaseConnectionManager.Init( currentDatabaseConnectionManagerGetter );
			DataAccessStatics.InitRetrievalCaches();

			BlobStorageStatics.Init();
			HtmlBlockStatics.Init();

			UserManagementStatics.Init(
				() => {
					if( ExternalFunctionalityStatics.SamlFunctionalityEnabled )
						ExternalFunctionalityStatics.ExternalSamlProvider.RefreshConfiguration();
				},
				currentUserGetter ?? ( () => null ) );

			GlobalInitializationOps.globalInitializer = globalInitializer;
			globalInitializer.InitStatics();

			ExecuteWithAutomaticDatabaseConnections( UserManagementStatics.InitSystemSpecificLogicDependencies );
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

	internal static bool SecondaryInitFailed => secondaryInitFailed;

	/// <summary>
	/// Performs cleanup activities so the application can be shut down.
	/// </summary>
	public static void CleanUpStatics() {
		try {
			globalInitializer?.CleanUpStatics();

			AppMemoryCache.CleanUp();
		}
		catch( Exception e ) {
			TelemetryStatics.ReportError( "An exception occurred during application cleanup:", e );
		}
	}

	internal static void ExecuteWithAutomaticDatabaseConnections( Action method ) {
		var connectionManager = new AutomaticDatabaseConnectionManager();
		try {
			connectionManager.DataAccessState.ExecuteWithThis( method );
			connectionManager.CommitTransactionsAndExecuteNonTransactionalModificationMethods( false );
		}
		catch {
			connectionManager.RollbackTransactions( false );
			throw;
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

	/// <summary>
	/// Installation Support Utility and internal use only.
	/// </summary>
	public static void DelayIfMachineWasRecentlyStarted() {
		var timeSinceStartup = Duration.FromMilliseconds( GetTickCount64() );
		if( timeSinceStartup < MachineStartupDelay )
			Thread.Sleep( ( MachineStartupDelay - timeSinceStartup ).ToTimeSpan() );
	}

	[ DllImport( "kernel32" ) ]
	private static extern ulong GetTickCount64();
}