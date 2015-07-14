using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.Email;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary {
	public static class AppTools {
		/// <summary>
		/// Gets the name of the system.
		/// </summary>
		public static string SystemName { get { return ConfigurationStatics.InstallationConfiguration.SystemName; } }

		internal static bool IsDevelopmentInstallation {
			get { return ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Development; }
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public static bool IsIntermediateInstallation {
			get { return ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Intermediate; }
		}

		/// <summary>
		/// Gets whether this is a live installation. Use with caution. If you do not deliberately test code that only runs in live installations, you may not
		/// discover problems with it until it is live.
		/// </summary>
		public static bool IsLiveInstallation { get { return ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Live; } }

		internal static bool DatabaseExists { get { return ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo != null; } }

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
			get { return EwfApp.Instance != null && EwfApp.Instance.RequestState != null ? EwfApp.Instance.RequestState.UserAndImpersonator.Item1 : null; }
		}

		[ Obsolete( "Guaranteed through 31 July 2015. Please use TelemetryStatics.ReportError instead." ) ]
		public static void EmailAndLogError( Exception e ) {
			TelemetryStatics.ReportError( e );
		}

		[ Obsolete( "Guaranteed through 31 July 2015. Please use TelemetryStatics.ReportError instead." ) ]
		public static void EmailAndLogError( string prefix, Exception exception ) {
			TelemetryStatics.ReportError( prefix, exception );
		}

		[ Obsolete( "Guaranteed through 31 July 2015. Please use EmailStatics.SendEmailWithDefaultFromAddress instead." ) ]
		public static void SendEmailWithDefaultFromAddress( EmailMessage m ) {
			EmailStatics.SendEmailWithDefaultFromAddress( m );
		}

		[ Obsolete( "Guaranteed through 31 March 2015. Please use EmailStatics.SendEmail instead." ) ]
		public static void SendEmail( EmailMessage message ) {
			EmailStatics.SendEmail( message );
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
			try {
				method();
				return true;
			}
			catch( Exception e ) {
				TelemetryStatics.ReportError( e );
				return false;
			}
		}

		/// <summary>
		/// Use this to email errors from web service methods and turn normal exceptions into FaultExceptions.
		/// </summary>
		public static void ExecuteWebServiceWithStandardExceptionHandling( Action method ) {
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
				TelemetryStatics.ReportError( e );
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

		[ Obsolete( "Guaranteed through 31 July 2015. Please use EmailStatics.GetAdministratorEmailAddresses instead." ) ]
		public static List<EmailAddress> AdministratorEmailAddresses { get { return EmailStatics.GetAdministratorEmailAddresses().ToList(); } }

		internal static string CertificateEmailAddressOverride { get { return ConfigurationStatics.InstallationConfiguration.CertificateEmailAddressOverride; } }

		/// <summary>
		/// Gets the path of the Files folder for the system.
		/// </summary>
		public static string FilesFolderPath {
			get {
				return
					StandardLibraryMethods.CombinePaths(
						InstallationFileStatics.GetGeneralFilesFolderPath(
							ConfigurationStatics.InstallationConfiguration.InstallationPath,
							ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Development ),
						InstallationFileStatics.FilesFolderName );
			}
		}

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string InstallationPath { get { return ConfigurationStatics.InstallationConfiguration.InstallationPath; } }

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string ConfigurationFolderPath { get { return ConfigurationStatics.InstallationConfiguration.ConfigurationFolderPath; } }

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string ServerSideConsoleAppRelativeFolderPath {
			get {
				return ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Development
					       ? StandardLibraryMethods.GetProjectOutputFolderPath( true )
					       : "";
			}
		}
	}
}