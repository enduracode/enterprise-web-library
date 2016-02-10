using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using Humanizer;

namespace EnterpriseWebLibrary.Email {
	public static class EmailStatics {
		/// <summary>
		/// System Manager and private use only.
		/// </summary>
		public const string InstallationIdHeaderFieldName = "X-EWL-Installation-ID";

		private static Action<EmailMessage> emailSender;

		internal static void Init() {
			if( ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Development )
				emailSender = message => sendEmailWithSmtpServer( null, message );
			else {
				Configuration.InstallationStandard.EmailSendingService service;
				if( ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Live ) {
					var liveConfig = ConfigurationStatics.InstallationConfiguration.LiveInstallationConfiguration;
					service = liveConfig.EmailSendingService;
				}
				else {
					var intermediateConfig = ConfigurationStatics.InstallationConfiguration.IntermediateInstallationConfiguration;
					service = intermediateConfig.EmailSendingService;
				}

				var sendGridService = service as Configuration.InstallationStandard.SendGrid;
				var smtpServerService = service as Configuration.InstallationStandard.SmtpServer;
				if( sendGridService != null ) {
					var webTransport = new SendGrid.Web( sendGridService.ApiKey );
					emailSender = message => {
						var sendGridMessage = getSendGridMessage( message );
						try {
							Task.Run( () => webTransport.DeliverAsync( sendGridMessage ) ).Wait();
						}
						catch( Exception e ) {
							throw new EmailSendingException( "Failed to send an email message using SendGrid.", e );
						}
					};
				}
				else if( smtpServerService != null )
					emailSender = message => sendEmailWithSmtpServer( smtpServerService, message );
				else
					throw new ApplicationException( "Failed to find an email-sending provider in the installation configuration file." );
			}
		}

		private static SendGrid.SendGridMessage getSendGridMessage( EmailMessage message ) {
			var m = new SendGrid.SendGridMessage();

			m.From = message.From.ToMailAddress();
			m.ReplyTo = message.ReplyToAddresses.Select( i => i.ToMailAddress() ).ToArray();

			m.To = message.ToAddresses.Select( i => i.ToMailAddress() ).ToArray();
			m.Cc = message.CcAddresses.Select( i => i.ToMailAddress() ).ToArray();
			m.Bcc = message.BccAddresses.Select( i => i.ToMailAddress() ).ToArray();

			m.Subject = message.Subject;

			foreach( var i in message.CustomHeaders )
				m.Headers.Add( i.Item1, i.Item2 );

			m.Text = htmlToPlainText( message.BodyHtml );
			m.Html = message.BodyHtml;

			foreach( var i in message.Attachments ) {
				if( i.Stream == null )
					m.AddAttachment( i.FilePath );
				else
					m.AddAttachment( i.Stream, i.AttachmentDisplayName );
			}

			return m;
		}

		private static void sendEmailWithSmtpServer( Configuration.InstallationStandard.SmtpServer smtpServer, EmailMessage message ) {
			// We used to cache the SmtpClient object. It turned out not to be thread safe, so now we create a new one for every email.
			var smtpClient = new System.Net.Mail.SmtpClient();
			try {
				if( smtpServer != null ) {
					smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
					smtpClient.Host = smtpServer.Server;
					if( smtpServer.PortSpecified )
						smtpClient.Port = smtpServer.Port;
					if( smtpServer.Credentials != null ) {
						smtpClient.Credentials = new System.Net.NetworkCredential( smtpServer.Credentials.UserName, smtpServer.Credentials.Password );
						smtpClient.EnableSsl = true;
					}
				}
				else {
					smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory;

					var pickupFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, "Outgoing Dev Mail" );
					Directory.CreateDirectory( pickupFolderPath );
					smtpClient.PickupDirectoryLocation = pickupFolderPath;
				}

				using( var m = new System.Net.Mail.MailMessage() ) {
					m.From = message.From.ToMailAddress();
					addAddressesToMailAddressCollection( message.ReplyToAddresses, m.ReplyToList );

					addAddressesToMailAddressCollection( message.ToAddresses, m.To );
					addAddressesToMailAddressCollection( message.CcAddresses, m.CC );
					addAddressesToMailAddressCollection( message.BccAddresses, m.Bcc );

					m.Subject = message.Subject;

					foreach( var i in message.CustomHeaders )
						m.Headers.Add( i.Item1, i.Item2 );

					m.Body = htmlToPlainText( message.BodyHtml );

					// Add an alternate view for the HTML part.
					m.AlternateViews.Add(
						System.Net.Mail.AlternateView.CreateAlternateViewFromString( message.BodyHtml, new System.Net.Mime.ContentType( ContentTypes.Html ) ) );

					foreach( var attachment in message.Attachments )
						m.Attachments.Add( attachment.ToAttachment() );

					try {
						smtpClient.Send( m );
					}
					catch( System.Net.Mail.SmtpException e ) {
						throw new EmailSendingException( "Failed to send an email message using an SMTP server.", e );
					}
				}
			}
			finally {
				// Microsoft's own dispose method fails to work if Host is not specified, even though Host doesn't need to be specified for operation.
				if( !string.IsNullOrEmpty( smtpClient.Host ) )
					smtpClient.Dispose();
			}
		}

