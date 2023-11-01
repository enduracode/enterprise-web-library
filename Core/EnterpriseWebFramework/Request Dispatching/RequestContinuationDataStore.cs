using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.TewlContrib;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class RequestContinuationDataStore: PeriodicEvictionCompositeCacheEntry {
	private class RequestData {
		public string Url { get; }
		public string RequestMethod { get; }
		public RequestState RequestState { get; }
		public Instant StorageTime { get; }

		public RequestData( string url, string requestMethod, RequestState requestState, Instant storageTime ) {
			Url = url;
			RequestMethod = requestMethod;
			RequestState = requestState;
			StorageTime = storageTime;
		}
	}

	public static async Task<RequestState?> GetRequestState( string url, string baseUrl, string requestMethod ) {
		// This depends on the generation logic in AddRequestState.
		if( url.Length < 51 )
			return null;
		var requestId = url[ ^( 51 - 9 ).. ];
		url = url[ ..^51 ];

		if( !getDataStoreFromCache().requestDataById.TryRemove( requestId, out var requestData ) )
			return null;

		await requestData.RequestState.ContinuationSemaphore.WaitAsync();

		if( !string.Equals( url, requestData.Url, StringComparison.Ordinal ) )
			return null;

		if( !string.Equals( requestMethod, requestData.RequestMethod, StringComparison.Ordinal ) )
			return null;

		var requestState = requestData.RequestState;
		requestState.ResetForContinuation( url, baseUrl );
		return requestState;
	}

	public static string AddRequestState( string url, string requestMethod, RequestState requestState, Action<HttpContext> requestHandler ) {
		requestState.RequestHandler = requestHandler;

		var dataStore = getDataStoreFromCache();

		// Character structure: 1 parameter separator + 7 parameter name + 1 name/value separator + 9 ID number + 1 number/secret separator + 32 ID secret = 51.
		// If this changes, you must update the parsing logic in GetRequestState.
		var requestId = "{0:d9}-{1}".FormatWith( Interlocked.Increment( ref dataStore.requestIdNumber ) % 1000000000, RandomStatics.GetRandomHexString() );
		var parameter = "{0}request={1}".FormatWith( url.Contains( '?' ) ? "&" : "?", requestId );

		( (IDictionary<string, RequestData>)dataStore.requestDataById ).Add(
			requestId,
			new RequestData( url, requestMethod, requestState, SystemClock.Instance.GetCurrentInstant() ) );

		var fragmentIndex = url.IndexOf( '#' );
		return fragmentIndex >= 0 ? url.Insert( fragmentIndex, parameter ) : url + parameter;
	}

	private static RequestContinuationDataStore getDataStoreFromCache() =>
		AppMemoryCache.GetCacheValue( "ewfRequestContinuationDataStore", () => new RequestContinuationDataStore() );

	private readonly ConcurrentDictionary<string, RequestData> requestDataById = new();
	private uint requestIdNumber;

	void PeriodicEvictionCompositeCacheEntry.EvictOldEntries() {
		var cutoffTime = SystemClock.Instance.GetCurrentInstant() - Duration.FromSeconds( 30 );
		foreach( var requestId in requestDataById.Keys ) {
			if( !requestDataById.TryGetValue( requestId, out var requestData ) || requestData.StorageTime > cutoffTime )
				continue;
			if( !requestDataById.TryRemove( requestId, out _ ) )
				continue;

			var requestState = requestData.RequestState;
			requestState.RollbackDatabaseTransactions();
			requestState.CleanUp();
		}
	}
}