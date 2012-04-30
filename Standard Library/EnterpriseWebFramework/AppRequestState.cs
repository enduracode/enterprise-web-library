using System;
using System.Collections.Generic;
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
		/// Gets an open connection to the primary database.
		/// </summary>
		public static DBConnection PrimaryDatabaseConnection { get { return Instance.primaryDatabaseConnection; } }

		/// <summary>
		/// Gets an open connection to the specified secondary database.
		/// </summary>
		public static DBConnection GetSecondaryDatabaseConnection( string databaseName ) {
			return Instance.getSecondaryDatabaseConnection( databaseName );
		}

		/// <summary>
		/// Queues the specified non-transactional modification method to be executed after database transactions are committed.
		/// </summary>
		public static void AddNonTransactionalModificationMethod( Action modificationMethod ) {
			Instance.addNonTransactionalModificationMethod( modificationMethod );
		}

		private readonly DateTime beginTime;
		private readonly Uri url;
		private readonly bool homeUrlRequest = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath == NetTools.HomeUrl;

		private DBConnection primaryConnection;
		private readonly Dictionary<string, DBConnection> secondaryDatabaseNamesToConnections = new Dictionary<string, DBConnection>();
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

		private bool cachingEnabled;
		private object cache;

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
			if( !AppTools.IsLiveInstallation )
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
					"Failed to initialize URL. Host header was \"" + hostHeader + "\". User agent was \"" + HttpContext.Current.Request.GetUserAgent() + "\".", e );
			}

			TransferRequestPath = "";

			// We cache the browser capabilities so we can determine the actual browser making the request even after modifying the capabilities, which we do later in
			// the life cycle from EwfPage.
			Browser = HttpContext.Current.Request.Browser;
		}

		internal string Url { get { return url.AbsoluteUri; } }

		internal string GetBaseUrlWithSpecificSecurity( bool secure ) {
			// NOTE: Make this method decisive about a domain to use instead of just using the current one.
			return NetTools.CombineUrls( ( secure ? "https" : "http" ) + "://" + url.Host + ( url.IsDefaultPort ? "" : ( ":" + url.Port ) ),
			                             HttpRuntime.AppDomainAppVirtualPath );
		}

		internal bool HomeUrlRequest { get { return homeUrlRequest; } }

		/// <summary>
		/// If a primary connection does not already exist, creates a new connection, opens it, and begins a transaction.
		/// Otherwise, returns the existing connection which is already open and in a transaction.
		/// If the system does not have a database, returns null.
		/// </summary>
		private DBConnection primaryDatabaseConnection {
			get {
				if( !AppTools.DatabaseExists )
					return null;

				if( primaryConnection == null ) {
					// We must ensure that primary connection does not get initialized unless it is successfully created, opened, and has a transaction begun on it.
					primaryConnection = initDatabaseConnection( AppTools.GetNewPrimaryDatabaseConnection() );
				}
				return primaryConnection;
			}
		}

		/// <summary>
		/// Returns the database connection associated with the given secondary database.  If a connection does not already exist
		/// for that database, creats a new connection, opens it, and begins a new transaction.
		/// Otherwise, returns the existing connection which is already open and in a transaction.
		/// </summary>
		private DBConnection getSecondaryDatabaseConnection( string secondaryDatabaseName ) {
			DBConnection secondaryCn;
			secondaryDatabaseNamesToConnections.TryGetValue( secondaryDatabaseName, out secondaryCn );
			if( secondaryCn == null ) {
				secondaryCn = initDatabaseConnection( AppTools.GetNewSecondaryDatabaseConnection( secondaryDatabaseName ) );
				secondaryDatabaseNamesToConnections.Add( secondaryDatabaseName, secondaryCn );
			}
			return secondaryCn;
		}

		/// <summary>
		/// Returns the specified database connection only if it can be properly initialized.
		/// </summary>
		private static DBConnection initDatabaseConnection( DBConnection cn ) {
			cn.Open();
			cn.BeginTransaction();
			return cn;
		}

		private void addNonTransactionalModificationMethod( Action modificationMethod ) {
			nonTransactionalModificationMethods.Add( modificationMethod );
		}

		internal void PreExecuteCommitTimeValidationMethodsForAllOpenConnections() {
			if( primaryConnection != null )
				primaryConnection.PreExecuteCommitTimeValidationMethods();
			foreach( var secondaryConnection in secondaryDatabaseNamesToConnections.Values )
				secondaryConnection.PreExecuteCommitTimeValidationMethods();
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
				if( !userLoaded )
					loadUser();
				return user;
			}
		}

		internal bool UserAccessible { get { return !EwfApp.SupportsSecureConnections || HttpContext.Current.Request.IsSecureConnection; } }

		private void loadUser() {
			var identity = HttpContext.Current.User.Identity;

			// Try to associate a user with the current identity, if it is authenticated. Expose this user via the AuthenticatedUser property.
			if( identity.IsAuthenticated && UserManagementStatics.UserManagementEnabled ) {
				if( identity.AuthenticationType == "Forms" )
					user = UserManagementStatics.GetUser( PrimaryDatabaseConnection, int.Parse( identity.Name ) );
				else if( identity.AuthenticationType == CertificateAuthenticationModule.CertificateAuthenticationType )
					user = UserManagementStatics.GetUser( PrimaryDatabaseConnection, identity.Name );
			}

			userLoaded = true;
		}

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

		internal void EnableCache() {
			cachingEnabled = true;
		}

		/// <summary>
		/// AppTools.GetCache use only.
		/// </summary>
		internal T GetOrAddCache<T>() where T: class, new() {
			if( !cachingEnabled )
				return new T();

			if( cache == null )
				cache = new T();
			return (T)cache;
		}

		/// <summary>
		/// EwfPage use only.
		/// </summary>
		internal void ClearAndDisableCache() {
			cache = null;
			cachingEnabled = false;
		}

		internal void SetError( string prefix, Exception exception ) {
			errorPrefix = prefix;
			errorException = exception;
		}

		internal void CleanUp() {
			cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods();

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

		private void cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods() {
			var methods = new List<Action>();
			if( primaryConnection != null ) {
				methods.Add( delegate {
					var tempConnection = primaryConnection;
					primaryConnection = null;
					cleanUpDatabaseConnection( tempConnection );
				} );
			}
			foreach( var connection in secondaryDatabaseNamesToConnections.Values ) {
				var connectionCopy = connection;
				methods.Add( delegate {
					secondaryDatabaseNamesToConnections.Remove( connectionCopy.DatabaseInfo.SecondaryDatabaseName );
					cleanUpDatabaseConnection( connectionCopy );
				} );
			}
			methods.Add( () => {
				try {
					if( !transactionMarkedForRollback ) {
						foreach( var i in nonTransactionalModificationMethods )
							i();
					}
				}
				finally {
					nonTransactionalModificationMethods.Clear();
				}
			} );
			StandardLibraryMethods.CallEveryMethod( methods.ToArray() );
			transactionMarkedForRollback = false;
		}

		private void cleanUpDatabaseConnection( DBConnection cn ) {
			try {
				try {
					if( !transactionMarkedForRollback )
						cn.CommitTransaction();
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
						cn.RollbackTransaction();
				}
			}
			finally {
				cn.Close();
			}
		}
	}
}