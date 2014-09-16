using System.Web;

namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// A file that will be sent to the user.
	/// </summary>
	public class FileToBeSent {
		/// <summary>
		/// File name.
		/// </summary>
		protected readonly string fileName;

		/// <summary>
		/// Content type.
		/// </summary>
		protected readonly string contentType;

		// One of these should always be null.
		private readonly string textContents;

		/// <summary>
		/// Binary contents.
		/// </summary>
		protected byte[] binaryContents;

		/// <summary>
		/// Creates a new text file to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public FileToBeSent( string fileName, string contentType, string contents ) {
			this.fileName = fileName;
			this.contentType = contentType;
			textContents = contents;
		}

		/// <summary>
		/// Creates a new binary file to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public FileToBeSent( string fileName, string contentType, byte[] contents ) {
			this.fileName = fileName;
			this.contentType = contentType;
			binaryContents = contents;
		}

		internal void WriteToResponse( bool sendInline ) {
			var response = HttpContext.Current.Response;

			response.ClearHeaders();
			response.ClearContent();
			if( contentType.Length > 0 )
				response.ContentType = contentType;
			if( !sendInline )
				response.AppendHeader( "content-disposition", "attachment; filename=\"" + fileName + "\"" );
			if( textContents != null )
				response.Write( textContents );
			else
				response.OutputStream.Write( binaryContents, 0, binaryContents.Length );
		}
	}
}