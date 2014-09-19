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
		public SecondaryResponse( BlobFileResponse response ): this( response.GetResponse() ) {}

		/// <summary>
		/// Creates a generic secondary response.
		/// </summary>
		/// <param name="response">The generic response.</param>
		public SecondaryResponse( EwfResponse response ) {
			fullResponse = new Lazy<FullResponse>( response.CreateFullResponse );
		}

		internal void SetInSessionState() {
			StandardLibrarySessionState.Instance.ResponseToSend = fullResponse.Value;
		}

		internal bool HasFileName { get { return fullResponse.Value.FileName.Any(); } }
	}
}