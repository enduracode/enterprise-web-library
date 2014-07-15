using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using StackExchange.Profiling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The state for a request in an EWF application.
	/// </summary>
	public class AppRequestState {
		/// <summary>
		/// Gets the app request state object for the current request.
		/// </summary>
		public static AppRequestState Instance { get { return EwfApp.Instance.RequestState; } }

		/// <summary>
		/// Queues the specified non-transactional modification method to be executed after database transactions are committed.
		/// </summary>
		public static void AddNonTransactionalModificationMethod( Action modificationMethod ) {
			Instance.addNonTransactionalModificationMethod( modificationMethod );
		}

		private readonly DateTime beginTime;
		private readonly Uri url;
		private readonly bool homeUrlRequest = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath == NetTools.HomeUrl;

		private readonly DataAccessState dataAccessState;
		private bool primaryDatabaseConnectionInitialized;
		private readonly List<string> secondaryDatabasesWithInitializedConnections = new List<string>();
		private readonly List<Action> nonTransactionalModificationMethods = new List<Action>();
		private bool transactionMarkedForRollback;

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public bool IntermediateUserExists { get; set; }

		private bool userEnabled;
		internal bool UserDisabledByPage { get; set; }
		private User user;
		private bool userLoaded;

		private string errorPrefix = "";
		private Exception errorException;
		internal string TransferRequestPath { get; set; }
		internal EwfPageRequestState EwfPageRequestState { get; set; }

		/// <summary>
		/// Do not use. This exists to support legacy behavior.
		/// </summary>
		public HttpBrowserCapabilities Browser { get; private set; }

		internal AppRequestState( string hostHeader ) {
			beginTime = DateTime.Now;
			MiniProfiler.Start();

			// This used to be just HttpContext.Current.Request.Url, but that doesn't work with Azure due to the use of load balancing. An Azure load balancer will
			// bind to the ip/host/port through which all web requests should come in, and then the request is redirected to one of the server instances running this
			// web application. For example, a user will request mydomain.com. In Azure, there may be two instances running this web site, on someIp:81 and someIp:82.
			// For some reason, HttpContext.Current.Request.Url ends up using the host and port from one of these addresses instead of using the host and port from
			// the HTTP Host header, which is what the client is actually "viewing". Basically, HttpContext.Current.Request.Url returns http://someIp:81?something=1
			// instead of http://mydomain.com?something=1. See
			// http://stackoverflow.com/questions/9560838/azure-load-balancer-causes-400-error-invalid-hostname-on-postback.
			try {
				url = new Uri( HttpContext.Current.Request.Url.Scheme + "://" + hostHeader + HttpContext.Current.Request.Url.PathAndQuery );
			}
			catch( Exception e ) {
				throw new ApplicationException(
					"Failed to initialize URL. Host header was \"" + hostHeader + "\". User agent was \"" + HttpContext.Current.Request.GetUserAgent() + "\".",
					e );
			}

			dataAccessState = new DataAccessState( databaseConnectionInitializer: initDatabaseConnection );
			dataAccessState.ResetCache();

			TransferRequestPath = "";

			// We cache the browser capabilities so we can determine the actual browser making the request even after modifying the capabilities, which we do later in
			// the life cycle from EwfPage.
			Browser = HttpContext.Current.Request.Browser;
		}

		internal string Url { get { return url.AbsoluteUri; } }

		internal string GetBaseUrlWithSpecificSecurity( bool secure ) {
			// NOTE: Make this method decisive about a domain to use instead of just using the current one.
			return
				NetTools.CombineUrls(
					( secure ? "https" : "http" ) + "://" + url.Host +
					( url.IsDefaultPort
						  ? ""
						  : ( ":" +
						      ( AppTools.IsDevelopmentInstallation && url.Port == getIisExpressPort( HttpContext.Current.Request.IsSecureConnection )
							        ? getIisExpressPort( secure )
							        : url.Port ) ) ),
					HttpRuntime.AppDomainAppVirtualPath );
		}

		private int getIisExpressPort( bool secure ) {
			return secure ? 44300 : 8080;
		}

		internal bool HomeUrlRequest { get { return homeUrlRequest; } }

		/// <summary>
		/// EwfApp.ewfApplicationStart use only.
		/// </summary>
		internal DataAccessState DataAccessState { get { return dataAccessState; } }

		private void initDatabaseConnection( DBConnection connection ) {
			if( connection.DatabaseInfo.SecondaryDatabaseName.Any()
				    ? secondaryDatabasesWithInitializedConnections.Contains( connection.DatabaseInfo.SecondaryDatabaseName )
				    : primaryDatabaseConnectionInitialized )
				return;

			connection.Open();
			if( DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
				connection.BeginTransaction();

			if( connection.DatabaseInfo.SecondaryDatabaseName.Any() )
				secondaryDatabasesWithInitializedConnections.Add( connection.DatabaseInfo.SecondaryDatabaseName );
			else
				primaryDatabaseConnectionInitialized = true;
		}

		private void addNonTransactionalModificationMethod( Action modificationMethod ) {
			nonTransactionalModificationMethods.Add( modificationMethod );
		}

		internal void PreExecuteCommitTimeValidationMethodsForAllOpenConnections() {
			if( primaryDatabaseConnectionInitialized ) {
				var connection = DataAccessState.Current.PrimaryDatabaseConnection;
				if( DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
					connection.PreExecuteCommitTimeValidationMethods();
			}
			foreach( var databaseName in secondaryDatabasesWithInitializedConnections ) {
				var connection = DataAccessState.Current.GetSecondaryDatabaseConnection( databaseName );
				if( DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
					connection.PreExecuteCommitTimeValidationMethods();
			}
		}

		internal void CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods() {
			cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods();
		}

		internal void RollbackDatabaseTransactions() {
			transactionMarkedForRollback = true;
			cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods();
		}

		/// <summary>
		/// EwfApp use only.
		/// </summary>
		internal void EnableUser() {
			userEnabled = true;

			// Abandon the profiling session if it's not needed. The boolean expressions are in this order because we don't want to short circuit the user check if
			// the installation is not live or the request is local; doing so would prevent adequate testing of the user check.
			var userIsProfiling = UserAccessible && AppTools.User != null && AppMemoryCache.UserIsProfilingRequests( AppTools.User.UserId );
			if( !userIsProfiling && !HttpContext.Current.Request.IsLocal && AppTools.IsLiveInstallation )
				MiniProfiler.Stop( discardResults: true );
		}

		/// <summary>
		/// AppTools.User use only.
		/// </summary>
		internal User User {
			get {
				if( !userEnabled )
					throw new ApplicationException( "User cannot be accessed this early in the request life cycle." );
				if( UserDisabledByPage )
					throw new UserDisabledByPageException( "User cannot be accessed. See the AppTools.User documentation for details." );
				if( !UserAccessible )
					throw new ApplicationException( "User cannot be accessed from a nonsecure connection in an application that supports secure connections." );
				if( !userLoaded ) {
					user = UserManagementStatics.UserManagementEnabled ? UserManagementStatics.GetUserFromRequest() : null;
					userLoaded = true;
				}
				return user;
			}
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public bool UserAccessible { get { return !EwfApp.SupportsSecureConnections || HttpContext.Current.Request.IsSecureConnection; } }

		internal void ClearUser() {
			user = null;
			userLoaded = false;
		}

		/// <summary>
		/// For use by log in and log out post back logic only.
		/// </summary>
		public void SetUser( FormsAuthCapableUser user ) {
			this.user = user;
			userLoaded = true;
		}

		internal void SetError( string prefix, Exception exception ) {
			errorPrefix = prefix;
			errorException = exception;
		}

		internal void CleanUp() {
			// Skip non-transactional modification methods because they could cause database connections to be reinitialized.
			cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods( skipNonTransactionalModificationMethods: true );

			if( errorException != null ) {
				AppTools.EmailAndLogError( errorPrefix, errorException );
				MiniProfiler.Stop();
			}
			else {
				var duration = DateTime.Now - beginTime;
				MiniProfiler.Stop();
				if( MiniProfiler.Current != null )
					duration = TimeSpan.FromMilliseconds( (double)MiniProfiler.Current.DurationMilliseconds );
				const int thresholdInSeconds = 30;
				if( duration > new TimeSpan( 0, 0, thresholdInSeconds ) && !AppTools.IsDevelopmentInstallation )
					AppTools.EmailAndLogError( "Request took " + duration.TotalSeconds + " seconds to process. The threshold is " + thresholdInSeconds + " seconds.", null );
			}
		}

		private void cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods( bool skipNonTransactionalModificationMethods = false ) {
			var methods = new List<Action>();
			if( primaryDatabaseConnectionInitialized )
				methods.Add( () => cleanUpDatabaseConnection( DataAccessState.Current.PrimaryDatabaseConnection ) );
			foreach( var databaseName in secondaryDatabasesWithInitializedConnections ) {
				var databaseNameCopy = databaseName;
				methods.Add( () => cleanUpDatabaseConnection( DataAccessState.Current.GetSecondaryDatabaseConnection( databaseNameCopy ) ) );
			}
			methods.Add(
				() => {
					try {
						if( !skipNonTransactionalModificationMethods && !transactionMarkedForRollback ) {
							DataAccessState.Current.DisableCache();
							try {
								foreach( var i in nonTransactionalModificationMethods )
									i();
							}
							finally {
								DataAccessState.Current.ResetCache();
							}
						}
					}
					finally {
						nonTransactionalModificationMethods.Clear();
					}
				} );
			StandardLibraryMethods.CallEveryMethod( methods.ToArray() );
			transactionMarkedForRollback = false;
		}

		private void cleanUpDatabaseConnection( DBConnection connection ) {
			// Keep the connection initialized during cleanup to accommodate commit-time validation methods.
			try {
				try {
					if( !DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
						return;

					try {
						if( !transactionMarkedForRollback )
							connection.CommitTransaction();
					}
					catch {
						// Modifying this boolean here means that the order in which connections are cleaned up matters. Not modifying it here means
						// possibly committing things to secondary databases that shouldn't be committed. We've decided that the primary connection
						// is the most likely to have these errors, and is cleaned up first, so modifying this boolean here will yield the best results
						// until we implement a true distributed transaction model with two-phase commit.
						transactionMarkedForRollback = true;

						throw;
					}
					finally {
						if( transactionMarkedForRollback )
							connection.RollbackTransaction();
					}
				}
				finally {
					connection.Close();
				}
			}
			finally {
				if( connection.DatabaseInfo.SecondaryDatabaseName.Any() )
					secondaryDatabasesWithInitializedConnections.Remove( connection.DatabaseInfo.SecondaryDatabaseName );
				else
					primaryDatabaseConnectionInitialized = false;
			}
		}
	}
}