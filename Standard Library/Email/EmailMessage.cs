using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace RedStapler.StandardLibrary.Email {
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
		}

		internal void ConfigureMailMessage( MailMessage message ) {
			message.From = From.ToMailAddress();
			addAddressesToMailAddressCollection( ToAddresses, message.To );
			addAddressesToMailAddressCollection( CcAddresses, message.CC );
			addAddressesToMailAddressCollection( BccAddresses, message.Bcc );

			message.Subject = Subject;
			message.Body = htmlToPlainText( BodyHtml );

			// Add an alternate view for the HTML part.
			message.AlternateViews.Add( AlternateView.CreateAlternateViewFromString( BodyHtml, new System.Net.Mime.ContentType( ContentTypes.Html ) ) );

			foreach( var attachment in attachments )
				message.Attachments.Add( attachment.ToAttachment() );
			foreach( var address in ReplyToAddresses )
				message.ReplyToList.Add( address.ToMailAddress() );
		}

		private static void addAddressesToMailAddressCollection( IEnumerable<EmailAddress> emailAddressCollection, MailAddressCollection mailAddressCollection ) {
			foreach( var address in emailAddressCollection )
				mailAddressCollection.Add( address.ToMailAddress() );
		}

		private static string htmlToPlainText( string html ) {
			const string emptyString = "";
			const string singleSpace = " ";

			// Maintains insert-order. Sadly, is not generic.
			var regexToReplacements = new OrderedDictionary
			                          	{
			                          		{ "(\r\n|\r|\n)+", singleSpace },
			                          		{ "\t", emptyString },
			                          		{ @"/\*.*\*/", emptyString },
			                          		{ @"<!--.*-->", emptyString },
			                          		{ @"\s+", singleSpace },
			                          		{ @"<\s*head([^>])*>.*(<\s*(/)\s*head\s*>)", emptyString },
			                          		{ @"<\s*script([^>])*>.*(<\s*(/)\s*script\s*>)", emptyString },
			                          		{ @"<\s*style([^>])*>.*(<\s*(/)\s*style\s*>)", emptyString },
			                          		{ @"<\s*td([^>])*>", "\t" },
			                          		{ @"<\s*br\s*/?>", Environment.NewLine },
			                          		{ @"<\s*li\s*>", Environment.NewLine },
			                          		{ @"<\s*div([^>])*>", Environment.NewLine + Environment.NewLine },
			                          		{ @"<\s*tr([^>])*>", Environment.NewLine + Environment.NewLine },
			                          		{ @"<\s*p([^>])*>", Environment.NewLine + Environment.NewLine },
			                          		{ RegularExpressions.HtmlTag, emptyString },
			                          		{ @"<![^>]*>", emptyString },
			                          		{ @"&bull;", " * " },
			                          		{ @"&lsaquo;", "<" },
			                          		{ @"&rsaquo;", ">" },
			                          		{ @"&trade;", "(tm)" },
			                          		{ @"&frasl;", "/" },
			                          		{ @"&lt;", "<" },
			                          		{ @"&gt;", ">" },
			                          		{ @"&copy;", "(c)" },
			                          		{ @"&reg;", "(r)" },
			                          		{ @"&(.{2,6});", emptyString },
			                          		{ Environment.NewLine + @"\s+" + Environment.NewLine, Environment.NewLine + Environment.NewLine },
			                          		{ @"\t\s+\t", "\t\t" },
			                          		{ @"\t\s+" + Environment.NewLine, "\t" + Environment.NewLine },
			                          		{ Environment.NewLine + @"\s+\t", Environment.NewLine + "\t" },
			                          		{ Environment.NewLine + @"\t+" + Environment.NewLine, Environment.NewLine + Environment.NewLine },
			                          		{ Environment.NewLine + @"\t+", Environment.NewLine + "\t" }
			                          	};

			return
				regexToReplacements.Cast<DictionaryEntry>().Aggregate( html,
				                                                       ( current, regexToReplacement ) =>
				                                                       Regex.Replace( current,
				                                                                      (string)regexToReplacement.Key,
				                                                                      (string)regexToReplacement.Value,
				                                                                      RegexOptions.IgnoreCase ) ).Trim();
		}
	}
}