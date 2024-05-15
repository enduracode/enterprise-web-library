using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using JetBrains.Annotations;
using NodaTime;

namespace EnterpriseWebLibrary;

[ PublicAPI ]
public static class TelemetryStatics {
	private static Action<TextWriter>? appErrorContextWriter;
	private static RateLimiter? errorEmailLimiter;

	internal static void Init( Action<TextWriter>? appErrorContextWriter ) {
		TelemetryStatics.appErrorContextWriter = appErrorContextWriter;
		errorEmailLimiter = new RateLimiter( Duration.FromMinutes( 5 ), 10 );
	}

	/// <summary>
	/// Reports an error to the developers. The report includes the specified exception and additional information about the running program.
	/// </summary>
	public static void ReportError( Exception e ) {
		ReportError( "", e );
	}

	/// <summary>
	/// Reports an error to the developers. The report includes the specified exception and additional information about the running program. The prefix
	/// provides additional information before the standard exception and page information.
	/// </summary>
	public static void ReportError( string prefix, Exception? exception ) {
		using var sw = new StringWriter();
		if( prefix.Length > 0 ) {
			sw.WriteLine( prefix );
			sw.WriteLine();
		}
		if( exception != null ) {
			sw.WriteLine( exception.ToString() );
			sw.WriteLine();
		}

		sw.WriteLine( "Application: {0}".FormatWith( ConfigurationStatics.AppName ) );
		sw.WriteLine( "Version: {0}".FormatWith( ConfigurationStatics.AppAssembly.GetName().Version ) );

		if( !ConfigurationStatics.IsDevelopmentInstallation ) {
			sw.WriteLine();
			sw.WriteLine( "Installation: {0}".FormatWith( ConfigurationStatics.InstallationConfiguration.InstallationName ) );
			sw.WriteLine( "Machine: {0}".FormatWith( Tewl.Tools.NetTools.GetLocalHostName() ) );
		}

		appErrorContextWriter?.Invoke( sw );

		ExceptionHandlingTools.CallEveryMethod(
			() => {
				lock( errorEmailLimiter! ) {
					errorEmailLimiter.RequestAction(
						() => EmailStatics.SendDeveloperNotificationEmail( getErrorEmailMessage( sw.ToString() ) ),
						() => SendDeveloperNotification(
							"An error occurred and the email rate-limit was reached! See the log file for this and any other errors that may occur in the near future." ),
						() => {} );
				}
			},
			() => logError( sw.ToString() ) );
	}

	/// <summary>
	/// Reports an error to the developers. The report includes the specified message and additional information about the running program.
	/// </summary>
	public static void ReportError( string message ) {
		ReportError( message, null );
	}

	private static readonly object key = new();

	private static void logError( string errorText ) {
		lock( key ) {
			using var writer = new StreamWriter( File.Open( ConfigurationStatics.InstallationConfiguration.ErrorLogFilePath, FileMode.Append ) );
			writer.WriteLine( DateTime.Now + ":" );
			writer.WriteLine();
			writer.Write( errorText );
			writer.WriteLine();
			writer.WriteLine();
		}
	}

	/// <summary>
	/// Reports a fault (a problem that could later cause errors) to the developers.
	/// </summary>
	public static void ReportFault( string message ) {
		EmailStatics.SendDeveloperNotificationEmail( getFaultEmailMessage( message ) );
	}

	/// <summary>
	/// Sends a notification to the developers. Do not use for anything that requires corrective action.
	/// </summary>
	public static void SendDeveloperNotification( string message ) {
		EmailStatics.SendDeveloperNotificationEmail( getNotificationEmailMessage( message ) );
	}

	/// <summary>
	/// Reports an administrator-correctable error to the administrators.
	/// </summary>
	public static void ReportAdministratorCorrectableError( Exception e ) {
		var emailMessage = getErrorEmailMessage( e.Message );
		emailMessage.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
		EmailStatics.SendEmailWithDefaultFromAddress( emailMessage );
	}

