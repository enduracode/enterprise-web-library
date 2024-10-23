namespace EnterpriseWebLibrary.Email;

/// <summary>
/// An email attachment.
/// </summary>
public class EmailAttachment {
	private readonly string? contentType;
	internal readonly string? FileName;
	internal readonly byte[]? Content;
	internal readonly string? FilePath;

	/// <summary>
	/// Creates an email attachment.
	/// </summary>
	/// <param name="contentType">The media type of the attachment.</param>
	/// <param name="fileName">The file name of the attachment. Should include the proper extension.</param>
	/// <param name="content">The content of the attachment.</param>
	public EmailAttachment( string contentType, string fileName, byte[] content ) {
		this.contentType = contentType;
		FileName = fileName;
		Content = content;
	}

	/// <summary>
	/// Creates an email attachment. The specified file must exist until the message is sent.
	/// </summary>
	public EmailAttachment( string filePath ) {
		FilePath = filePath;
	}

	/// <summary>
	/// Converts this to a System.Net.Mail.Attachment.
	/// </summary>
	internal System.Net.Mail.Attachment ToAttachment() {
		if( FilePath is not null )
			return new System.Net.Mail.Attachment( FilePath );

		var attachment = new System.Net.Mail.Attachment( new MemoryStream( Content! ), contentType );
		attachment.ContentDisposition!.FileName = FileName;
		return attachment;
	}
}