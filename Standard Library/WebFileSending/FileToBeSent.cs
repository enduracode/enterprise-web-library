using System.Web;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// A file that will be sent to the user.
	/// </summary>
	public class FileToBeSent {
		private FullResponse response;

		/// <summary>
		/// Creates a new text file to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public FileToBeSent( string fileName, string contentType, string contents ) {
			response = new FullResponse( contentType, fileName, contents );
		}

		/// <summary>
		/// Creates a new binary file to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public FileToBeSent( string fileName, string contentType, byte[] contents ) {
			response = new FullResponse( contentType, fileName, contents );
		}

		protected void changeBinaryContents( byte[] contents ) {
			response = new FullResponse( response.ContentType, response.FileName, contents );
		}

		internal void WriteToResponse( bool sendInline ) {
			var aspNetResponse = HttpContext.Current.Response;

			aspNetResponse.ClearHeaders();
			aspNetResponse.ClearContent();
			if( response.ContentType.Length > 0 )
				aspNetResponse.ContentType = response.ContentType;
			if( !sendInline )
				aspNetResponse.AppendHeader( "content-disposition", "attachment; filename=\"" + response.FileName + "\"" );
			if( response.TextBody != null )
				aspNetResponse.Write( response.TextBody );
			else
				aspNetResponse.OutputStream.Write( response.BinaryBody, 0, response.BinaryBody.Length );
		}
	}
}