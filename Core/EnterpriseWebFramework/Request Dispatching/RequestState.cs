using System.Net;
using System.Text;
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

internal class RequestState {
	private static readonly Duration warmupPeriodDuration = Duration.FromSeconds( 5 );

	private static ulong initSecondsFromStartup;
	private static uint requestCount;
	private static Func<Instant?>? firstRequestCompletionTimeGetter;

	private class UrlHandlerState: UrlHandlerStateOverride {
		public IReadOnlyCollection<BasicUrlHandler>? Handlers { get; private set; }
		public ResourceParent? WebItem { get; private set; }

		public UrlHandlerState( bool disabled ) {
			if( !disabled )
				Handlers = [ ];
		}

		void UrlHandlerStateOverride.Set( IReadOnlyCollection<BasicUrlHandler> handlers, ResourceParent webItem ) {
			// Multiple copies of the same handler can exist in a list from UrlHandlingStatics.ResolveUrl. When a new handler object is created and it matches more
			// than one handler in the list, we want parameters to be taken from the lowest-level segment. That’s why we reverse the handlers here.
			Set( handlers.Reverse().Materialize(), webItem );
		}

		void UrlHandlerStateOverride.Set( ResourceParent webItem ) {
			var handlers = new List<BasicUrlHandler>();
			UrlHandler? handler = webItem;
			do
				handlers.Add( handler );
			while( ( handler = handler!.GetParent() ) is not null );

			Set( handlers, webItem );
		}

		public void Set( IReadOnlyCollection<BasicUrlHandler> handlers, ResourceParent webItem ) {
			Handlers = handlers;
			WebItem = webItem;
		}
	}

	internal class UrlHandlerStateOverrideMethodExecutor: UrlHandlerStateOverride.OverrideMethodExecutor {
		T UrlHandlerStateOverride.OverrideMethodExecutor.ExecuteWithUrlHandlerStateOverride<T>( SpecifiedValue<UrlHandlerStateOverride?>? state, Func<T> method ) =>
			RequestState.ExecuteWithUrlHandlerStateOverride( state, method );
	}

	internal static void Init( Func<Instant?> firstRequestCompletionTimeGetter ) {
		initSecondsFromStartup = (ulong)Duration.FromMilliseconds( Clock.GetTickCount64() ).TotalSeconds;
		RequestState.firstRequestCompletionTimeGetter = firstRequestCompletionTimeGetter;
	}

	/// <summary>
	/// Do not use. RequestDispatchingStatics.RequestState replaces this property.
	/// </summary>
	internal static RequestState Instance => RequestDispatchingStatics.RequestState;

	internal static void ExecuteWithUrlHandlerStateOverride( SpecifiedValue<UrlHandlerStateOverride?>? state, Action method ) {
		var stack = Instance.urlHandlerStateStack;
		stack.Push( state?.Value is {} value ? (UrlHandlerState)value : new UrlHandlerState( state is not null ) );
		try {
			method();
		}
		finally {
			stack.Pop();
		}
	}

	internal static T ExecuteWithUrlHandlerStateOverride<T>( SpecifiedValue<UrlHandlerStateOverride?>? state, Func<T> method ) {
		if( EwfRequest.Current is null )
			return method();

		var stack = Instance.urlHandlerStateStack;
		stack.Push( state?.Value is {} value ? (UrlHandlerState)value : new UrlHandlerState( state is not null ) );
		try {
			return method();
		}
		finally {
			stack.Pop();
		}
	}

	internal string RequestId { get; private set; }
	internal readonly Instant BeginInstant;
	private readonly bool requestInWarmupPeriod;
	internal MiniProfiler? Profiler { get; set; }
	internal string Url { get; private set; }
	internal string BaseUrl { get; private set; }
	internal IPAddress? ClientIp { get; private set; }

	internal readonly List<( string, string, CookieOptions )> ResponseCookies;

	/// <summary>
	/// EwfOps.RunApplication and private use only.
	/// </summary>
	internal AutomaticDatabaseConnectionManager DatabaseConnectionManager { get; }

	private readonly Stack<UrlHandlerState> urlHandlerStateStack = new();
	private bool newUrlParameterValuesEffective;

	internal bool IntermediateUserExists { get; set; }

	private bool userEnabled;
	private bool userDisabled;
	private ( SystemUser?, SpecifiedValue<SystemUser?>?, Instant? )? authenticationData;

