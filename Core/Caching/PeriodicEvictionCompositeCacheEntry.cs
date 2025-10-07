namespace EnterpriseWebLibrary.Caching;

/// <summary>
/// A memory cache entry that can periodically evict data from itself.
/// </summary>
public interface PeriodicEvictionCompositeCacheEntry {
	void EvictOldEntries();
}