using System;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Threading;
using Humanizer;

namespace EnterpriseWebLibrary.Caching {
	/// <summary>
	/// An intra-app memory cache.
	/// </summary>
	public static class AppMemoryCache {
		private const string keyPrefix = "ewl-";
		private const int tickInterval = 10000;

		private static Timer timer;
		private static ConcurrentBag<string> periodicEvictionKeys;

		internal static void Init() {
			timer = new Timer( tick, null, tickInterval, Timeout.Infinite );
			periodicEvictionKeys = new ConcurrentBag<string>();
		}

		internal static void CleanUp() {
			if( timer == null )
				return;
			var waitHandle = new ManualResetEvent( false );
			timer.Dispose( waitHandle );
			waitHandle.WaitOne();
		}

		private static void tick( object state ) {
			TelemetryStatics.ExecuteBlockWithStandardExceptionHandling(
				delegate {
					// We need to schedule the next tick even if there is an exception thrown in this one. Use try-finally instead of CallEveryMethod so we don't lose
					// exception stack traces.
					try {
						foreach( var key in periodicEvictionKeys ) {
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
							timer.Change( tickInterval, Timeout.Infinite );
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
			var lazy = new Lazy<object>(
				() => {
					var value = valueCreator();
					if( value is PeriodicEvictionCompositeCacheEntry )
						periodicEvictionKeys.Add( key );
					return value;
				} );
			lazy = (Lazy<object>)cache.AddOrGetExisting( key, lazy, ObjectCache.InfiniteAbsoluteExpiration ) ?? lazy;
			return (T)lazy.Value;
		}


		// EWF request profiling

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static bool UserIsProfilingRequests( int? userId ) {
			return cache.Contains( getRequestProfilingKey( userId ) );
		}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static void SetRequestProfilingForUser( int? userId, TimeSpan duration ) {
			if( duration != TimeSpan.Zero ) {
				cache.Set(
					getRequestProfilingKey( userId ),
					"dummy",
					new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow + duration, Priority = CacheItemPriority.NotRemovable } );
			}
			else
				cache.Remove( getRequestProfilingKey( userId ) );
		}

		private static string getRequestProfilingKey( int? userId ) {
			return keyPrefix + "requestProfiling-{0}".FormatWith( userId.HasValue ? userId.Value.ToString() : "unrecognized" );
		}


		private static MemoryCache cache { get { return MemoryCache.Default; } }
	}
}