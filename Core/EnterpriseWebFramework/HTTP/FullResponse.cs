using System;
using EnterpriseWebLibrary.Caching;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// EWF use only.
	/// </summary>
	public class FullResponse {
		internal static FullResponse GetFromCache( string key, DateTimeOffset lastModificationDateAndTime, Func<FullResponse> valueCreator ) {
			var cache = AppMemoryCache.GetCacheValue( "ewfResponse-" + key, () => new DateAndTimeVersionedCache<FullResponse>() );
			return cache.ValuesByDateAndTime.GetOrAdd( lastModificationDateAndTime, valueCreator );
		}

		internal readonly string ContentType;
		internal readonly string FileName;

		// One of these should always be null.
		internal readonly string TextBody;
		internal readonly byte[] BinaryBody;

		internal FullResponse( string contentType, string fileName, string textBody ) {
			ContentType = contentType;
			FileName = fileName;
			TextBody = textBody;
		}

		internal FullResponse( string contentType, string fileName, byte[] binaryBody ) {
			ContentType = contentType;
			FileName = fileName;
			BinaryBody = binaryBody;
		}
	}
}