using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Contains basic attributes of a file stored in a database, including the ID and the name.
	/// </summary>
	public class BlobFile {
		private readonly int fileId;
		private readonly string fileName;
		private readonly string contentType;
		private readonly DateTime uploadedDate;

		/// <summary>
		/// Creates a new BlobFile with the specified ID, file name, content type, and upload date. Do NOT pass null for the content type if you don't have it;
		/// instead pass the empty string.
		/// </summary>
		public BlobFile( int fileId, string fileName, string contentType, DateTime uploadedDate ) {
			this.fileId = fileId;
			this.fileName = fileName;
			this.contentType = contentType;
			this.uploadedDate = uploadedDate;
		}

		internal int FileId { get { return fileId; } }

		internal string FileName { get { return fileName; } }

		internal string ContentType { get { return contentType; } }

		internal DateTime UploadedDate { get { return uploadedDate; } }
	}
}