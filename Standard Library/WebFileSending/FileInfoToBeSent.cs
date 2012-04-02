namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// Information about a file that will be sent to the user.
	/// </summary>
	public class FileInfoToBeSent {
		private readonly string fileName;
		private readonly string contentType;

		/// <summary>
		/// Creates a new file to be sent. We recommend that you always specify a content type, but if you don't have one, pass the empty string. Do NOT pass null.
		/// </summary>
		public FileInfoToBeSent( string fileName, string contentType ) {
			this.fileName = fileName;
			this.contentType = contentType;
		}

		internal string FileName { get { return fileName; } }
		internal string ContentType { get { return contentType; } }
	}
}