using System;

namespace EnterpriseWebLibrary.Email {
	/// <summary>
	/// An exception caused by a failure to send an email message.
	/// </summary>
	public class EmailSendingException: ApplicationException {
		/// <summary>
		/// Creates an email sending exception.
		/// </summary>
		public EmailSendingException( string message, Exception innerException ): base( message, innerException ) {}
	}
}