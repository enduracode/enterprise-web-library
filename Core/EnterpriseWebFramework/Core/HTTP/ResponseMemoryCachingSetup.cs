using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The memory-caching configuration for an HTTP response.
/// </summary>
public class ResponseMemoryCachingSetup {
	internal readonly string CacheKey;
	internal readonly Instant LastModificationTime;

	/// <summary>
	/// Creates a response memory-caching setup object.
	/// </summary>
	/// <param name="cacheKey">The memory-cache key for the response. Everything that the response varies on should be incorporated into the key. Do not pass
	/// null or the empty string.</param>
	/// <param name="lastModificationTime">The last-modification time of the response.</param>
	public ResponseMemoryCachingSetup( string cacheKey, Instant lastModificationTime ) {
		CacheKey = cacheKey;
		LastModificationTime = lastModificationTime;
	}
}