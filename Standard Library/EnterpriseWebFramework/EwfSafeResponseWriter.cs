using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that writes a response to a safe HTTP request (e.g. GET, HEAD).
	/// </summary>
	public class EwfSafeResponseWriter {
		internal const string UrlVersionStringFormat = "yyyyMMddHHmm";

		/// <summary>
		/// Returns the URL resource-version string for the specified date/time.
		/// </summary>
		public static string GetUrlVersionString( DateTimeOffset dateAndTime ) {
			return dateAndTime.ToString( UrlVersionStringFormat );
		}

		private static Action<HttpRequest, HttpResponse> createWriter(
			Func<EwfResponse> responseCreator, string urlVersionString, Func<DateTimeOffset> lastModificationDateAndTimeGetter, Func<string> memoryCacheKeyGetter ) {
			return ( aspNetRequest, aspNetResponse ) => {
				// Disable ASP.NET output caching.
				aspNetResponse.Cache.SetNoServerCaching();

				var response = new Lazy<EwfResponse>( responseCreator );

				// If we ever want to implement content negotiation, we can do so here. We can accept multiple response-creator functions instead of just one. Then, if
				// the request contains an Accept header, we can evaluate the response-creator functions here and then choose the response with the best content type.
				// If there is no match, we can send a 406 (Not Acceptable) status code and exit the method. Note that if we do add support for multiple
				// response-creator functions, we need to incorporate the chosen response's content type into the ETag if we want it to remain a strong ETag. We also
				// need to incorporate the content type into the memory-cache key.

				aspNetResponse.Cache.SetCacheability( HttpCacheability.Public );
				aspNetResponse.Cache.SetMaxAge( urlVersionString.Any() ? TimeSpan.FromDays( 365 ) : TimeSpan.Zero );

				var lastModificationDateAndTime = lastModificationDateAndTimeGetter != null ? new Lazy<DateTimeOffset>( lastModificationDateAndTimeGetter ) : null;
				string eTag;
				if( urlVersionString.Any() )
					eTag = urlVersionString;
				else if( lastModificationDateAndTimeGetter != null )
					eTag = GetUrlVersionString( lastModificationDateAndTime.Value ).Substring( 1 );
				else {
					// Buffer the response body.
					var responseWithBufferedBody = new EwfResponse(
						response.Value.ContentType,
						response.Value.BodyCreator.GetBufferedBodyCreator(),
						fileNameCreator: response.Value.FileNameCreator );
					response = new Lazy<EwfResponse>( () => responseWithBufferedBody );

					var bodyAsBinary = response.Value.BodyCreator.BodyIsText
						                   ? Encoding.UTF8.GetBytes( response.Value.BodyCreator.TextBodyCreator() )
						                   : response.Value.BodyCreator.BinaryBodyCreator();
					eTag = Convert.ToBase64String( MD5.Create().ComputeHash( bodyAsBinary ) );
				}

				// Strong ETags must vary by content coding. Since we don't know yet how this response will be encoded (gzip or otherwise), the best thing we can do is
				// use the Accept-Encoding header value in the ETag.
				var acceptEncoding = aspNetRequest.Headers[ "Accept-Encoding" ]; // returns null if field missing
				eTag += acceptEncoding ?? "";

				aspNetResponse.Cache.SetETag( eTag );

				// Sending a Last-Modified header isn't a good enough reason to force evaluation of lastModificationDateAndTimeGetter, which could be expensive.
				if( lastModificationDateAndTimeGetter != null && lastModificationDateAndTime.IsValueCreated )
					aspNetResponse.Cache.SetLastModified( lastModificationDateAndTime.Value.UtcDateTime );

				// When we separate EWF from Web Forms, we may need to add a Vary header with "Accept-Encoding". Do that here.

				var ifNoneMatch = aspNetRequest.Headers.GetValues( "If-None-Match" ); // returns null if field missing
				if( ifNoneMatch != null && ifNoneMatch.Contains( eTag ) ) {
					aspNetResponse.StatusCode = 304;
					return;
				}

				var memoryCacheKey = memoryCacheKeyGetter();
				if( memoryCacheKey.Any() ) {
					var fullResponse = FullResponse.GetFromCache( memoryCacheKey, lastModificationDateAndTime.Value, () => response.Value.CreateFullResponse() );
					response = new Lazy<EwfResponse>( () => new EwfResponse( fullResponse ) );
				}

				if( response.Value.ContentType.Length > 0 )
					aspNetResponse.ContentType = response.Value.ContentType;

				var fileName = response.Value.FileNameCreator();
				if( fileName.Any() )
					aspNetResponse.AppendHeader( "content-disposition", "attachment; filename=\"" + fileName + "\"" );

				if( aspNetRequest.HttpMethod != "HEAD" )
					response.Value.BodyCreator.WriteToResponse( aspNetResponse );
			};
		}

		private readonly Action<HttpRequest, HttpResponse> writer;

		/// <summary>
		/// Creates a response writer with a BLOB-file response.
		/// </summary>
		/// <param name="responseCreator">The response creator.</param>
		/// <param name="urlVersionString">The resource-version string from the request URL. Including a version string in a resource's URL greatly improves the
		/// cacheability of the resource, so you should use this technique whenever you have the ability to change the URL when the resource changes. Do not pass
		/// null.</param>
		/// <param name="useMemoryCacheGetter">A function that gets whether you want to use EWL's memory cache for this response. Do not pass null.</param>
		public EwfSafeResponseWriter( Func<BlobFileResponse> responseCreator, string urlVersionString, Func<bool> useMemoryCacheGetter ) {
			var response = new Lazy<BlobFileResponse>( responseCreator );
			writer = createWriter(
				() => response.Value.GetResponse(),
				urlVersionString,
				() => response.Value.FileLastModificationDateAndTime,
				() => useMemoryCacheGetter() ? response.Value.MemoryCacheKey : "" );
		}

		/// <summary>
		/// Creates a response writer with a generic response and no caching information.
		/// </summary>
		/// <param name="response">The response.</param>
		public EwfSafeResponseWriter( EwfResponse response ) {
			writer = createWriter( () => response, "", null, () => "" );
		}

		/// <summary>
		/// Creates a response writer with a generic response, a last-modification date/time (which enables conditional requests), and an optional memory-cache key.
		/// </summary>
		/// <param name="responseCreator">The response creator.</param>
		/// <param name="lastModificationDateAndTime">The last-modification date/time of the resource.</param>
		/// <param name="memoryCacheKeyGetter">A function that gets the memory-cache key for this response. Pass null or return the empty string if you do not want
		/// to use EWL's memory cache. Do not return null.</param>
		public EwfSafeResponseWriter( Func<EwfResponse> responseCreator, DateTimeOffset lastModificationDateAndTime, Func<string> memoryCacheKeyGetter = null ) {
			writer = createWriter( responseCreator, "", () => lastModificationDateAndTime, memoryCacheKeyGetter ?? ( () => "" ) );
		}

		/// <summary>
		/// Creates a response writer with a generic response, a resource-version string from the request URL, and optional memory caching information. Including a
		/// version string in a resource's URL greatly improves the cacheability of the resource, so you should use this technique whenever you have the ability to
		/// change the URL when the resource changes.
		/// </summary>
		/// <param name="responseCreator">The response creator.</param>
		/// <param name="urlVersionString">The resource-version string from the request URL. Do not pass null or the empty string.</param>
		/// <param name="memoryCachingSetupGetter">A function that gets the memory-caching setup object for the response. Pass null or return null if you do not
		/// want to use EWL's memory cache.</param>
		public EwfSafeResponseWriter( Func<EwfResponse> responseCreator, string urlVersionString, Func<ResponseMemoryCachingSetup> memoryCachingSetupGetter = null ) {
			var memoryCachingSetup = new Lazy<ResponseMemoryCachingSetup>( memoryCachingSetupGetter ?? ( () => null ) );
			writer = createWriter(
				responseCreator,
				urlVersionString,
				() => memoryCachingSetup.Value.LastModificationDateAndTime,
				() => memoryCachingSetup.Value != null ? memoryCachingSetup.Value.CacheKey : "" );
		}

		internal void WriteResponse() {
			writer( HttpContext.Current.Request, HttpContext.Current.Response );
		}
	}
}