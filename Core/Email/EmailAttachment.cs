using System.IO;

namespace EnterpriseWebLibrary.Email {
	/// <summary>
	/// An email attachment.
	/// </summary>
	public class EmailAttachment {
		internal readonly Stream Stream;
		private readonly string contentType;
		internal readonly string AttachmentDisplayName;
		internal readonly string FilePath;

		/// <summary>
		/// Creates an email attachment. The stream must contain the contents of the attachment, be readable and must not be closed.
		/// The attachmentDisplayName should be a file name with the proper extension.
		/// NOTE: It would be kind of nice if this could use FileInfoToBeSent. THen we could have ExcelFileWriter just know how to return one of those and it would cover
		/// sending the file to the browser and emailing it.
		/// </summary>
		public EmailAttachment( Stream stream, string contentType, string attachmentDisplayName ) {
			Stream = stream;
			this.contentType = contentType;
			AttachmentDisplayName = attachmentDisplayName;
		}

		/// <summary>
		/// Creates an email attachment.
		/// </summary>
		public EmailAttachment( string filePath ) {
			FilePath = filePath;
		}

		/// <summary>
		/// Converts this to a System.Net.Mail.Attachment.
		/// </summary>
		internal System.Net.Mail.Attachment ToAttachment() {
			if( Stream == null )
				return new System.Net.Mail.Attachment( FilePath );

			var attachment = new System.Net.Mail.Attachment( Stream, contentType );
			attachment.ContentDisposition.FileName = AttachmentDisplayName;
			return attachment;
		}
	}
}