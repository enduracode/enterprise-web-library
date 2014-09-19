using System;
using System.Linq;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that writes a response to a safe HTTP request (e.g. GET, HEAD).
	/// </summary>
	public class EwfSafeResponseWriter {
		private static Action createWriter( Func<EwfResponse> responseCreator ) {
			return () => {
				var aspNetResponse = HttpContext.Current.Response;

				var response = responseCreator();
				if( response.ContentType.Length > 0 )
					aspNetResponse.ContentType = response.ContentType;

				var fileName = response.FileNameCreator();
				if( fileName.Any() )
					aspNetResponse.AppendHeader( "content-disposition", "attachment; filename=\"" + fileName + "\"" );

				response.BodyCreator.WriteToResponse( aspNetResponse );
			};
		}

		private readonly Action writer;

		/// <summary>
		/// Creates a response writer with a BLOB-file response.
		/// </summary>
		/// <param name="response">The response.</param>
		public EwfSafeResponseWriter( BlobFileResponse response ) {
			writer = createWriter( response.GetResponse );
		}

		/// <summary>
		/// Creates a response writer with a generic response and no caching information.
		/// </summary>
		/// <param name="response">The response.</param>
		public EwfSafeResponseWriter( EwfResponse response ) {
			writer = createWriter( () => response );
		}

		internal void WriteResponse() {
			writer();
		}
	}
}