using System.IO;

namespace RedStapler.StandardLibrary.Email {
	/// <summary>
	/// An email attachment.
	/// </summary>
	public class EmailAttachment {
		private readonly Stream stream;
		private readonly string contentType;
		private readonly string attachmentDisplayName;
		private readonly string filePath;

		/// <summary>
		/// Creates an email attachment. The stream must contain the contents of the attachment and be readable, but it does not need to be open.
		/// The attachmentDisplayName should be a file name with the proper extension.
		/// // NOTE: It would be kind of nice if this could use FileInfoToBeSent. THen we could have ExcelFileWriter just know how to return one of those and it would cover
		/// sending the file to the browser and emailing it.
		/// </summary>
		public EmailAttachment( Stream stream, string contentType, string attachmentDisplayName ) {
			this.stream = stream;
			this.contentType = contentType;
			this.attachmentDisplayName = attachmentDisplayName;
		}

		/// <summary>
		/// Creates an email attachment.
		/// </summary>
		public EmailAttachment( string filePath ) {
			this.filePath = filePath;
		}

		/// <summary>
		/// Converts this to a System.Net.Mail.Attachment.
		/// </summary>
		internal System.Net.Mail.Attachment ToAttachment() {
			if( stream == null )
				return new System.Net.Mail.Attachment( filePath );

			var attachment = new System.Net.Mail.Attachment( stream, contentType );
			attachment.ContentDisposition.FileName = attachmentDisplayName;
			return attachment;
		}
	}
}