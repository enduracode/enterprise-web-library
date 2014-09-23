using System;
using System.Linq;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class SecondaryResponse {
		private readonly Lazy<FullResponse> fullResponse;

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
		/// <param name="responseCreator">The generic-response creator.</param>
		/// <param name="memoryCachingSetup">The memory-caching setup object for the response. Pass null if you do not want to use EWL's memory cache.</param>
		public SecondaryResponse( Func<EwfResponse> responseCreator, ResponseMemoryCachingSetup memoryCachingSetup = null ) {
			fullResponse =
				new Lazy<FullResponse>(
					() =>
					memoryCachingSetup != null
						? FullResponse.GetFromCache( memoryCachingSetup.CacheKey, memoryCachingSetup.LastModificationDateAndTime, () => responseCreator().CreateFullResponse() )
						: responseCreator().CreateFullResponse() );
		}

		internal void SetInSessionState() {
			StandardLibrarySessionState.Instance.ResponseToSend = fullResponse.Value;
		}

		internal bool HasFileName { get { return fullResponse.Value.FileName.Any(); } }
	}
}