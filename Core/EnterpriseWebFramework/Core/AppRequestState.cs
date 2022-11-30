using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;
using NodaTime;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The state for a request in an EWF application.
	/// </summary>
	public class AppRequestState {
		/// <summary>
		/// Gets the app request state object for the current request.
		/// </summary>
		public static AppRequestState Instance => EwfApp.RequestState;

		/// <summary>
		/// Gets the time instant for the current request.
		/// </summary>
		public static Instant RequestTime => Instance.beginInstant;

		/// <summary>
		/// Queues the specified non-transactional modification method to be executed after database transactions are committed.
		/// </summary>
		public static void AddNonTransactionalModificationMethod( Action modificationMethod ) {
			Instance.databaseConnectionManager.AddNonTransactionalModificationMethod( modificationMethod );
		}

		internal static T ExecuteWithUrlHandlerStateDisabled<T>( Func<T> method ) {
			if( EwfApp.RequestState == null )
				return method();

			Instance.urlHandlerStateDisabled = true;
			try {
				return method();
			}
			finally {
				Instance.urlHandlerStateDisabled = false;
			}
		}

		private readonly Instant beginInstant;
		private readonly string url;
		private readonly string baseUrl;

		private readonly AutomaticDatabaseConnectionManager databaseConnectionManager;

		private bool urlHandlerStateDisabled;
		private IReadOnlyCollection<BasicUrlHandler> urlHandlers;
		private ResourceBase resource;
		private bool newUrlParameterValuesEffective;

		/// <summary>
		/// EWL use only.
		/// </summary>
		public bool IntermediateUserExists { get; set; }

		private bool userEnabled;
		private bool userDisabled;
		private Tuple<User, SpecifiedValue<User>> userAndImpersonator;

		private readonly List<( string prefix, Exception exception )> errors = new List<( string, Exception )>();
		internal EwfPageRequestState EwfPageRequestState { get; set; }

		internal AppRequestState( string url, string baseUrl ) {
			beginInstant = SystemClock.Instance.GetCurrentInstant();

			var profiler = MiniProfiler.StartNew( profilerName: url );
			profiler.User = ( (MiniProfilerOptions)profiler.Options ).UserIdProvider( EwfRequest.Current.AspNetRequest );

			this.url = url;
			this.baseUrl = baseUrl;

			databaseConnectionManager = new AutomaticDatabaseConnectionManager();
			databaseConnectionManager.DataAccessState.ResetCache();
		}

		/// <summary>
		/// EWF use only. This is the absolute URL for the request. Absolute means the entire URL, including the scheme, host, path, and query string.
		/// </summary>
		public string Url => url;

		internal string BaseUrl => baseUrl;

		/// <summary>
		/// EwfInitializationOps.InitStatics use only.
		/// </summary>
		internal DataAccessState DataAccessState => databaseConnectionManager.DataAccessState;

		internal void PreExecuteCommitTimeValidationMethodsForAllOpenConnections() {
			databaseConnectionManager.PreExecuteCommitTimeValidationMethods();
		}

		internal void CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods() {
			databaseConnectionManager.CommitTransactionsAndExecuteNonTransactionalModificationMethods( true );
		}

		internal void RollbackDatabaseTransactions() {
			databaseConnectionManager.RollbackTransactions( true );
		}

		internal void SetUrlHandlers( IReadOnlyCollection<BasicUrlHandler> handlers ) {
			urlHandlers = handlers;
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public IReadOnlyCollection<BasicUrlHandler> UrlHandlers =>
			( urlHandlerStateDisabled ? null : urlHandlers ) ?? Enumerable.Empty<BasicUrlHandler>().Materialize();

		internal void SetResource( ResourceBase resource ) {
			this.resource = resource;
		}

		internal ResourceBase Resource => urlHandlerStateDisabled ? null : resource;

		internal void SetNewUrlParameterValuesEffective( bool effective ) {
			newUrlParameterValuesEffective = effective;
		}

		/// <summary>
		/// Framework use only.
		/// </summary>
		public bool NewUrlParameterValuesEffective => newUrlParameterValuesEffective && !urlHandlerStateDisabled;

		/// <summary>
		/// RequestDispatchingStatics use only.
		/// </summary>
		internal void EnableUser() {
			userEnabled = true;

			// Abandon the profiling session if it's not needed. The boolean expressions are in this order because we don't want to short circuit the user check if
			// the installation is not live or the request is local; doing so would prevent adequate testing of the user check.
			var userIsProfiling = UserAccessible && ( ProfilingUserId.HasValue || ImpersonatorExists ) && AppMemoryCache.UserIsProfilingRequests( ProfilingUserId );
			if( !userIsProfiling && !EwfRequest.Current.IsLocal && ConfigurationStatics.IsLiveInstallation )
				MiniProfiler.Current.Stop( discardResults: true );
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

		internal bool ImpersonatorExists => UserAndImpersonator.Item2 != null;

		internal User ImpersonatorUser => UserAndImpersonator.Item2.Value;

		internal int? ProfilingUserId => ( ImpersonatorExists ? ImpersonatorUser : UserAndImpersonator.Item1 )?.UserId;

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
					userAndImpersonator = AuthenticationStatics.GetUserAndImpersonatorFromRequest();
				return userAndImpersonator;
			}
		}

		internal bool UserAccessible => !EwfConfigurationStatics.AppSupportsSecureConnections || EwfRequest.Current.IsSecure;

		internal void RefreshUserAndImpersonator() {
			if( userAndImpersonator != null )
				userAndImpersonator = AuthenticationStatics.RefreshUserAndImpersonator( userAndImpersonator );
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
			databaseConnectionManager.CleanUpConnectionsAndExecuteNonTransactionalModificationMethods( true, skipNonTransactionalModificationMethods: true );

			if( errors.Any() ) {
				foreach( var i in errors )
					TelemetryStatics.ReportError( i.prefix, i.exception );
				MiniProfiler.Current?.Stop();
			}
			else {
				var duration = SystemClock.Instance.GetCurrentInstant() - beginInstant;
				MiniProfiler.Current?.Stop();
				if( MiniProfiler.Current != null )
					duration = Duration.FromMilliseconds( (double)MiniProfiler.Current.DurationMilliseconds );
				const int thresholdInSeconds = 30;
				if( duration > Duration.FromSeconds( thresholdInSeconds ) && !ConfigurationStatics.IsDevelopmentInstallation )
					TelemetryStatics.ReportError(
						"Request took " + duration.TotalSeconds + " seconds to process. The threshold is " + thresholdInSeconds + " seconds.",
						null );
			}
		}
	}
}