	// page infrastructure
	internal string ClientSideNewUrl { get; set; }
	internal IReadOnlyCollection<( StatusMessageType, string )> StatusMessages { get; set; }
	internal uint? SecondaryResponseId { get; set; }

	private readonly List<( string prefix, Exception exception )> errors = [ ];

	private Duration networkWaitDuration = Duration.Zero;
	private Duration slowRequestThreshold;

	// request continuation
	internal SemaphoreSlim ContinuationSemaphore { get; } = new( 0, 1 );
	private Instant? continuationSemaphoreReleaseTime { get; set; }
	internal Action<HttpContext>? RequestHandler { get; set; }
	private IRequestCookieCollection? requestCookies;

	internal RequestState( HttpContext context, string url, string baseUrl, IPAddress? clientIp, SlowRequestThreshold slowRequestThreshold ) {
		BeginInstant = Clock.GetCurrentTime();
		RequestId = getRequestId( (ulong)BeginInstant.ToUnixTimeSeconds() );

		var firstRequestCompletionTime = firstRequestCompletionTimeGetter!();
		requestInWarmupPeriod = !firstRequestCompletionTime.HasValue || BeginInstant - firstRequestCompletionTime.Value < warmupPeriodDuration;

		Profiler = MiniProfiler.StartNew( profilerName: url )!;
		Profiler.User = ( (MiniProfilerOptions)Profiler.Options ).UserIdProvider( context.Request );

		Url = url;
		BaseUrl = baseUrl;
		ClientIp = clientIp;

		ResponseCookies = new List<( string, string, CookieOptions )>();

		DatabaseConnectionManager = new AutomaticDatabaseConnectionManager();
		DatabaseConnectionManager.DataAccessState.ResetCache();

		urlHandlerStateStack.Push( new UrlHandlerState( false ) );

		ClientSideNewUrl = "";
		StatusMessages = [ ];

		// Sometimes requests are slow when nightly operations are underway.
		this.slowRequestThreshold = BeginInstant.InZone( DateTimeZoneProviders.Tzdb.GetSystemDefault() ).TimeOfDay.IsInNight()
			                            ? Duration.FromMinutes( 5 )
			                            : Duration.FromMilliseconds( (long)slowRequestThreshold );
	}

	private string getRequestId( ulong unixTime ) {
		// 3 bytes for current time, in seconds; wraps approximately every six months
		var time = new byte[ 3 ];
		unchecked {
			time[ 0 ] = (byte)( unixTime >> 16 );
			time[ 1 ] = (byte)( unixTime >> 8 );
			time[ 2 ] = (byte)unixTime;
		}

		var differentiator = new byte [ 3 ];

		// 1 byte for app initialization second; prevents overlapping processes or an app restart after a clock sync from generating duplicate IDs
		var secondsSinceInit = (ulong)Duration.FromMilliseconds( Clock.GetTickCount64() ).TotalSeconds - initSecondsFromStartup;
		differentiator[ 0 ] = secondsSinceInit < byte.MaxValue ? (byte)( initSecondsFromStartup % byte.MaxValue ) : byte.MaxValue; // 255 for apps not newly-started

		// 2 bytes for request number; handles up to 65,536 requests per second without duplicating IDs
		var requestNumber = Interlocked.Increment( ref requestCount );
		differentiator[ 1 ] = (byte)( requestNumber >> 8 );
		differentiator[ 2 ] = (byte)requestNumber;

		// Add a character between these strings to handle multiple servers. See EnduraCode goal 2582. The load balancer can likely provide this in a header.
		return Convert.ToBase64String( time ) + Convert.ToBase64String( differentiator );
	}

	internal IRequestCookieCollection RequestCookies => requestCookies ?? EwfRequest.Current!.AspNetRequest.Cookies;

	internal IReadOnlyCollection<BasicUrlHandler> UrlHandlers => urlHandlerStateStack.Peek().Handlers ?? [ ];

	internal ResourceParent? WebItem => urlHandlerStateStack.Peek().WebItem;

