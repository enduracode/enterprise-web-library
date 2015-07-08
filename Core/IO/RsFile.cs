namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Represents a file, including its contents.
	/// NOTE: Do not use. We want this class to become the only way we deal with files in memory, but we are trying to make it use a stream instead of a byte
	/// array and we fear that this change could cause the whole concept to fall apart. If we successfully make the transition to a stream, we should come up with
	/// a more permanent name for this class and try to get rid of FileToBeSent and/or FileInfoToBeSent.
	/// </summary>
	public class RsFile {
		private readonly string fileName;
		private readonly string contentType;
		private readonly byte[] contents;

		// NOTE: It's going to be tough to create Stream, byte[], and string overloads of the constructor without having a tri-modal class.

		/// <summary>
		/// Creates a new binary file to be sent. Do not pass null for the content type if you don't have it; instead pass the empty string.
		/// </summary>
		public RsFile( byte[] contents, string fileName, string contentType = "" ) {
			this.fileName = fileName;
			this.contentType = contentType;
			this.contents = contents;
		}

		/// <summary>
		/// The contents of the file.
		/// </summary>
		public byte[] Contents { get { return contents; } }

		/// <summary>
		/// The content type of the file.
		/// </summary>
		public string ContentType { get { return contentType; } }

		/// <summary>
		/// The file name. This may or may not contain path information (absolute or relative). It is simply what was passed into the constructor.
		/// </summary>
		public string FileName { get { return fileName; } }
	}
}