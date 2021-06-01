﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using NodaTime;
using StackExchange.Profiling;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The state for a request in an EWF application.
	/// </summary>
	public class AppRequestState {
		/// <summary>
		/// Gets the app request state object for the current request.
		/// </summary>
		public static AppRequestState Instance => EwfApp.Instance.RequestState;

		/// <summary>
		/// Gets the time instant for the current request.
		/// </summary>
		public static Instant RequestTime => Instance.beginInstant;

		/// <summary>
		/// Queues the specified non-transactional modification method to be executed after database transactions are committed.
		/// </summary>
		public static void AddNonTransactionalModificationMethod( Action modificationMethod ) {
			Instance.addNonTransactionalModificationMethod( modificationMethod );
		}

		private readonly Instant beginInstant;
		private readonly string url;
		private readonly string baseUrl;
		private readonly bool homeUrlRequest = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath == "~/";

		private readonly DataAccessState dataAccessState;
		private bool primaryDatabaseConnectionInitialized;
		private readonly List<string> secondaryDatabasesWithInitializedConnections = new List<string>();
		private readonly List<Action> nonTransactionalModificationMethods = new List<Action>();
		private bool transactionMarkedForRollback;

		private IReadOnlyCollection<BasicUrlHandler> urlHandlers;
		private ResourceBase resource;
		private bool newUrlParameterValuesEffective;
		internal bool UrlHandlerStateDisabled;

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool IntermediateUserExists { get; set; }

		private bool userEnabled;
		private bool userDisabled;
		private Tuple<User, SpecifiedValue<User>> userAndImpersonator;

		private readonly List<( string prefix, Exception exception )> errors = new List<( string, Exception )>();
		internal EwfPageRequestState EwfPageRequestState { get; set; }

		/// <summary>
		/// Do not use. This exists to support legacy behavior.
		/// </summary>
		public HttpBrowserCapabilities Browser { get; }

		internal AppRequestState( string url, string baseUrl ) {
			beginInstant = SystemClock.Instance.GetCurrentInstant();
			MiniProfiler.Start();
			this.url = url;
			this.baseUrl = baseUrl;

			dataAccessState = new DataAccessState( databaseConnectionInitializer: initDatabaseConnection );
			dataAccessState.ResetCache();

			// We cache the browser capabilities so we can determine the actual browser making the request even after modifying the capabilities, which we do later in
			// the life cycle from EwfPage.
			Browser = HttpContext.Current.Request.Browser;
		}

		/// <summary>
		/// EWF use only. This is the absolute URL for the request. Absolute means the entire URL, including the scheme, host, path, and query string.
		/// </summary>
		public string Url => url;

		internal string BaseUrl => baseUrl;

		internal bool HomeUrlRequest => homeUrlRequest;

		/// <summary>
		/// EwfApp.ewfApplicationStart use only.
		/// </summary>
		internal DataAccessState DataAccessState => dataAccessState;

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

		internal void SetUrlHandlers( IReadOnlyCollection<BasicUrlHandler> handlers ) {
			urlHandlers = handlers;
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public IReadOnlyCollection<BasicUrlHandler> UrlHandlers =>
			( UrlHandlerStateDisabled ? null : urlHandlers ) ?? Enumerable.Empty<BasicUrlHandler>().Materialize();

		internal void SetResource( ResourceBase resource ) {
			this.resource = resource;
		}

		internal ResourceBase Resource => UrlHandlerStateDisabled ? null : resource;

		internal void SetNewUrlParameterValuesEffective( bool effective ) {
			newUrlParameterValuesEffective = effective;
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public bool NewUrlParameterValuesEffective => newUrlParameterValuesEffective && !UrlHandlerStateDisabled;

		/// <summary>
		/// EwfApp use only.
		/// </summary>
		internal void EnableUser() {
			userEnabled = true;

			// Abandon the profiling session if it's not needed. The boolean expressions are in this order because we don't want to short circuit the user check if
			// the installation is not live or the request is local; doing so would prevent adequate testing of the user check.
			var userIsProfiling = UserAccessible && ( ProfilingUserId.HasValue || ImpersonatorExists ) && AppMemoryCache.UserIsProfilingRequests( ProfilingUserId );
			if( !userIsProfiling && !HttpContext.Current.Request.IsLocal && ConfigurationStatics.IsLiveInstallation )
				MiniProfiler.Stop( discardResults: true );
		}

		internal T ExecuteWithUserDisabled<T>( Func<T> method ) {
			userDisabled = true;
			try {
				return method();
			}
			finally {
				userDisabled = false;
			}
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool ImpersonatorExists => UserAndImpersonator.Item2 != null;

		/// <summary>
		/// EWL use only.
		/// </summary>
		public User ImpersonatorUser => UserAndImpersonator.Item2.Value;

		/// <summary>
		/// EWL use only.
		/// </summary>
		public int? ProfilingUserId => ( ImpersonatorExists ? ImpersonatorUser : UserAndImpersonator.Item1 )?.UserId;

		/// <summary>
		/// AppTools.User and private use only.
		/// </summary>
		internal Tuple<User, SpecifiedValue<User>> UserAndImpersonator {
			get {
				if( !userEnabled )
					throw new ApplicationException( "User cannot be accessed this early in the request life cycle." );
				if( userDisabled )
					throw new UserDisabledException( "User cannot be accessed. See the AppTools.User documentation for details." );
				if( !UserAccessible )
					throw new ApplicationException( "User cannot be accessed from a nonsecure connection in an application that supports secure connections." );
				if( userAndImpersonator == null )
					userAndImpersonator = UserManagementStatics.UserManagementEnabled
						                      ? UserManagementStatics.GetUserAndImpersonatorFromRequest()
						                      : Tuple.Create<User, SpecifiedValue<User>>( null, null );
				return userAndImpersonator;
			}
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool UserAccessible => !EwfConfigurationStatics.AppSupportsSecureConnections || EwfApp.Instance.RequestIsSecure( HttpContext.Current.Request );

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
		/// <param name="user">Pass null to end impersonation. Pass a value to begin impersonation for the specified user or an anonymous user.</param>
		internal void SetUserAndImpersonator( SpecifiedValue<User> user ) {
			var impersonator = userAndImpersonator.Item2;
			userAndImpersonator = user != null
				                      ? Tuple.Create( user.Value, impersonator ?? new SpecifiedValue<User>( userAndImpersonator.Item1 ) )
				                      : Tuple.Create( impersonator.Value, (SpecifiedValue<User>)null );
		}

		internal void AddError( string prefix, Exception exception ) {
			errors.Add( ( prefix, exception ) );
		}

		internal void CleanUp() {
			// Skip non-transactional modification methods because they could cause database connections to be reinitialized.
			cleanUpDatabaseConnectionsAndExecuteNonTransactionalModificationMethods( skipNonTransactionalModificationMethods: true );

			if( errors.Any() ) {
				foreach( var i in errors )
					TelemetryStatics.ReportError( i.prefix, i.exception );
				MiniProfiler.Stop();
			}
			else {
				var duration = SystemClock.Instance.GetCurrentInstant() - beginInstant;
				MiniProfiler.Stop();
				if( MiniProfiler.Current != null )
					duration = Duration.FromMilliseconds( (double)MiniProfiler.Current.DurationMilliseconds );
				const int thresholdInSeconds = 30;
				if( duration > Duration.FromSeconds( thresholdInSeconds ) && !ConfigurationStatics.IsDevelopmentInstallation )
					TelemetryStatics.ReportError(
						"Request took " + duration.TotalSeconds + " seconds to process. The threshold is " + thresholdInSeconds + " seconds.",
						null );
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