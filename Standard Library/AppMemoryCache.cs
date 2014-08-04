using System;
using System.Runtime.Caching;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// An intra-app memory cache.
	/// </summary>
	public static class AppMemoryCache {
		private const string keyPrefix = "ewl-";

		internal static void Init() {}
		internal static void CleanUp() {}


		/// <summary>
		/// Gets the cache value associated with the specified key. If no value exists, adds one by executing the specified creator function.
		/// </summary>
		public static T GetCacheValue<T>( string key, Func<T> valueCreator ) {
			// From http://stackoverflow.com/a/15894928/35349.
			var lazy = new Lazy<T>( valueCreator );
			lazy = (Lazy<T>)cache.AddOrGetExisting( keyPrefix + key, lazy, ObjectCache.InfiniteAbsoluteExpiration ) ?? lazy;
			return lazy.Value;
		}


		// EWF request profiling

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static bool UserIsProfilingRequests( int userId ) {
			return cache.Contains( getRequestProfilingKey( userId ) );
		}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static void SetRequestProfilingForUser( int userId, TimeSpan duration ) {
			if( duration != TimeSpan.Zero ) {
				cache.Set(
					getRequestProfilingKey( userId ),
					"dummy",
					new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow + duration, Priority = CacheItemPriority.NotRemovable } );
			}
			else
				cache.Remove( getRequestProfilingKey( userId ) );
		}

		private static string getRequestProfilingKey( int userId ) {
			return keyPrefix + "requestProfiling-" + userId;
		}


		private static MemoryCache cache { get { return MemoryCache.Default; } }
	}
}