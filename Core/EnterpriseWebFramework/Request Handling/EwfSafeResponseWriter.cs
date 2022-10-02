﻿using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that writes a response to a safe HTTP request (e.g. GET, HEAD).
	/// </summary>
	public class EwfSafeResponseWriter {
		/// <summary>
		/// Returns the URL resource-version string for the specified date/time.
		/// </summary>
		public static string GetUrlVersionString( DateTimeOffset dateAndTime ) => dateAndTime.ToString( "yyyyMMddHHmmssfff", DateTimeFormatInfo.InvariantInfo );

		internal static void AddCacheControlHeader( HttpResponse aspNetResponse, bool requestIsSecure, bool responseHasCachingInfo, bool? responseNeverExpires ) {
			var headerValue = new CacheControlHeaderValue();

			// Assume that all HTTPS responses are private. This isn’t true for CSS, JavaScript, etc. requests that are only secure in order to match the security of
			// a page, but that’s not a big deal since most shared caches can’t open and cache HTTPS anyway.
			//
			// If we don’t have caching information, the response is probably not shareable.
			if( !requestIsSecure && responseHasCachingInfo )
				headerValue.Public = true;
			else
				headerValue.Private = true;

			if( responseNeverExpires.HasValue )
				headerValue.MaxAge = responseNeverExpires.Value ? TimeSpan.FromDays( 365 ) : TimeSpan.Zero;
			aspNetResponse.GetTypedHeaders().CacheControl = headerValue;
		}

		private static Action<HttpResponse, HttpRequest, bool> createWriter(
			Func<EwfResponse> responseCreator, string urlVersionString, string eTagBase, Func<DateTimeOffset> lastModificationDateAndTimeGetter,
			Func<string> memoryCacheKeyGetter ) {
			return ( aspNetResponse, aspNetRequest, forceImmediateResponseExpiration ) => {
				var response = new Lazy<EwfResponse>( responseCreator );

				// If we ever want to implement content negotiation, we can do so here. We can accept multiple response-creator functions instead of just one. Then, if
				// the request contains an Accept header, we can evaluate the response-creator functions here and then choose the response with the best content type.
				// If there is no match, we can send a 406 (Not Acceptable) status code and exit the method. Note that if we do add support for multiple
				// response-creator functions, we need to incorporate the chosen response's content type into the ETag if we want it to remain a strong ETag. We also
				// need to incorporate the content type into the memory-cache key.

				AddCacheControlHeader(
					aspNetResponse,
					EwfRequest.AppBaseUrlProvider.RequestIsSecure( aspNetRequest ),
					urlVersionString.Any() || eTagBase.Any() || lastModificationDateAndTimeGetter != null,
					urlVersionString.Any() && !forceImmediateResponseExpiration );

				var lastModificationDateAndTime = lastModificationDateAndTimeGetter != null ? new Lazy<DateTimeOffset>( lastModificationDateAndTimeGetter ) : null;
				string eTag;
				if( urlVersionString.Any() )
					eTag = urlVersionString;
				else if( eTagBase.Any() || lastModificationDateAndTimeGetter != null )
					eTag = eTagBase + ( lastModificationDateAndTimeGetter != null ? GetUrlVersionString( lastModificationDateAndTime.Value ) : "" );
				else {
					// Buffer the response body.
					var responseWithBufferedBody = EwfResponse.Create(
						response.Value.ContentType,
						response.Value.BodyCreator.GetBufferedBodyCreator(),
						fileNameCreator: response.Value.FileNameCreator,
						additionalHeaderFieldGetter: response.Value.AdditionalHeaderFieldGetter );
					response = new Lazy<EwfResponse>( () => responseWithBufferedBody );

					var bodyAsBinary = response.Value.BodyCreator.BodyIsText
						                   ? Encoding.UTF8.GetBytes( response.Value.BodyCreator.TextBodyCreator() )
						                   : response.Value.BodyCreator.BinaryBodyCreator();
					eTag = Convert.ToBase64String( MD5.Create().ComputeHash( bodyAsBinary ) );
				}

				// Strong ETags must vary by content coding. Since we don't know yet how this response will be encoded (gzip or otherwise), the best thing we can do is
				// use the Accept-Encoding header value in the ETag.
				eTag += StringTools.ConcatenateWithDelimiter( "", aspNetRequest.Headers.AcceptEncoding );

				var typedHeaders = aspNetResponse.GetTypedHeaders();
				eTag = "\"{0}\"".FormatWith( eTag );
				typedHeaders.ETag = new EntityTagHeaderValue( eTag );

				// Sending a Last-Modified header isn't a good enough reason to force evaluation of lastModificationDateAndTimeGetter, which could be expensive.
				if( lastModificationDateAndTimeGetter != null && lastModificationDateAndTime.IsValueCreated )
					typedHeaders.LastModified = lastModificationDateAndTime.Value.UtcDateTime;

				// When we separate EWF from Web Forms, we may need to add a Vary header with "Accept-Encoding". Do that here.

				if( aspNetRequest.Headers.IfNoneMatch.Contains( eTag ) ) {
					aspNetResponse.StatusCode = 304;
					return;
				}

				var memoryCacheKey = memoryCacheKeyGetter();
				if( memoryCacheKey.Any() ) {
					var fullResponse = FullResponse.GetFromCache( memoryCacheKey, lastModificationDateAndTime.Value, () => response.Value.CreateFullResponse() );
					response = new Lazy<EwfResponse>( () => new EwfResponse( fullResponse ) );
				}

				response.Value.WriteToAspNetResponse( aspNetResponse, omitBody: aspNetRequest.Method == "HEAD" );
			};
		}

		private readonly Action<HttpResponse, HttpRequest, bool> writer;

		/// <summary>
		/// Creates a response writer with CSS text.
		/// </summary>
		/// <param name="cssGetter">A function that gets the CSS that will be preprocessed and used as the response. Do not pass or return null.</param>
		/// <param name="urlVersionString">The resource-version string from the request URL. Do not pass null or the empty string; this parameter is required
		/// because it greatly improves cacheability, which is important for CSS. You must change the URL when the CSS changes, and you must not vary the response
		/// based on non-URL elements of the request, such as the authenticated user.</param>
		/// <param name="memoryCachingSetupGetter">A function that gets the memory-caching setup object for the response. Do not pass or return null; memory caching
		/// is important for CSS and is therefore required.</param>
		public EwfSafeResponseWriter( Func<string> cssGetter, string urlVersionString, Func<ResponseMemoryCachingSetup> memoryCachingSetupGetter ) {
			var memoryCachingSetup = new Lazy<ResponseMemoryCachingSetup>( memoryCachingSetupGetter );
			writer = createWriter(
				() => EwfResponse.Create( ContentTypes.Css, new EwfResponseBodyCreator( () => CssPreprocessor.TransformCssFile( cssGetter() ) ) ),
				urlVersionString,
				"",
				() => memoryCachingSetup.Value.LastModificationDateAndTime,
				() => memoryCachingSetup.Value.CacheKey );
		}

		/// <summary>
		/// Creates a response writer with a BLOB-file response.
		/// </summary>
		/// <param name="responseCreator">The response creator.</param>
		/// <param name="urlVersionString">The resource-version string from the request URL. Including a version string in a resource's URL greatly improves the
		/// cacheability of the resource, so you should use this technique whenever you have the ability to change the URL when the resource changes. Do not use a
		/// version string if the response will vary based on non-URL elements of the request, such as the authenticated user. Do not pass null.</param>
		/// <param name="useMemoryCacheGetter">A function that gets whether you want to use EWL's memory cache for this response. Do not pass null.</param>
		public EwfSafeResponseWriter( Func<BlobFileResponse> responseCreator, string urlVersionString, Func<bool> useMemoryCacheGetter ) {
			var response = new Lazy<BlobFileResponse>( responseCreator );

			// The ETag base is unused and unnecessary if we have a resource-version string from the request URL. Otherwise, it is only necessary if the response
			// varies based on non-URL elements of the request. We don't know if this is true, so we assume that it is.
			writer = createWriter(
				() => response.Value.GetResponse(),
				urlVersionString,
				urlVersionString.Any() ? "" : response.Value.ETagBase,
				() => response.Value.FileLastModificationDateAndTime,
				() => useMemoryCacheGetter() ? response.Value.MemoryCacheKey : "" );
		}

		/// <summary>
		/// Creates a response writer with a generic response and no caching information.
		/// </summary>
		/// <param name="response">The response.</param>
		public EwfSafeResponseWriter( EwfResponse response ) {
			writer = createWriter( () => response, "", "", null, () => "" );
		}

		/// <summary>
		/// Creates a response writer with a generic response, a last-modification date/time (which enables conditional requests), and an optional memory-cache key.
		/// Do not use this overload if the response will vary based on non-URL elements of the request, such as the authenticated user, since a parameter doesn't
		/// exist yet to incorporate those elements into the ETag.
		/// </summary>
		/// <param name="responseCreator">The response creator.</param>
		/// <param name="lastModificationDateAndTime">The last-modification date/time of the resource.</param>
		/// <param name="memoryCacheKeyGetter">A function that gets the memory-cache key for this response. Pass null or return the empty string if you do not want
		/// to use EWL's memory cache. Do not return null.</param>
		public EwfSafeResponseWriter( Func<EwfResponse> responseCreator, DateTimeOffset lastModificationDateAndTime, Func<string> memoryCacheKeyGetter = null ) {
			writer = createWriter( responseCreator, "", "", () => lastModificationDateAndTime, memoryCacheKeyGetter ?? ( () => "" ) );
		}

		/// <summary>
		/// Creates a response writer with a generic response, a resource-version string from the request URL, and optional memory caching information. Including a
		/// version string in a resource's URL greatly improves the cacheability of the resource, so you should use this technique whenever you have the ability to
		/// change the URL when the resource changes. Do not use a version string if the response will vary based on non-URL elements of the request, such as the
		/// authenticated user.
		/// </summary>
		/// <param name="responseCreator">The response creator.</param>
		/// <param name="urlVersionString">The resource-version string from the request URL. Do not pass null or the empty string.</param>
		/// <param name="memoryCachingSetupGetter">A function that gets the memory-caching setup object for the response. Pass null or return null if you do not
		/// want to use EWL's memory cache.</param>
		public EwfSafeResponseWriter(
			Func<EwfResponse> responseCreator, string urlVersionString, Func<ResponseMemoryCachingSetup> memoryCachingSetupGetter = null ) {
			var memoryCachingSetup = new Lazy<ResponseMemoryCachingSetup>( memoryCachingSetupGetter ?? ( () => null ) );
			writer = createWriter(
				responseCreator,
				urlVersionString,
				"",
				() => memoryCachingSetup.Value.LastModificationDateAndTime,
				() => memoryCachingSetup.Value != null ? memoryCachingSetup.Value.CacheKey : "" );
		}

		internal void WriteResponse( HttpContext context, bool forceImmediateResponseExpiration ) {
			writer( context.Response, context.Request, forceImmediateResponseExpiration );
		}
	}
}