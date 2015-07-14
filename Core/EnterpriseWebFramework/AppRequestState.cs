using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RedStapler.StandardLibrary.Caching;
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
		private readonly string url;
		private readonly bool homeUrlRequest = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath == NetTools.HomeUrl;

		private readonly DataAccessState dataAccessState;
		private bool primaryDatabaseConnectionInitialized;
		private readonly List<string> secondaryDatabasesWithInitializedConnections = new List<string>();
		private readonly List<Action> nonTransactionalModificationMethods = new List<Action>();
		private bool transactionMarkedForRollback;

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool IntermediateUserExists { get; set; }

		private bool userEnabled;
		internal bool UserDisabledByPage { get; set; }
		private Tuple<User, Tuple<User>> userAndImpersonator;

		private string errorPrefix = "";
		private Exception errorException;
		internal string TransferRequestPath { get; set; }
		internal EwfPageRequestState EwfPageRequestState { get; set; }

		/// <summary>
		/// Do not use. This exists to support legacy behavior.
		/// </summary>
		public HttpBrowserCapabilities Browser { get; private set; }

		internal AppRequestState( string url ) {
			beginTime = DateTime.Now;
			MiniProfiler.Start();
			this.url = url;

			dataAccessState = new DataAccessState( databaseConnectionInitializer: initDatabaseConnection );
			dataAccessState.ResetCache();

			TransferRequestPath = "";

			// We cache the browser capabilities so we can determine the actual browser making the request even after modifying the capabilities, which we do later in
			// the life cycle from EwfPage.
			Browser = HttpContext.Current.Request.Browser;
		}

		/// <summary>
		/// EWF use only. This is the absolute URL for the request. Absolute means the entire URL, including the scheme, host, path, and query string.
		/// </summary>
		public string Url { get { return url; } }

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
			var userIsProfiling = UserAccessible && ( ProfilingUserId.HasValue || ImpersonatorExists ) && AppMemoryCache.UserIsProfilingRequests( ProfilingUserId );
			if( !userIsProfiling && !HttpContext.Current.Request.IsLocal && AppTools.IsLiveInstallation )
				MiniProfiler.Stop( discardResults: true );
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool ImpersonatorExists { get { return UserAndImpersonator.Item2 != null; } }

		/// <summary>
		/// EWL use only.
		/// </summary>
		public User ImpersonatorUser { get { return UserAndImpersonator.Item2.Item1; } }

		/// <summary>
		/// EWL use only.
		/// </summary>
		public int? ProfilingUserId {
			get {
				var profilingUser = ImpersonatorExists ? ImpersonatorUser : UserAndImpersonator.Item1;
				return profilingUser != null ? (int?)profilingUser.UserId : null;
			}
		}

		/// <summary>
		/// AppTools.User and private use only.
		/// </summary>
		internal Tuple<User, Tuple<User>> UserAndImpersonator {
			get {
				if( !userEnabled )
					throw new ApplicationException( "User cannot be accessed this early in the request life cycle." );
				if( UserDisabledByPage )
					throw new UserDisabledByPageException( "User cannot be accessed. See the AppTools.User documentation for details." );
				if( !UserAccessible )
					throw new ApplicationException( "User cannot be accessed from a nonsecure connection in an application that supports secure connections." );
				if( userAndImpersonator == null ) {
					userAndImpersonator = UserManagementStatics.UserManagementEnabled
						                      ? UserManagementStatics.GetUserAndImpersonatorFromRequest()
						                      : Tuple.Create<User, Tuple<User>>( null, null );
				}
				return userAndImpersonator;
			}
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool UserAccessible {
			get { return !EwfConfigurationStatics.AppSupportsSecureConnections || EwfApp.Instance.RequestIsSecure( HttpContext.Current.Request ); }
		}

		internal void ClearUserAndImpersonator() {
			userAndImpersonator = null;
		}

		/// <summary>
		/// For use by user-management post back logic only. Assumes the user and impersonator (if one exists) are loaded.
		/// </summary>
		internal void SetUser( User user ) {
			userAndImpersonator = Tuple.Create( user, userAndImpersonator.Item2 );
		}

		/// <summary>
		/// For use by impersonation post back logic only. Assumes the user and impersonator (if one exists) are loaded.
		/// </summary>
		/// <param name="user">Pass null to end impersonation. Pass a tuple to begin impersonation for the specified user or an anonymous user.</param>
		internal void SetUserAndImpersonator( Tuple<User> user ) {
			var impersonator = userAndImpersonator.Item2;
			userAndImpersonator = user != null
				                      ? Tuple.Create( user.Item1, impersonator ?? Tuple.Create( userAndImpersonator.Item1 ) )
				                      : Tuple.Create( impersonator.Item1, (Tuple<User>)null );
		}

		internal void SetError( string prefix, Exception exception ) {
			errorPrefix = prefix;
			errorException = exception;
		}

		internal void CleanUp() {
			// Skip non-transactional modification methods because they could cause database connections to be reinitialized.
			cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods( skipNonTransactionalModificationMethods: true );

			if( errorPrefix.Any() || errorException != null ) {
				TelemetryStatics.ReportError( errorPrefix, errorException );
				MiniProfiler.Stop();
			}
			else {
				var duration = DateTime.Now - beginTime;
				MiniProfiler.Stop();
				if( MiniProfiler.Current != null )
					duration = TimeSpan.FromMilliseconds( (double)MiniProfiler.Current.DurationMilliseconds );
				const int thresholdInSeconds = 30;
				if( duration > new TimeSpan( 0, 0, thresholdInSeconds ) && !AppTools.IsDevelopmentInstallation )
					TelemetryStatics.ReportError( "Request took " + duration.TotalSeconds + " seconds to process. The threshold is " + thresholdInSeconds + " seconds.", null );
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
			EwlStatics.CallEveryMethod( methods.ToArray() );
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