	// This should only be called if the resource has a connection security setting that is compatible with the current request.
	internal void ForceAncestorCreationAndSetUrlHandlerState( ResourceBase resource ) {
		if( UrlHandlerStateOverridden )
			throw new InvalidOperationException();

		var handlers = new List<BasicUrlHandler>();
		ResourceParent? parent = resource;
		do
			handlers.Add( parent );
		while( ( parent = parent!.Parent ) is not null );

		urlHandlerStateStack.Peek().Set( handlers, resource );

		// New parameter values, if effective, should be applied to the ancestors we create above.
		newUrlParameterValuesEffective = false;
	}

	internal bool UrlHandlerStateOverridden => urlHandlerStateStack.Count > 1;

	internal UrlHandlerStateOverride? UrlHandlerStateOverride =>
		UrlHandlerStateOverridden && urlHandlerStateStack.Peek() is { Handlers: not null } state ? state : null;

	internal bool NewUrlParameterValuesEffective => newUrlParameterValuesEffective && urlHandlerStateStack.Peek().Handlers is { Count: > 0 };

	internal void SetNewUrlParameterValuesEffective() {
		newUrlParameterValuesEffective = true;
	}

	/// <summary>
	/// RequestDispatchingStatics use only.
	/// </summary>
	internal void EnableUser() {
		userEnabled = true;

		if( requestInWarmupPeriod )
			Profiler!.Stop( discardResults: true );
		else {
			// Abandon the profiling session if it’s not needed. The boolean expressions are in this order because we don’t want to short circuit the user check if
			// the installation is not live; doing so would prevent adequate testing of the user check.
			var userIsProfiling = UserAccessible && ( ProfilingUserId.HasValue || ImpersonatorExists ) && AppMemoryCache.UserIsProfilingRequests( ProfilingUserId );
			if( !userIsProfiling && ( ConfigurationStatics.IsLiveInstallation || AppMemoryCache.UnconditionalRequestProfilingDisabled() ) )
				Profiler!.Storage = new StackExchange.Profiling.Storage.NullStorage();
		}
	}

	internal T ExecuteWithUserDisabled<T>( Func<T> method ) {
		if( userDisabled )
			return method();

		userDisabled = true;
		try {
			return method();
		}
		finally {
			userDisabled = false;
		}
	}

	internal bool ImpersonatorExists => AuthenticationData.impersonator is not null;

	internal SystemUser? ImpersonatorUser => AuthenticationData.impersonator!.Value;

	internal int? ProfilingUserId => ( ImpersonatorExists ? ImpersonatorUser : AuthenticationData.user )?.UserId;

	/// <summary>
	/// EwfOps.RunApplication and private use only.
	/// </summary>
	internal ( SystemUser? user, SpecifiedValue<SystemUser?>? impersonator, Instant? expirationTime ) AuthenticationData {
		get {
			if( !userEnabled )
				throw new ApplicationException( "User cannot be accessed this early in the request life cycle." );
			if( userDisabled )
				throw new UserDisabledException( "User cannot be accessed. See the SystemUser.Current documentation for details." );
			if( !UserAccessible )
				throw new ApplicationException( "User cannot be accessed from a nonsecure connection in an application that supports secure connections." );
			authenticationData ??= AuthenticationStatics.GetSessionDataFromCookies();
			return authenticationData.Value;
		}
	}

	internal bool UserAccessible => !EwfConfigurationStatics.AppSupportsSecureConnections || EwfRequest.Current!.IsSecure;

	internal void ClearAuthenticationData() {
		authenticationData = null;
	}

	/// <summary>
	/// EwfOps.RunApplication use only.
	/// </summary>
	internal Action? GetUserRequestLogger() {
		// Skip on the unhandled-exception page to decrease the probability of getting another exception.
		if( GetLastError() is not null )
			return null;

		if( !UserAccessible )
			return null;

		var modMethods = new List<Action>();
		if( SystemUser.Current is {} user )
			modMethods.Add( () => UserManagementStatics.SystemProvider.InsertUserRequest( user.UserId, EwfRequest.Current!.RequestTime ) );
		if( ImpersonatorExists && ImpersonatorUser is {} impersonatorUser )
			modMethods.Add( () => UserManagementStatics.SystemProvider.InsertUserRequest( impersonatorUser.UserId, EwfRequest.Current!.RequestTime ) );
		if( !modMethods.Any() )
			return null;

		return () => {
			foreach( var i in modMethods )
				i();
		};
	}

	internal ( string prefix, Exception exception )? GetLastError() => errors.Any() ? errors.Last() : null;

	internal void AddError( string prefix, Exception exception ) {
		errors.Add( ( prefix, exception ) );
	}

