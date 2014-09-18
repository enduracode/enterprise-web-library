using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// A file that will be sent to the user.
	/// </summary>
	public class FileToBeSent {
		internal readonly FullResponse Response;

		/// <summary>
		/// Creates a new text file to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public FileToBeSent( string fileName, string contentType, string contents ) {
			Response = new FullResponse( contentType, fileName, contents );
		}

		/// <summary>
		/// Creates a new binary file to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public FileToBeSent( string fileName, string contentType, byte[] contents ) {
			Response = new FullResponse( contentType, fileName, contents );
		}
	}
}