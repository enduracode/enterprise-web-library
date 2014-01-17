using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.Configuration.InstallationStandard;

namespace RedStapler.StandardLibrary.Email {
	internal class EmailStatics {
		/// <summary>
		/// AppTools use only.
		/// </summary>
		internal static void SendEmail( InstallationConfiguration configuration, EmailMessage message ) {
			if( configuration.InstallationType == InstallationType.Development )
				sendEmailWithSmtpServer( null, message );
			else {
				EmailSendingService service;
				if( configuration.InstallationType == InstallationType.Live ) {
					var liveConfig = configuration.LiveInstallationConfiguration;
					service = liveConfig.EmailSendingService;
				}
				else {
					alterMessageForIntermediateInstallation( configuration, message );

					var intermediateConfig = configuration.IntermediateInstallationConfiguration;
					service = intermediateConfig.EmailSendingService;
				}

				if( service is SendGrid )
					sendEmailWithSendGrid( service as SendGrid, message );
				else if( service is SmtpServer )
					sendEmailWithSmtpServer( service as SmtpServer, message );
				else
					throw new ApplicationException( "Failed to find an email-sending provider in the installation configuration file." );
			}
		}

		private static void alterMessageForIntermediateInstallation( InstallationConfiguration configuration, EmailMessage m ) {
			m.Subject = "[{0}] ".FormatWith( configuration.FullShortName ) + m.Subject;

			var recipients = m.ToAddresses.Select( eml => eml.Address ).GetCommaDelimitedStringFromCollection();
			m.BodyHtml =
				( "Had this been a live installation, this message would have been sent from {0} to the following recipients: {1}".FormatWith(
					m.From.ToMailAddress().ToString(),
					recipients ) + Environment.NewLine + Environment.NewLine ).GetTextAsEncodedHtml() + m.BodyHtml;

			// Override the From address to enable and encourage developers to use a separate email sending service for intermediate installations. It is generally a
			// bad idea to mix testing and demo mail into deliverability reports for live mail.
			var config = configuration.IntermediateInstallationConfiguration;
			m.From = new EmailAddress( config.EmailFromAddress, config.EmailFromName );

			// Don't actually send email to recipients (they may be real people). Instead, send to the developers.
			m.ToAddresses.Clear();
			m.CcAddresses.Clear();
			m.BccAddresses.Clear();
			m.ToAddresses.AddRange( GetDeveloperEmailAddresses( configuration ) );
		}

		private static void sendEmailWithSendGrid( SendGrid sendGrid, EmailMessage message ) {
			// We want this method to use the SendGrid API, but doing so while EWL still targets .NET 4.0 creates too many NuGet package dependencies.

			// We used to cache the SmtpClient object. It turned out not to be thread safe, so now we create a new one for every email.
			var smtpClient = new System.Net.Mail.SmtpClient( "smtp.sendgrid.net" );
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

					var pickupFolderPath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "Outgoing Dev Mail" );
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

		internal static IEnumerable<EmailAddress> GetDeveloperEmailAddresses( InstallationConfiguration configuration ) {
			return configuration.Developers.Select( i => new EmailAddress( i.EmailAddress, i.Name ) );
		}
	}
}