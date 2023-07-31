using System.Collections.Concurrent;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace EnterpriseWebLibrary.Caching;

/// <summary>
/// An intra-app memory cache.
/// </summary>
[ PublicAPI ]
public static class AppMemoryCache {
	private static readonly string keyPrefix = "{0}-".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() );
	private const int tickInterval = 10000;

	private static IMemoryCache? staticCache;
	private static Func<IMemoryCache>? cacheGetter;
	private static Timer? timer;
	private static ConcurrentBag<string>? periodicEvictionKeys;

	internal static void Init( Func<IMemoryCache>? cacheGetter ) {
		staticCache = cacheGetter is null ? new MemoryCache( new MemoryCacheOptions() ) : null;
		AppMemoryCache.cacheGetter = cacheGetter ?? ( () => staticCache! );

		timer = new Timer( tick, null, tickInterval, Timeout.Infinite );
		periodicEvictionKeys = new ConcurrentBag<string>();
	}

	internal static void CleanUp() {
		staticCache?.Dispose();

		if( timer is null )
			return;
		var waitHandle = new ManualResetEvent( false );
		timer.Dispose( waitHandle );
		waitHandle.WaitOne();
	}

	private static void tick( object? state ) {
		TelemetryStatics.ExecuteBlockWithStandardExceptionHandling(
			delegate {
				// We need to schedule the next tick even if there is an exception thrown in this one. Use try-finally instead of CallEveryMethod so we don't lose
				// exception stack traces.
				try {
					foreach( var key in periodicEvictionKeys! ) {
						var entryWrapper = cache.Get( key ) as Lazy<object>;
						if( entryWrapper == null )
							continue;
						var entry = entryWrapper.Value as PeriodicEvictionCompositeCacheEntry;
						if( entry == null )
							continue;

						entry.EvictOldEntries();
					}
				}
				finally {
					try {
						timer!.Change( tickInterval, Timeout.Infinite );
					}
					catch( ObjectDisposedException ) {
						// This should not be necessary with the Timer.Dispose overload we are using, but see http://stackoverflow.com/q/12354883/35349.
					}
				}
			} );
	}


	/// <summary>
	/// Gets the cache value associated with the specified key. If no value exists, adds one by executing the specified creator function.
	/// </summary>
	public static T GetCacheValue<T>( string key, Func<T> valueCreator ) {
		key = keyPrefix + key;

		// From http://stackoverflow.com/a/15894928/35349. Use object as the type parameter since we need covariance on the way out.
		var lazy = new Lazy<object?>(
			() => {
				var value = valueCreator();
				if( value is PeriodicEvictionCompositeCacheEntry )
					periodicEvictionKeys!.Add( key );
				return value;
			} );
		lazy = cache.GetOrCreate( key, _ => lazy );
		return (T)lazy.Value!; // There doesn’t seem to be a way to express that, if T is non-nullable, .Value will never be null.
	}


	// Framework request profiling

	internal static bool UnconditionalRequestProfilingDisabled() => cache.TryGetValue( getUnconditionalRequestProfilingDisabledKey(), out _ );

	internal static void SetUnconditionalRequestProfilingDisabled( TimeSpan duration ) {
		if( duration != TimeSpan.Zero )
			cache.Set(
				getUnconditionalRequestProfilingDisabledKey(),
				"dummy",
				new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration, Priority = CacheItemPriority.NeverRemove } );
		else
			cache.Remove( getUnconditionalRequestProfilingDisabledKey() );
	}

	private static string getUnconditionalRequestProfilingDisabledKey() => keyPrefix + "unconditionalRequestProfilingDisabled";

	internal static bool UserIsProfilingRequests( int? userId ) => cache.TryGetValue( getRequestProfilingKey( userId ), out _ );

	internal static void SetRequestProfilingForUser( int? userId, TimeSpan duration ) {
		if( duration != TimeSpan.Zero )
			cache.Set(
				getRequestProfilingKey( userId ),
				"dummy",
				new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration, Priority = CacheItemPriority.NeverRemove } );
		else
			cache.Remove( getRequestProfilingKey( userId ) );
	}

	private static string getRequestProfilingKey( int? userId ) =>
		keyPrefix + "requestProfiling-{0}".FormatWith( userId.HasValue ? userId.Value.ToString() : "unrecognized" );


	private static IMemoryCache cache => cacheGetter!();

	/// <summary>
	/// Gets the underlying .NET cache object, which you can use if you need maximum control over caching behavior.
	/// </summary>
	public static IMemoryCache UnderlyingCache => cache;
}