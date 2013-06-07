using System;
using System.Runtime.Caching;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// An intra-app memory cache.
	/// </summary>
	public static class AppMemoryCache {
		private const string keyPrefix = "ewl-";


		// EWF request profiling

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static bool UserIsProfilingRequests( int userId ) {
			return cache.Contains( keyPrefix + userId );
		}

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public static void SetRequestProfilingForUser( int userId, TimeSpan duration ) {
			if( duration != TimeSpan.Zero )
				cache.Set( keyPrefix + userId,
				           "dummy",
				           new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow + duration, Priority = CacheItemPriority.NotRemovable } );
			else
				cache.Remove( keyPrefix + userId );
		}


		private static MemoryCache cache { get { return MemoryCache.Default; } }
	}
}