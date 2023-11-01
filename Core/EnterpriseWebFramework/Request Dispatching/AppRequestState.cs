#nullable disable
using System.Threading;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;
using Microsoft.AspNetCore.Http;
using NodaTime;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The state for a request in an EWF application.
/// </summary>
public class AppRequestState {
	/// <summary>
	/// Do not use. RequestDispatchingStatics.RequestState replaces this property.
	/// </summary>
	internal static AppRequestState Instance => RequestDispatchingStatics.RequestState;

	internal static T ExecuteWithUrlHandlerStateDisabled<T>( Func<T> method ) {
		if( EwfRequest.Current == null )
			return method();

		Instance.urlHandlerStateDisabled = true;
		try {
			return method();
		}
		finally {
			Instance.urlHandlerStateDisabled = false;
		}
	}

	internal readonly Instant BeginInstant;
	internal MiniProfiler Profiler { get; set; }
	internal string Url { get; private set; }
	internal string BaseUrl { get; private set; }

	internal readonly List<( string, string, CookieOptions )> ResponseCookies;

	/// <summary>
	/// EwfOps.RunApplication and private use only.
	/// </summary>
	internal AutomaticDatabaseConnectionManager DatabaseConnectionManager { get; }

	private bool urlHandlerStateDisabled;
	private IReadOnlyCollection<BasicUrlHandler> urlHandlers;
	private ResourceBase resource;
	private bool newUrlParameterValuesEffective;

	internal bool IntermediateUserExists { get; set; }

	private bool userEnabled;
	private bool userDisabled;
	private Tuple<SystemUser, SpecifiedValue<SystemUser>> userAndImpersonator;

	// page infrastructure
	internal string ClientSideNewUrl { get; set; }
	internal IReadOnlyCollection<( StatusMessageType, string )> StatusMessages { get; set; }
	internal uint? SecondaryResponseId { get; set; }
	internal PageRequestState PageRequestState { get; set; }

	private readonly List<( string prefix, Exception exception )> errors = new();

	private Duration networkWaitDuration = Duration.Zero;
	private Duration slowRequestThreshold = Duration.FromMilliseconds( 5000 );

	// request continuation
	internal SemaphoreSlim ContinuationSemaphore { get; } = new( 0, 1 );
	internal Action<HttpContext> RequestHandler { get; set; }

	internal AppRequestState( HttpContext context, string url, string baseUrl ) {
		BeginInstant = SystemClock.Instance.GetCurrentInstant();

		Profiler = MiniProfiler.StartNew( profilerName: url );
		Profiler.User = ( (MiniProfilerOptions)Profiler.Options ).UserIdProvider( context.Request );

		Url = url;
		BaseUrl = baseUrl;

		ResponseCookies = new List<( string, string, CookieOptions )>();

		DatabaseConnectionManager = new AutomaticDatabaseConnectionManager();
		DatabaseConnectionManager.DataAccessState.ResetCache();

		ClientSideNewUrl = "";
		StatusMessages = Enumerable.Empty<( StatusMessageType, string )>().Materialize();
	}

	internal void PreExecuteCommitTimeValidationMethodsForAllOpenConnections() {
		DatabaseConnectionManager.PreExecuteCommitTimeValidationMethods();
	}

	internal void CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods() {
		DatabaseConnectionManager.CommitTransactionsAndExecuteNonTransactionalModificationMethods( true );
	}

	internal void RollbackDatabaseTransactions() {
		DatabaseConnectionManager.RollbackTransactions( true );
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

		// Abandon the profiling session if it’s not needed. The boolean expressions are in this order because we don’t want to short circuit the user check if
		// the installation is not live; doing so would prevent adequate testing of the user check.
		var userIsProfiling = UserAccessible && ( ProfilingUserId.HasValue || ImpersonatorExists ) && AppMemoryCache.UserIsProfilingRequests( ProfilingUserId );
		if( !userIsProfiling && ( ConfigurationStatics.IsLiveInstallation || AppMemoryCache.UnconditionalRequestProfilingDisabled() ) )
			Profiler.Stop( discardResults: true );
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

	internal SystemUser ImpersonatorUser => UserAndImpersonator.Item2.Value;

	internal int? ProfilingUserId => ( ImpersonatorExists ? ImpersonatorUser : UserAndImpersonator.Item1 )?.UserId;

	/// <summary>
	/// AppTools.User and private use only.
	/// </summary>
	internal Tuple<SystemUser, SpecifiedValue<SystemUser>> UserAndImpersonator {
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
	internal void SetUser( SystemUser user ) {
		userAndImpersonator = Tuple.Create( user, userAndImpersonator.Item2 );
	}

	/// <summary>
	/// For use by impersonation post back logic only. Assumes the user and impersonator (if one exists) are loaded.
	/// </summary>
	/// <param name="user">Pass null to end impersonation. Pass a value to begin impersonation for the specified user or an anonymous user.</param>
	internal void SetUserAndImpersonator( SpecifiedValue<SystemUser> user ) {
		var impersonator = userAndImpersonator.Item2;
		userAndImpersonator = user != null
			                      ? Tuple.Create( user.Value, impersonator ?? new SpecifiedValue<SystemUser>( userAndImpersonator.Item1 ) )
			                      : Tuple.Create( impersonator.Value, (SpecifiedValue<SystemUser>)null );
	}

	internal ( string prefix, Exception exception ) GetLastError() => errors.Last();

	internal void AddError( string prefix, Exception exception ) {
		errors.Add( ( prefix, exception ) );
	}

	internal void AddNetworkWaitTime( Duration duration ) {
		networkWaitDuration += duration;
	}

	internal void AllowSlowRequest( bool allowUnlimitedTime = false ) {
		slowRequestThreshold = Duration.Max( allowUnlimitedTime ? Duration.MaxValue : Duration.FromMinutes( 3 ), slowRequestThreshold );
	}

	internal void ResetForContinuation( string url, string baseUrl ) {
		Url = url;
		BaseUrl = baseUrl;

		ResponseCookies.Clear();
	}

	internal void CleanUp() {
		EwlStatics.CallEveryMethod(
			() => {
				// Skip non-transactional modification methods because they could cause database connections to be reinitialized.
				DatabaseConnectionManager.CleanUpConnectionsAndExecuteNonTransactionalModificationMethods( true, skipNonTransactionalModificationMethods: true );
			},
			() => {
				if( errors.Any() ) {
					foreach( var i in errors )
						TelemetryStatics.ReportError( i.prefix, i.exception );
					Profiler?.Stop();
				}
				else {
					var duration = SystemClock.Instance.GetCurrentInstant() - BeginInstant;
					Profiler?.Stop();
					if( Profiler is not null )
						duration = Duration.FromMilliseconds( (double)Profiler.DurationMilliseconds );
					duration -= networkWaitDuration;
					if( duration > slowRequestThreshold && !ConfigurationStatics.IsDevelopmentInstallation )
						TelemetryStatics.ReportError(
							"Request took {0} to process. The threshold is {1}. If the performance problem is too difficult to fix, you can suppress this error by {2} or by {3}."
								.FormatWith(
									duration.ToTimeSpan().ToConciseString(),
									slowRequestThreshold.ToTimeSpan().ToConciseString(),
									"overriding PageBase.IsSlow (for GET request issues)",
									"overriding PageBase.dataUpdateIsSlow or using the isSlow parameter on the PostBack constructors (for post-back issues)" ),
							null );
				}
			},
			ContinuationSemaphore.Dispose );
	}
}