using System.Collections.Concurrent;
using System.Runtime;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using NodaTime;

namespace EnterpriseWebLibrary.Caching;

/// <summary>
/// An intra-app memory cache.
/// </summary>
[ PublicAPI ]
public static class AppMemoryCache {
	private static readonly string keyPrefix = "{0}-".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() );
	private const int tickInterval = 10000;

	private static IMemoryCache? cacheField;
	private static Timer? timer;
	private static ConcurrentBag<string>? periodicEvictionKeys;

	internal static void Init() {
		cacheField = new MemoryCache( new MemoryCacheOptions() );

		timer = new Timer( tick, null, tickInterval, Timeout.Infinite );
		periodicEvictionKeys = new ConcurrentBag<string>();
	}

	internal static void CleanUp() {
		if( timer is not null ) {
			var waitHandle = new ManualResetEvent( false );
			timer.Dispose( waitHandle );
			waitHandle.WaitOne();
		}

		cacheField?.Dispose();
	}

	private static void tick( object? state ) {
		TelemetryStatics.ExecuteBlockWithStandardExceptionHandling(
			delegate {
				// We need to schedule the next tick even if there is an exception thrown in this one. Use try-finally instead of CallEveryMethod so we don’t lose
				// exception stack traces.
				try {
					foreach( var key in periodicEvictionKeys!.Distinct() ) {
						if( cache.Get( key ) is not PeriodicEvictionCompositeCacheEntry entry )
							continue;

						entry.EvictOldEntries();
					}

					// This seems to minimize pauses during web request processing by keeping the garbage-collection backlog under control.
					GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, true, true );
				}
				finally {
					timer!.Change( tickInterval, Timeout.Infinite );
				}
			} );
	}


	/// <summary>
	/// Gets the cache value associated with the specified key. If no value exists, adds one by executing the specified creator function.
	/// </summary>
	public static T GetCacheValue<T>( string key, Func<T> valueCreator, Func<Duration?>? lifetimeGetter = null ) {
		key = keyPrefix + key;

		// This is currently subject to cache-stampede issues since the factory method can be called concurrently. We’re waiting for a new memory cache
		// implementation to solve this; see https://github.com/dotnet/runtime/issues/48567.
		return cache.GetOrCreate(
			key,
			entry => {
				var value = valueCreator();

				if( value is PeriodicEvictionCompositeCacheEntry )
					periodicEvictionKeys!.Add( key );

				var lifetime = lifetimeGetter?.Invoke();
				if( lifetime.HasValue )
					entry.AbsoluteExpirationRelativeToNow = lifetime.Value.ToTimeSpan();

				return value;
			} )!;
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


	private static IMemoryCache cache => cacheField!;

	/// <summary>
	/// Gets the underlying .NET cache object, which you can use if you need maximum control over caching behavior.
	/// </summary>
	public static IMemoryCache UnderlyingCache => cache;
}