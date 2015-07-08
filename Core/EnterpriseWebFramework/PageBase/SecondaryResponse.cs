using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class SecondaryResponse {
		private readonly Func<FullResponse> fullResponseGetter;

		/// <summary>
		/// Creates a BLOB-file secondary response.
		/// </summary>
		/// <param name="response">The BLOB-file response.</param>
		/// <param name="useMemoryCache">Pass true if you want to use EWL's memory cache for this response.</param>
		public SecondaryResponse( BlobFileResponse response, bool useMemoryCache )
			: this(
				response.GetResponse,
				memoryCachingSetup: useMemoryCache ? new ResponseMemoryCachingSetup( response.MemoryCacheKey, response.FileLastModificationDateAndTime ) : null ) {}

		/// <summary>
		/// Creates a generic secondary response.
		/// </summary>
		/// <param name="responseCreator">The generic-response creator. Executes with the data-access cache enabled.</param>
		/// <param name="memoryCachingSetup">The memory-caching setup object for the response. Pass null if you do not want to use EWL's memory cache.</param>
		public SecondaryResponse( Func<EwfResponse> responseCreator, ResponseMemoryCachingSetup memoryCachingSetup = null ) {
			fullResponseGetter =
				() =>
				memoryCachingSetup != null
					? FullResponse.GetFromCache( memoryCachingSetup.CacheKey, memoryCachingSetup.LastModificationDateAndTime, () => responseCreator().CreateFullResponse() )
					: responseCreator().CreateFullResponse();
		}

		internal FullResponse GetFullResponse() {
			return fullResponseGetter();
		}
	}
}