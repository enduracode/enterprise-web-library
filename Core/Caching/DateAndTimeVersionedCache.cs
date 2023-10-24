﻿using EnterpriseWebLibrary.Collections;

namespace EnterpriseWebLibrary.Caching;

internal class DateAndTimeVersionedCache<T>: PeriodicEvictionCompositeCacheEntry {
	public readonly Cache<DateTimeOffset, T> ValuesByDateAndTime = new( true );

	void PeriodicEvictionCompositeCacheEntry.EvictOldEntries() {
		// When we remove values, we remove all of them because we don't ever really know which ones to keep. In some cases, the latest values could all be from
		// within a transaction that is going to roll back.
		if( ValuesByDateAndTime.Keys.Count() <= 2 )
			return;
		foreach( var key in ValuesByDateAndTime.Keys )
			ValuesByDateAndTime.Remove( key );
	}
}