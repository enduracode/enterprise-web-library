﻿using System;
using System.Collections.Generic;
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
		internal readonly IReadOnlyCollection<( string, string )> AdditionalHeaderFields;

		// One of these should always be null.
		internal readonly string TextBody;
		internal readonly byte[] BinaryBody;

		internal FullResponse( string contentType, string fileName, IReadOnlyCollection<( string, string )> additionalHeaderFields, string textBody ) {
			ContentType = contentType;
			FileName = fileName;
			AdditionalHeaderFields = additionalHeaderFields;
			TextBody = textBody;
		}

		internal FullResponse( string contentType, string fileName, IReadOnlyCollection<( string, string )> additionalHeaderFields, byte[] binaryBody ) {
			ContentType = contentType;
			FileName = fileName;
			AdditionalHeaderFields = additionalHeaderFields;
			BinaryBody = binaryBody;
		}
	}
}