using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.Email {
	/// <summary>
	/// An email message.
	/// </summary>
	public class EmailMessage {
		private readonly List<EmailAttachment> attachments = new List<EmailAttachment>();

		/// <summary>
		/// The from address.
		/// </summary>
		public EmailAddress From { get; set; }

		/// <summary>
		/// Collection of to addresses.
		/// </summary>
		public List<EmailAddress> ToAddresses { get; set; }

		/// <summary>
		/// Collection of CC addresses.
		/// </summary>
		public List<EmailAddress> CcAddresses { get; set; }

		/// <summary>
		/// Collection of BCC addresses.
		/// </summary>
		public List<EmailAddress> BccAddresses { get; set; }

		/// <summary>
		/// Collection of Reply-To addresses.
		/// </summary>
		public List<EmailAddress> ReplyToAddresses { get; set; }

		/// <summary>
		/// The subject.
		/// </summary>
		public string Subject { get; set; }

		internal List<Tuple<string, string>> CustomHeaders { get; set; }

		/// <summary>
		/// The body of the email. This will always be HTML. The HTML should not contain a body tag, doctype tag, or similar page-level tags.
		/// If you know this will be plain text, call NetTools.GetTextAsEncodedHtml on the text entering this property.
		/// </summary>
		public string BodyHtml { get; set; }

		/// <summary>
		/// Collection of attachments.
		/// </summary>
		public List<EmailAttachment> Attachments { get { return attachments; } }

		/// <summary>
		/// Creates a new Email message.
		/// </summary>
		public EmailMessage() {
			Subject = "";
			BodyHtml = "";
			ToAddresses = new List<EmailAddress>();
			CcAddresses = new List<EmailAddress>();
			BccAddresses = new List<EmailAddress>();
			ReplyToAddresses = new List<EmailAddress>();
			CustomHeaders = new List<Tuple<string, string>>();
		}
	}
}