using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Web;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.Configuration.Machine;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Email;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Provides a suite of static methods to make database connection operations easy.
	/// </summary>
	public static partial class AppTools {
		private static bool initialized;
		internal static string AppName { get; private set; }
		private static bool isClientSideProgram;
		private static MachineConfiguration machineConfiguration;
		private static Assembly appAssembly;
		internal static InstallationConfiguration InstallationConfiguration { get; private set; }
		private static SystemGeneralProvider provider;
		private static bool secondaryInitFailed;

		/// <summary>
		/// RSIS use only. The path to the machine configuration XML file.
		/// </summary>
		public static readonly string MachineConfigXmlFilePath = StandardLibraryMethods.CombinePaths( RedStaplerFolderPath, "Machine Configuration.xml" );

		/// <summary>
		/// Gets the path of the Red Stapler folder on the machine.
		/// </summary>
		public static string RedStaplerFolderPath { get { return StandardLibraryMethods.CombinePaths( "C:", "Red Stapler" ); } }

		/// <summary>
		/// Initializes the class. This includes loading application settings from the configuration file. The application name should be scoped within the system.
		/// For non web applications, this method must be called directly from the main executable assembly and not from a supporting library.
		/// 
		/// To debug this method, create a folder called C:\AnyoneFullControl and give Everyone full control. A file will appear in that folder explaining how far
		/// it got in init.
		/// </summary>
		/// <param name="appName"></param>
		/// <param name="isClientSideProgram"></param>
		/// <param name="systemLogic"></param>
		/// <param name="mainDataAccessStateGetter">A method that returns the current main data-access state whenever it is requested, including during this
		/// AppTools.Init call. Do not allow multiple threads to use the same state at the same time. If you pass null, the data-access subsystem will not be
		/// available in the application.</param>
		public static void Init( string appName, bool isClientSideProgram, SystemLogic systemLogic, Func<DataAccessState> mainDataAccessStateGetter = null ) {
			var initializationLog = "Starting init";
			try {
				if( initialized )
					throw new ApplicationException( "This class can only be initialized once." );

				AppName = appName;
				AppTools.isClientSideProgram = isClientSideProgram;

				initializationLog += Environment.NewLine + "About to load machine config";

				// Load machine configuration.
				if( File.Exists( MachineConfigXmlFilePath ) ) {
					// Do not perform schema validation since the schema file won't be available on non-development machines.
					machineConfiguration = XmlOps.DeserializeFromFile<MachineConfiguration>( MachineConfigXmlFilePath, false );
				}

				initializationLog += Environment.NewLine + "About to determine installation path";

				// Determine the installation path and load configuration information.
				string installationPath;
				bool isDevelopmentInstallation;
				if( isWebApp() ) {
					initializationLog += Environment.NewLine + "Is a web app";

					// Assume the first assembly up the call stack that is not this assembly is the web application assembly.
					var stackFrames = new StackTrace().GetFrames();
					if( stackFrames == null )
						throw new ApplicationException( "No stack trace available." );
					appAssembly = stackFrames.Select( frame => frame.GetMethod().DeclaringType.Assembly ).First( assembly => assembly != Assembly.GetExecutingAssembly() );

					initializationLog += Environment.NewLine + "Stack trace loaded, about to create installation path";

					installationPath = StandardLibraryMethods.CombinePaths( HttpRuntime.AppDomainAppPath, ".." );
					isDevelopmentInstallation = !InstallationConfiguration.InstalledInstallationExists( installationPath );
				}
				else {
					initializationLog += Environment.NewLine + "Is not a web app";

					// Assume this is an installed installation. If this assumption turns out to be wrong, consider it a development installation.
					// We use the assembly folder path here so we're not relying on the working directory of the application.
					// Installed executables are one level below the installation folder.
					appAssembly = Assembly.GetCallingAssembly();
					var assemblyFolderPath = Path.GetDirectoryName( appAssembly.Location );
					installationPath = StandardLibraryMethods.CombinePaths( assemblyFolderPath, ".." );
					isDevelopmentInstallation = !InstallationConfiguration.InstalledInstallationExists( installationPath );
					if( isDevelopmentInstallation )
						installationPath = StandardLibraryMethods.CombinePaths( assemblyFolderPath, "..", "..", ".." ); // Visual Studio puts executables inside bin\Debug.
				}
				initializationLog += Environment.NewLine + "Successfully determined installation path";
				InstallationConfiguration = new InstallationConfiguration( installationPath, isDevelopmentInstallation );
				initializationLog += Environment.NewLine + "Successfully loaded installation configuration";

				if( systemLogic == null )
					throw new ApplicationException( "The system must have a global logic class and you must pass an instance of it to AppTools.Init." );

				// Initialize the provider before the exception handling block below because it's reasonable for the exception handling to depend on this provider.
				provider = StandardLibraryMethods.GetSystemLibraryProvider( systemLogic.GetType(), "General" ) as SystemGeneralProvider;
				if( provider == null )
					throw new ApplicationException( "General provider not found in system" );

				initializationLog += Environment.NewLine + "Succeeded in primary init.";

				try {
					// Setting the initialized flag to true must be done first so the exception handling works.
					initialized = true;

					var asposeLicense = provider.AsposeLicenseName;
					if( asposeLicense.Any() ) {
						new Aspose.Pdf.License().SetLicense( asposeLicense );
						new Aspose.Words.License().SetLicense( asposeLicense );
						new Aspose.Cells.License().SetLicense( asposeLicense );
					}

					// This initialization could be performed using reflection. There is no need for AppTools to have a dependency on these classes.
					BlobFileOps.Init( systemLogic.GetType() );
					DataAccessStatics.Init( systemLogic.GetType() );
					DataAccessState.Init( mainDataAccessStateGetter );
					EncryptionOps.Init( systemLogic.GetType() );
					HtmlBlockStatics.Init( systemLogic.GetType() );
					InstallationSupportUtility.ConfigurationLogic.Init( systemLogic.GetType() );
					UserManagementStatics.Init( systemLogic.GetType() );

					systemLogic.InitSystem();

					initializationLog += Environment.NewLine + "Succeeded in secondary init.";
				}
				catch( Exception e ) {
					// NOTE: For web apps, non web apps that are being run non-interactively, and maybe all other apps too, we should suppress all exceptions from here
					// since they will only result in events being logged or error dialogs appearing, and neither of these is really helpful to us. We may also want to
					// suppress exceptions from the catch block in ExecuteAppWithStandardExceptionHandling.
					secondaryInitFailed = true;
					EmailAndLogError( "An exception occurred during application initialization:", e );
				}
			}
			catch( Exception e ) {
				initializationLog += Environment.NewLine + e;
				StandardLibraryMethods.EmergencyLog( "Initialization log", initializationLog );
				throw;
			}
		}

		/// <summary>
		/// Loads installation-specific custom configuration information.
		/// </summary>
		public static T LoadInstallationCustomConfiguration<T>() {
			assertClassInitialized();

			// Do not perform schema validation for non-development installations because the schema file won't be available on non-development machines. Do not
			// perform schema validation for development installations because we may create sample solutions and send them to tech support people for
			// troubleshooting, and these people may not put the solution in the proper location on disk. In this case we would not have access to the schema since
			// we use absolute paths in the XML files to refer to the schema files.
			return XmlOps.DeserializeFromFile<T>( InstallationConfiguration.InstallationCustomConfigurationFilePath, false );
		}

		internal static bool SecondaryInitFailed {
			get {
				assertClassInitialized();
				return secondaryInitFailed;
			}
		}

		/// <summary>
		/// Standard Library and ISU use only.
		/// </summary>
		public static MachineConfiguration MachineConfiguration {
			get {
				assertClassInitialized();
				return machineConfiguration;
			}
		}

		/// <summary>
		/// Gets the name of the system.
		/// </summary>
		public static string SystemName {
			get {
				assertClassInitialized();
				return InstallationConfiguration.SystemName;
			}
		}

		internal static bool IsDevelopmentInstallation {
			get {
				assertClassInitialized();
				return InstallationConfiguration.InstallationType == InstallationType.Development;
			}
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public static bool IsIntermediateInstallation {
			get {
				assertClassInitialized();
				return InstallationConfiguration.InstallationType == InstallationType.Intermediate;
			}
		}

		/// <summary>
		/// Gets whether this is a live installation. Use with caution. If you do not deliberately test code that only runs in live installations, you may not
		/// discover problems with it until it is live.
		/// </summary>
		public static bool IsLiveInstallation {
			get {
				assertClassInitialized();
				return InstallationConfiguration.InstallationType == InstallationType.Live;
			}
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public static SystemGeneralProvider SystemProvider {
			get {
				assertClassInitialized();
				return provider;
			}
		}

		internal static bool DatabaseExists {
			get {
				assertClassInitialized();
				return InstallationConfiguration.PrimaryDatabaseInfo != null;
			}
		}

		/// <summary>
		/// Gets the user object for the authenticated user. Returns null if the user has not been authenticated. In a web application, do not use from the
		/// initDefaultOptionalParameterPackage or init methods of Info classes because the page has not yet been able to correct the connection security of the
		/// request, if necessary, and because parent authorization logic has not yet executed. To use from initUserDefaultOptionalParameterPackage you must
		/// explicitly specify the connection security as SecureIfPossible in all pages and entity setups that use this item as a parent. To use from
		/// createParentPageInfo--which you should only do if, for a given set of parameters, there is no single parent that all users can access--you must
		/// explicitly specify the connection security as SecureIfPossible in the current item. With both of these uses, keep in mind that no parent authorization
		/// logic has executed and therefore you cannot assume anything about the user. Does not currently work outside of web applications.
		/// </summary>
		public static User User {
			get {
				assertClassInitialized();
				return EwfApp.Instance != null && EwfApp.Instance.RequestState != null ? EwfApp.Instance.RequestState.User : null;
			}
		}

		/// <summary>
		/// Sends an email from the default address to the developers with the given exception information and additional information about
		/// the running program.
		/// </summary>
		public static void EmailAndLogError( Exception e ) {
			EmailAndLogError( "", e );
		}

		/// <summary>
		/// Sends an email from the default address to the developers with the given exception information and additional information about
		/// the running program.  Prefix provides additional information before the standard exception and page information.
		/// The exception may be null.
		/// </summary>
		public static void EmailAndLogError( string prefix, Exception exception ) {
			using( var sw = new StringWriter() ) {
				if( prefix.Length > 0 ) {
					sw.WriteLine( prefix );
					sw.WriteLine();
				}
				if( exception != null ) {
					sw.WriteLine( exception.ToString() );
					sw.WriteLine();
				}

				if( isWebApp() ) {
					// This check ensures that there is an actual request, which is not the case during application initialization.
					if( EwfApp.Instance != null && EwfApp.Instance.RequestState != null ) {
						sw.WriteLine( "URL: " + EwfApp.Instance.RequestState.Url );

						sw.WriteLine();
						foreach( string fieldName in HttpContext.Current.Request.Form )
							sw.WriteLine( "Form field " + fieldName + ": " + HttpContext.Current.Request.Form[ fieldName ] );

						sw.WriteLine();
						foreach( string cookieName in HttpContext.Current.Request.Cookies )
							sw.WriteLine( "Cookie " + cookieName + ": " + HttpContext.Current.Request.Cookies[ cookieName ].Value );

						sw.WriteLine();
						sw.WriteLine( "User agent: " + HttpContext.Current.Request.GetUserAgent() );
						sw.WriteLine( "Referrer: " + NetTools.ReferringUrl );
						User user = null;

						// exception-prone code
						try {
							user = User;
						}
						catch {}

						if( user != null )
							sw.WriteLine( "User: " + user.Email );
					}
				}
				else {
					sw.WriteLine( "Program: " + AppName );
					sw.WriteLine( "Version: " + appAssembly.GetName().Version );
					sw.WriteLine( "Machine: " + StandardLibraryMethods.GetLocalHostName() );
				}

				StandardLibraryMethods.CallEveryMethod( delegate { sendErrorEmail( sw.ToString() ); }, delegate { logError( sw.ToString() ); } );
			}
		}

		/// <summary>
		/// Sends an error email message to the developer addresses specified in the config file using the SMTP server specified in the config file.
		/// </summary>
		private static void sendErrorEmail( string body ) {
			assertClassInitialized();

			var m = new EmailMessage();
			foreach( var developer in InstallationConfiguration.Developers )
				m.ToAddresses.Add( new EmailAddress( developer.EmailAddress, developer.Name ) );
			m.Subject = "Error in " + InstallationConfiguration.FullName;
			if( isClientSideProgram )
				m.Subject += " on " + StandardLibraryMethods.GetLocalHostName();
			m.BodyHtml = body.GetTextAsEncodedHtml();
			SendEmailWithDefaultFromAddress( m );
		}

		/// <summary>
		/// Sends a warning email message to the developer addresses specified in the config file using the SMTP server specified in the config file.
		/// </summary>
		internal static void SendWarningEmail( string subject, string body ) {
			assertClassInitialized();

			var m = new EmailMessage();
			foreach( var developer in InstallationConfiguration.Developers )
				m.ToAddresses.Add( new EmailAddress( developer.EmailAddress, developer.Name ) );
			m.Subject = "Warning: {0} - {1}".FormatWith( subject, InstallationConfiguration.FullName );
			m.BodyHtml = body.GetTextAsEncodedHtml();
			SendEmailWithDefaultFromAddress( m );
		}

		/// <summary>
		/// After setting the From property to the from address specified in the config file, sends the specified mail message using the SMTP server specified in
		/// the config file.
		/// </summary>
		public static void SendEmailWithDefaultFromAddress( EmailMessage m ) {
			assertClassInitialized();

			m.From = new EmailAddress( InstallationConfiguration.EmailDefaultFromAddress, InstallationConfiguration.EmailDefaultFromName );
			SendEmail( m );
		}

		/// <summary>
		/// Sends the specified mail message using the SMTP server specified in the config file.
		/// </summary>
		public static void SendEmail( EmailMessage message ) {
			assertClassInitialized();

			alterMessageForTestingIfNecessary( message );

			// We used to cache the SmtpClient object. It turned out not to be thread safe, so now we create a new one for every email.
			System.Net.Mail.SmtpClient smtpClient = null;
			try {
				if( InstallationConfiguration.SmtpServer.Length > 0 )
					smtpClient = new System.Net.Mail.SmtpClient { Host = InstallationConfiguration.SmtpServer };
				else if( InstallationConfiguration.InstallationType == InstallationType.Development ) {
					var pickupFolderPath = StandardLibraryMethods.CombinePaths( RedStaplerFolderPath, "Outgoing Dev Mail" );
					Directory.CreateDirectory( pickupFolderPath );
					smtpClient = new System.Net.Mail.SmtpClient
						{
							DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory,
							PickupDirectoryLocation = pickupFolderPath
						};
				}
				else if( isClientSideProgram )
					smtpClient = provider.CreateClientSideAppSmtpClient();
				else
					smtpClient = new System.Net.Mail.SmtpClient { DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.PickupDirectoryFromIis };

				using( var m = new System.Net.Mail.MailMessage() ) {
					message.ConfigureMailMessage( m );
					try {
						smtpClient.Send( m );
					}
					catch( System.Net.Mail.SmtpException e ) {
						throw new EmailSendingException( "Failed to send an email message.", e );
					}
				}
			}
			finally {
				// Microsoft's own dispose method fails to work if Host is not specified, even though Host doesn't need to be specified for operation.
				if( smtpClient != null && !string.IsNullOrEmpty( smtpClient.Host ) )
					smtpClient.Dispose();
			}
		}

		private static void alterMessageForTestingIfNecessary( EmailMessage m ) {
			// For testing installations, don't actually send email to recipients (they may be real people). Instead, send to the developers.
			if( IsIntermediateInstallation ) {
				var wouldHaveBeenEmailedTo = m.ToAddresses.Select( eml => eml.Address ).GetCommaDelimitedStringFromCollection();
				m.Subject = "Testing installation: " + m.Subject;
				m.BodyHtml =
					( "Because this is a testing installation, this message was not actually sent.  Had this been a live installation, the message would have been sent to the following recipients: " +
					  wouldHaveBeenEmailedTo + Environment.NewLine + Environment.NewLine ).GetTextAsEncodedHtml() + m.BodyHtml;

				m.ToAddresses.Clear();
				m.CcAddresses.Clear();
				m.BccAddresses.Clear();
				m.ToAddresses.AddRange( DeveloperEmailAddresses );
			}
		}

		private static bool isWebApp() {
			return HttpContext.Current != null;
		}

		/// <summary>
		/// Executes the specified method. Returns 0 if it is successful. If an exception occurs, this method returns 1 and details about the exception are emailed
		/// to the developers and logged. This should only be used at the root level of a console application because it checks to ensure the system logic has
		/// initialized properly and its return code is designed to be useful from the command line of such an application. Throw a DoNotEmailOrLogException to
		/// cause this method to return 1 without emailing or logging the exception.
		/// </summary>
		public static int ExecuteAppWithStandardExceptionHandling( Action method ) {
			assertClassInitialized();

			if( secondaryInitFailed )
				return 1;
			try {
				method();
			}
			catch( Exception e ) {
				if( !( e is DoNotEmailOrLogException ) )
					EmailAndLogError( e );
				return 1;
			}
			return 0;
		}

		/// <summary>
		/// Executes the specified method. Returns true if it is successful. If an exception occurs, this method returns false and details about the exception are emailed
		/// to the developers and logged. This should not be necessary in web applications, as they have their own error handling.
		/// Prior to calling this, you should be sure that system logic initialization has succeeded.  This almost always means that ExecuteAppWithStandardExceptionHandling
		/// has been called somewhere up the stack.
		/// This method has no side effects other than those of the given method and the email/logging that occurs in the event of an error.  This can be used
		/// repeatedly inside any application as an alternative to a try catch block.
		/// </summary>
		public static bool ExecuteBlockWithStandardExceptionHandling( Action method ) {
			assertClassInitialized();

			try {
				method();
				return true;
			}
			catch( Exception e ) {
				EmailAndLogError( e );
				return false;
			}
		}

		/// <summary>
		/// Use this to email errors from web service methods and turn normal exceptions into FaultExceptions.
		/// </summary>
		public static void ExecuteWebServiceWithStandardExceptionHandling( Action method ) {
			assertClassInitialized();

			// NOTE: Do we need to check whether the system logic was initialized properly or will EwfApp_BeginRequest take care of it?
			try {
				method();
			}
			catch( Exception e ) {
				throw createWebServiceException( e );
			}
		}

		/// <summary>
		/// Use this to email errors from web service methods and turn normal exceptions into FaultExceptions.
		/// </summary>
		public static T ExecuteWebServiceWithStandardExceptionHandling<T>( Func<T> method ) {
			assertClassInitialized();

			// NOTE: Do we need to check whether the system logic was initialized properly or will EwfApp_BeginRequest take care of it?
			try {
				return method();
			}
			catch( Exception e ) {
				throw createWebServiceException( e );
			}
		}

		private static Exception createWebServiceException( Exception e ) {
			if( !( e is Wcf.AccessDeniedException ) )
				EmailAndLogError( e );
			return new FaultException( e.ToString() );
		}

		/// <summary>
		/// Executes the given block of code as a critical region synchronized on the given GUID. The GUID should be passed with surrounding {}.
		/// The GUID is automatically prefixed with Global\ so that the mutex has machine scope. The GUID will usually be one to one with a program.
		/// Pass true for SkipExecutionIfMutexAlreadyOwned to return if something already has the mutex.  This is useful for killing a program when
		/// you only want one instance to run at a time. Pass false if you want to wait until the mutex is released to run your code.
		/// Returns false if execution was skipped.  Otherwise, returns true.
		/// If using this along with a WithStandardExceptionHandling method, this should go inside.
		/// </summary>
		public static bool ExecuteAsCriticalRegion( string guid, bool skipExecutionIfMutexAlreadyOwned, Action method ) {
			// The Global\ prefix makes the mutex visible across terminal services sessions. The double backslash is convention.
			// NOTE: What double backslash? Isn't it a single backslash as the comment states?
			guid = "Global\\" + guid;

			using( var mutex = new Mutex( false /*Do not try to immediately acquire the mutex*/, guid ) ) {
				if( skipExecutionIfMutexAlreadyOwned ) {
					try {
						if( !mutex.WaitOne( 0 ) )
							return false;
					}
					catch( AbandonedMutexException ) {}
				}

				try {
					// AbandonedMutexException exists to warn us that data might be corrupt because another thread didn't properly release the mutex. We ignore it because
					// in our case, we only use the mutex in one thread per process (NOTE: This is true, but only by coincidence) and therefore don't need to worry about data corruption.
					// AbandonedMutexExceptions are thrown when the mutex is acquired, not when it is abandoned. Therefore, only the one thread that acquires the mutex
					// next will have to deal with the exception. For this reason, we are OK here in terms of only letting one thread execute its method at a time.
					try {
						// Acquire the mutex, waiting if necessary.
						mutex.WaitOne();
					}
					catch( AbandonedMutexException ) {}

					method();
				}
				finally {
					// We release the mutex manually since, yet again, nobody can agree on whether the Dispose() method called at the end of the using block always properly
					// does this for us.  Some have reported you need to do what we are doing here, so for safety's sake, we have our own finally block.

					mutex.ReleaseMutex();
				}
			}
			return true;
		}

		/// <summary>
		/// Executes the given block of code and returns the time it took to execute.
		/// </summary>
		public static TimeSpan ExecuteTimedRegion( Action method ) {
			var chrono = new Chronometer();
			method();
			return chrono.Elapsed;
		}

		/// <summary>
		/// A list of developer email addresses.
		/// </summary>
		public static List<EmailAddress> DeveloperEmailAddresses {
			get {
				assertClassInitialized();
				return InstallationConfiguration.Developers.Select( developer => new EmailAddress( developer.EmailAddress, developer.Name ) ).ToList();
			}
		}

		/// <summary>
		/// A list of system administrator email addresses.
		/// </summary>
		public static List<EmailAddress> AdministratorEmailAddresses {
			get {
				assertClassInitialized();
				// NOTE: Why is Administrator a different type than Developer?
				return InstallationConfiguration.Administrators.Select( administrator => new EmailAddress( administrator.EmailAddress, administrator.Name ) ).ToList();
			}
		}

		private static readonly object key = new object();

		private static void logError( string errorText ) {
			assertClassInitialized();

			lock( key ) {
				using( var writer = new StreamWriter( File.Open( InstallationConfiguration.ErrorLogFilePath, FileMode.Append ) ) ) {
					writer.WriteLine( DateTime.Now + ":" );
					writer.WriteLine();
					writer.Write( errorText );
					writer.WriteLine();
					writer.WriteLine();
				}
			}
		}

		internal static string CertificateEmailAddressOverride {
			get {
				assertClassInitialized();
				return InstallationConfiguration.CertificateEmailAddressOverride;
			}
		}

		/// <summary>
		/// Gets the path of the Files folder for the system.
		/// </summary>
		public static string FilesFolderPath {
			get {
				assertClassInitialized();
				return
					StandardLibraryMethods.CombinePaths(
						InstallationFileStatics.GetGeneralFilesFolderPath( InstallationConfiguration.InstallationPath,
						                                                   InstallationConfiguration.InstallationType == InstallationType.Development ),
						InstallationFileStatics.FilesFolderName );
			}
		}

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string InstallationPath {
			get {
				assertClassInitialized();
				return InstallationConfiguration.InstallationPath;
			}
		}

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string ConfigurationFolderPath {
			get {
				assertClassInitialized();
				return InstallationConfiguration.ConfigurationFolderPath;
			}
		}

		private static void assertClassInitialized() {
			if( !initialized )
				throw new ApplicationException( "Initialize the class before calling this method." );
		}
	}
}