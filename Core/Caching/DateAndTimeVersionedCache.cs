using EnterpriseWebLibrary.Collections;
using NodaTime;

namespace EnterpriseWebLibrary.Caching;

internal class DateAndTimeVersionedCache<T>: PeriodicEvictionCompositeCacheEntry {
	public readonly Cache<Instant, T> ValuesByTime = new( true );

	void PeriodicEvictionCompositeCacheEntry.EvictOldEntries() {
		// When we remove values, we remove all of them because we don't ever really know which ones to keep. In some cases, the latest values could all be from
		// within a transaction that is going to roll back.
		if( ValuesByTime.Keys.Count() <= 2 )
			return;
		foreach( var key in ValuesByTime.Keys )
			ValuesByTime.Remove( key );
	}
}