using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
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
				EmailSendingService service;
				if( ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Live ) {
					var liveConfig = ConfigurationStatics.InstallationConfiguration.LiveInstallationConfiguration;
					service = liveConfig.EmailSendingService;
				}
				else {
					var intermediateConfig = ConfigurationStatics.InstallationConfiguration.IntermediateInstallationConfiguration;
					service = intermediateConfig.EmailSendingService;
				}

				var sendGridService = service as SendGrid;
				var smtpServerService = service as SmtpServer;
				if( sendGridService != null )
					emailSender = message => sendEmailWithSendGrid( sendGridService, message );
				else if( smtpServerService != null )
					emailSender = message => sendEmailWithSmtpServer( smtpServerService, message );
				else
					throw new ApplicationException( "Failed to find an email-sending provider in the installation configuration file." );
			}
		}

		private static void sendEmailWithSendGrid( SendGrid sendGrid, EmailMessage message ) {
			// We want this method to use the SendGrid API (https://github.com/sendgrid/sendgrid-csharp), but as of 20 June 2014 it looks like the SendGrid Web API
			// does not support CC recipients!

			// We used to cache the SmtpClient object. It turned out not to be thread safe, so now we create a new one for every email.
			var smtpClient = new System.Net.Mail.SmtpClient( "smtp.sendgrid.net", 587 );
			try {
				smtpClient.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
				smtpClient.Credentials = new System.Net.NetworkCredential( sendGrid.UserName, sendGrid.Password );

				using( var m = new System.Net.Mail.MailMessage() ) {
					message.ConfigureMailMessage( m );
					try {
						smtpClient.Send( m );
					}
					catch( System.Net.Mail.SmtpException e ) {
						throw new EmailSendingException( "Failed to send an email message using SendGrid.", e );
					}
				}
			}
			finally {
				smtpClient.Dispose();
			}
		}

		private static void sendEmailWithSmtpServer( SmtpServer smtpServer, EmailMessage message ) {
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
					message.ConfigureMailMessage( m );
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