	/// <summary>
	/// Reports an administrator-correctable error to the administrators.
	/// </summary>
	public static void ReportAdministratorCorrectableError( string message ) {
		var emailMessage = getErrorEmailMessage( message );
		emailMessage.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
		EmailStatics.SendEmailWithDefaultFromAddress( emailMessage );
	}

	/// <summary>
	/// Reports an administrator-correctable fault (a problem that could later cause errors) to the administrators.
	/// </summary>
	public static void ReportAdministratorCorrectableFault( string message ) {
		var emailMessage = getFaultEmailMessage( message );
		emailMessage.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
		EmailStatics.SendEmailWithDefaultFromAddress( emailMessage );
	}

	/// <summary>
	/// Sends a notification to the administrators. Do not use for anything that requires corrective action.
	/// </summary>
	public static void SendAdministratorNotification( string message ) {
		var emailMessage = getNotificationEmailMessage( message );
		emailMessage.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
		EmailStatics.SendEmailWithDefaultFromAddress( emailMessage );
	}

	private static EmailMessage getErrorEmailMessage( string body ) {
		return new EmailMessage
			{
				Subject = "Error in {0}".FormatWith( ConfigurationStatics.InstallationConfiguration.SystemName ) +
				          ( ConfigurationStatics.IsClientSideApp ? " on {0}".FormatWith( Tewl.Tools.NetTools.GetLocalHostName() ) : "" ),
				BodyHtml = body.GetTextAsEncodedHtml()
			};
	}

	private static EmailMessage getFaultEmailMessage( string message ) {
		return new EmailMessage
			{
				Subject = "Fault in {0}".FormatWith( ConfigurationStatics.InstallationConfiguration.SystemName ), BodyHtml = message.GetTextAsEncodedHtml()
			};
	}

	private static EmailMessage getNotificationEmailMessage( string message ) {
		return new EmailMessage
			{
				Subject = "Notification from {0}".FormatWith( ConfigurationStatics.InstallationConfiguration.SystemName ), BodyHtml = message.GetTextAsEncodedHtml()
			};
	}

	/// <summary>
	/// Executes the specified method. Returns true if it is successful. If an exception occurs, this method returns false and details about the exception are emailed
	/// to the developers and logged. This should not be necessary in web applications, as they have their own error handling.
	/// Prior to calling this, you should be sure that system logic initialization has succeeded.  This almost always means that ExecuteAppWithStandardExceptionHandling
	/// has been called somewhere up the stack.
	/// This method has no side effects other than those of the given method and the email/logging that occurs in the event of an error.  This can be used
	/// repeatedly inside any application as an alternative to a try catch block.
	/// </summary>
	public static bool ExecuteBlockWithStandardExceptionHandling( Action method ) {
		try {
			method();
			return true;
		}
		catch( Exception e ) {
			ReportError( e );
			return false;
		}
	}

	internal static void SendHealthCheck() {
		var message = new EmailMessage();

		var body = new StringBuilder();
		var tenGibibytes = 10 * Math.Pow( 1024, 3 );
		var freeSpaceIsLow = false;
		foreach( var driveInfo in DriveInfo.GetDrives().Where( d => d.DriveType == DriveType.Fixed ) ) {
			var bytesFree = driveInfo.TotalFreeSpace;
			freeSpaceIsLow = freeSpaceIsLow || bytesFree < tenGibibytes;
			body.AppendLine( "{0} free on {1} drive.".FormatWith( FormattingMethods.GetFormattedBytes( bytesFree ), driveInfo.Name ) );
		}

		message.Subject = StringTools.ConcatenateWithDelimiter(
			" ",
			"Health check",
			freeSpaceIsLow ? "and WARNING" : "",
			"from " + ConfigurationStatics.InstallationConfiguration.FullShortName + " - " + ConfigurationStatics.AppName );
		message.BodyHtml = body.ToString().GetTextAsEncodedHtml();
		EmailStatics.SendDeveloperNotificationEmail( message );
	}
}