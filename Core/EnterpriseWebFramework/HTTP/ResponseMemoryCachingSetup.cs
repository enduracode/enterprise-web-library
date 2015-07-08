using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The memory-caching configuration for an HTTP response.
	/// </summary>
	public class ResponseMemoryCachingSetup {
		internal readonly string CacheKey;
		internal readonly DateTimeOffset LastModificationDateAndTime;

		/// <summary>
		/// Creates a response memory-caching setup object.
		/// </summary>
		/// <param name="cacheKey">The memory-cache key for the response. Everything that the response varies on should be incorporated into the key. Do not pass
		/// null or the empty string.</param>
		/// <param name="lastModificationDateAndTime">The last-modification date/time of the response.</param>
		public ResponseMemoryCachingSetup( string cacheKey, DateTimeOffset lastModificationDateAndTime ) {
			CacheKey = cacheKey;
			LastModificationDateAndTime = lastModificationDateAndTime;
		}
	}
}