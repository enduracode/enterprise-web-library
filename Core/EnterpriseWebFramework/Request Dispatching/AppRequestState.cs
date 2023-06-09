﻿using EnterpriseWebLibrary.Caching;
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
	/// Gets the request-state object for the current request. Throws an exception if called outside of a request or from a non-web application.
	/// </summary>
	public static AppRequestState Instance => RequestDispatchingStatics.RequestState;

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

	private readonly Instant beginInstant;
	private readonly string url;
	private readonly string baseUrl;

	internal readonly List<( string, string, CookieOptions )> ResponseCookies;

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

	internal bool NonLiveWarningsHidden { get; set; }

	internal string ClientSideNewUrl { get; set; }

	private readonly List<( string prefix, Exception exception )> errors = new();

	private Duration networkWaitDuration = Duration.Zero;
	private Duration slowRequestThreshold = Duration.FromMilliseconds( 5000 );

	internal EwfPageRequestState EwfPageRequestState { get; set; }

	internal AppRequestState( HttpContext context, string url, string baseUrl ) {
		beginInstant = SystemClock.Instance.GetCurrentInstant();

		var profiler = MiniProfiler.StartNew( profilerName: url );
		profiler.User = ( (MiniProfilerOptions)profiler.Options ).UserIdProvider( context.Request );

		this.url = url;
		this.baseUrl = baseUrl;

		ResponseCookies = new List<( string, string, CookieOptions )>();

		databaseConnectionManager = new AutomaticDatabaseConnectionManager();
		databaseConnectionManager.DataAccessState.ResetCache();

		ClientSideNewUrl = "";
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

		// Abandon the profiling session if it’s not needed. The boolean expressions are in this order because we don’t want to short circuit the user check if
		// the installation is not live; doing so would prevent adequate testing of the user check.
		var userIsProfiling = UserAccessible && ( ProfilingUserId.HasValue || ImpersonatorExists ) && AppMemoryCache.UserIsProfilingRequests( ProfilingUserId );
		if( !userIsProfiling && ( ConfigurationStatics.IsLiveInstallation || AppMemoryCache.UnconditionalRequestProfilingDisabled() ) )
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
	}
}