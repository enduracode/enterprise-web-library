using System.Collections.Concurrent;
using System.Threading;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.TewlContrib;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;

internal class SecondaryResponseDataStore: PeriodicEvictionCompositeCacheEntry {
	private class ResponseData {
		public string Secret { get; }
		public FullResponse Response { get; }
		public Instant StorageTime { get; }

		public ResponseData( string secret, FullResponse response, Instant storageTime ) {
			Secret = secret;
			Response = response;
			StorageTime = storageTime;
		}
	}

	public static ( string secret, FullResponse response )? GetSecretAndResponse( uint id ) =>
		getDataStoreFromCache().responseDataById.TryGetValue( id, out var responseData ) ? ( responseData.Secret, responseData.Response ) : null;

	public static FullResponse? GetResponse( uint id, string secret ) =>
		!getDataStoreFromCache().responseDataById.TryGetValue( id, out var responseData ) ? null :
		!string.Equals( secret, responseData.Secret, StringComparison.Ordinal ) ? null : responseData.Response;

	public static uint AddResponse( FullResponse response ) {
		var dataStore = getDataStoreFromCache();
		var id = Interlocked.Increment( ref dataStore.responseId );
		( (IDictionary<uint, ResponseData>)dataStore.responseDataById ).Add(
			id,
			new ResponseData( RandomStatics.GetRandomHexString(), response, SystemClock.Instance.GetCurrentInstant() ) );
		return id;
	}

	private static SecondaryResponseDataStore getDataStoreFromCache() =>
		AppMemoryCache.GetCacheValue( "ewfSecondaryResponseDataStore", () => new SecondaryResponseDataStore() );

	private readonly ConcurrentDictionary<uint, ResponseData> responseDataById = new();
	private uint responseId;

	void PeriodicEvictionCompositeCacheEntry.EvictOldEntries() {
		var cutoffTime = SystemClock.Instance.GetCurrentInstant() - Duration.FromMinutes( 2 );
		foreach( var id in responseDataById.Keys )
			if( responseDataById[ id ].StorageTime < cutoffTime )
				responseDataById.TryRemove( id, out _ );
	}
}