		private static void addAddressesToMailAddressCollection(
			IEnumerable<EmailAddress> emailAddressCollection, System.Net.Mail.MailAddressCollection mailAddressCollection ) {
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
				regexToReplacements.Cast<DictionaryEntry>()
					.Aggregate(
						html,
						( current, regexToReplacement ) => Regex.Replace( current, (string)regexToReplacement.Key, (string)regexToReplacement.Value, RegexOptions.IgnoreCase ) )
					.Trim();
		}

		internal static void SendDeveloperNotificationEmail( EmailMessage message ) {
			message.From = defaultFromEmailAddress;
			message.ToAddresses.AddRange( getDeveloperEmailAddresses() );
			sendEmail( message, true );
		}

		/// <summary>
		/// After setting the From property to the from address specified in the config file, sends the specified mail message using the SMTP server specified in
		/// the config file.
		/// </summary>
		public static void SendEmailWithDefaultFromAddress( EmailMessage message ) {
			message.From = defaultFromEmailAddress;
			SendEmail( message );
		}

		private static EmailAddress defaultFromEmailAddress {
			get {
				return new EmailAddress(
					ConfigurationStatics.SystemGeneralProvider.EmailDefaultFromAddress,
					ConfigurationStatics.SystemGeneralProvider.EmailDefaultFromName );
			}
		}

		/// <summary>
		/// Sends the specified mail message using the SMTP server specified in the config file.
		/// </summary>
		public static void SendEmail( EmailMessage message ) {
			sendEmail( message, false );
		}

		private static void sendEmail( EmailMessage message, bool isDeveloperNotificationEmail ) {
			if( ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Intermediate && !isDeveloperNotificationEmail )
				alterMessageForIntermediateInstallation( message );
			emailSender( message );
		}

		private static void alterMessageForIntermediateInstallation( EmailMessage m ) {
			var originalInfoParagraph =
				"Had this been a live installation, this message would have been sent from {0} to the following recipients: {1}".FormatWith(
					m.From.ToMailAddress().ToString(),
					m.ToAddresses.Select( eml => eml.Address ).GetCommaDelimitedStringFromCollection() ) + Environment.NewLine + Environment.NewLine;

			// Override the From address to enable and encourage developers to use a separate email sending service for intermediate installations. It is generally a
			// bad idea to mix testing and demo mail into deliverability reports for live mail.
			var config = ConfigurationStatics.InstallationConfiguration.IntermediateInstallationConfiguration;
			m.From = new EmailAddress( config.EmailFromAddress, config.EmailFromName );

			// Don't actually send email to recipients (they may be real people).
			m.ToAddresses.Clear();
			m.CcAddresses.Clear();
			m.BccAddresses.Clear();

			if( ConfigurationStatics.InstallationConfiguration.RsisInstallationId.HasValue ) {
				m.ToAddresses.Add( new EmailAddress( "system-manager@enterpriseweblibrary.com", "EWL System Manager" ) );
				m.CustomHeaders.Add( Tuple.Create( InstallationIdHeaderFieldName, ConfigurationStatics.InstallationConfiguration.RsisInstallationId.Value.ToString() ) );
			}
			else {
				m.ToAddresses.AddRange( getDeveloperEmailAddresses() );
				m.Subject = "[{0}] ".FormatWith( ConfigurationStatics.InstallationConfiguration.FullShortName ) + m.Subject;
			}

			m.BodyHtml = originalInfoParagraph.GetTextAsEncodedHtml() + m.BodyHtml;
		}

		/// <summary>
		/// Returns a list of developer email addresses.
		/// </summary>
		private static IEnumerable<EmailAddress> getDeveloperEmailAddresses() {
			return ConfigurationStatics.InstallationConfiguration.Developers.Select( i => new EmailAddress( i.EmailAddress, i.Name ) );
		}

		/// <summary>
		/// Returns a list of administrator email addresses.
		/// </summary>
		public static IEnumerable<EmailAddress> GetAdministratorEmailAddresses() {
			return ConfigurationStatics.InstallationConfiguration.Administrators.Select( i => new EmailAddress( i.EmailAddress, i.Name ) );
		}

		internal static void SendHealthCheckEmail( string appFullName ) {
			var message = new EmailMessage();

			var body = new StringBuilder();
			var tenGibibytes = 10 * Math.Pow( 1024, 3 );
			var freeSpaceIsLow = false;
			foreach( var driveInfo in DriveInfo.GetDrives().Where( d => d.DriveType == DriveType.Fixed ) ) {
				var bytesFree = driveInfo.TotalFreeSpace;
				freeSpaceIsLow = freeSpaceIsLow || bytesFree < tenGibibytes;
				body.AppendLine( "{0} free on {1} drive.".FormatWith( FormattingMethods.GetFormattedBytes( bytesFree ), driveInfo.Name ) );
			}

			message.Subject = StringTools.ConcatenateWithDelimiter( " ", "Health check", freeSpaceIsLow ? "and WARNING" : "", "from " + appFullName );
			message.BodyHtml = body.ToString().GetTextAsEncodedHtml();
			SendDeveloperNotificationEmail( message );
		}
	}
}