	internal void AddNetworkWaitTime( Duration duration ) {
		networkWaitDuration += duration;
	}

	internal void AllowSlowRequest( bool allowUnlimitedTime = false ) {
		slowRequestThreshold = Duration.Max( allowUnlimitedTime ? Duration.MaxValue : Duration.FromMinutes( 3 ), slowRequestThreshold );
	}

	internal void ReleaseContinuationSemaphore() {
		requestCookies = RequestCookies;

		continuationSemaphoreReleaseTime = Clock.GetCurrentTime();
		ContinuationSemaphore.Release();
	}

	internal void ResetForContinuation( string url, string baseUrl ) {
		Url = url;
		BaseUrl = baseUrl;

		AddNetworkWaitTime( Clock.GetCurrentTime() - continuationSemaphoreReleaseTime!.Value );
		continuationSemaphoreReleaseTime = null;
	}

	internal bool ContinuedRequest => requestCookies is not null;

	internal void CleanUp( bool rollbackDatabaseTransactions ) {
		ExceptionHandlingTools.CallEveryMethod(
			() => {
				if( rollbackDatabaseTransactions )
					DatabaseConnectionManager.RollbackTransactions( true );
				else
					DatabaseConnectionManager.CommitTransactionsForCleanup( true );
			},
			() => {
				if( errors.Any() ) {
					foreach( var i in errors )
						TelemetryStatics.ReportError( i.prefix, i.exception );
					Profiler?.Stop();
				}
				else {
					var currentTime = Clock.GetCurrentTime();

					var duration = currentTime - BeginInstant;
					Profiler?.Stop();
					if( Profiler is not null )
						duration = Duration.FromMilliseconds( (double)Profiler.DurationMilliseconds );

					duration -= networkWaitDuration;
					if( continuationSemaphoreReleaseTime.HasValue ) {
						var releasedDuration = currentTime - continuationSemaphoreReleaseTime.Value;
						duration -= releasedDuration;
					}

					if( duration > slowRequestThreshold && !ConfigurationStatics.IsDevelopmentInstallation && !requestInWarmupPeriod )
						TelemetryStatics.ReportError(
							StringTools.ConcatenateWithDelimiter(
								" ",
								$"Request took {duration.ToTimeSpan().ToConciseString()} to process.",
								$"The threshold is {slowRequestThreshold.ToTimeSpan().ToConciseString()}.",
								"If the performance problem is too difficult to fix, you can suppress this error by {0} or by {1}.".FormatWith(
									"overriding PageBase.IsSlow (for GET request issues)",
									"overriding PageBase.dataUpdateIsSlow or using the isSlow parameter on the PostBack constructors (for post-back issues)" ),
								$"If the problem was caused by slow initial execution of a particular code path, you may be able to fix it by making a warmup request within {warmupPeriodDuration.ToTimeSpan().ToConciseString()} of the first request to the application, when performance problems are not reported." ) +
							Environment.NewLine + Environment.NewLine + "Profiler results (durations in ms):" + getProfilerResults(),
							null );
				}
			},
			ContinuationSemaphore.Dispose );
	}

	private string getProfilerResults() {
		var builder = new StringBuilder();
		foreach( var timing in Profiler!.GetTimingHierarchy() ) {
			builder.AppendLine();
			for( var i = 0; i < timing.Depth; i += 1 )
				builder.Append( ">" );
			if( timing.Depth > 0 )
				builder.Append( ' ' );
			builder.Append( $"{timing.Name} {( timing.DurationMilliseconds ?? 0 ).ToString( "#####0.##" )}" );
			if( timing.Depth > 0 )
				builder.Append( $" +{timing.StartMilliseconds.ToString( "#####0" )}" );

			if( !timing.HasCustomTimings )
				continue;

			foreach( var pair in timing.CustomTimings ) {
				var type = pair.Key;
				var customTimings = pair.Value;

				builder.Append( " (" )
					.Append( type )
					.Append( " = " )
					.Append( ( customTimings.Sum( ct => ct.DurationMilliseconds ) ?? 0 ).ToString( "###,##0.##" ) )
					.Append( " in " )
					.Append( customTimings.Count )
					.Append( " cmd" )
					.Append( customTimings.Count == 1 ? string.Empty : "s" )
					.Append( ")" );
			}
		}
		return builder.ToString();
	}
}