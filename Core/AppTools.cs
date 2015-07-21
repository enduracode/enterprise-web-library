using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

namespace EnterpriseWebLibrary {
	// Do NOT add anything new to this class; we will delete it after we figure out where to move the User property.
	public static class AppTools {
		/// <summary>
		/// Gets the user object for the authenticated user. Returns null if the user has not been authenticated. In a web application, do not use from the
		/// initDefaultOptionalParameterPackage or init methods of Info classes because the page has not yet been able to correct the connection security of the
		/// request, if necessary, and because parent authorization logic has not yet executed. To use from initUserDefaultOptionalParameterPackage you must
		/// explicitly specify the connection security as SecureIfPossible in all pages and entity setups that use this item as a parent. To use from
		/// createParentPageInfo--which you should only do if, for a given set of parameters, there is no single parent that all users can access--you must
		/// explicitly specify the connection security as SecureIfPossible in the current item. With both of these uses, keep in mind that no parent authorization
		/// logic has executed and therefore you cannot assume anything about the user. Does not currently work outside of web applications.
		/// </summary>
		public static User User {
			get { return EwfApp.Instance != null && EwfApp.Instance.RequestState != null ? EwfApp.Instance.RequestState.UserAndImpersonator.Item1 : null; }
		}

		[ Obsolete( "Guaranteed through 31 July 2015. Please use TelemetryStatics.ReportError instead." ) ]
		public static void EmailAndLogError( Exception e ) {
			TelemetryStatics.ReportError( e );
		}

		[ Obsolete( "Guaranteed through 31 July 2015. Please use TelemetryStatics.ReportError instead." ) ]
		public static void EmailAndLogError( string prefix, Exception exception ) {
			TelemetryStatics.ReportError( prefix, exception );
		}

		[ Obsolete( "Guaranteed through 31 July 2015. Please use EmailStatics.SendEmailWithDefaultFromAddress instead." ) ]
		public static void SendEmailWithDefaultFromAddress( EmailMessage m ) {
			EmailStatics.SendEmailWithDefaultFromAddress( m );
		}

		[ Obsolete( "Guaranteed through 31 March 2015. Please use EmailStatics.SendEmail instead." ) ]
		public static void SendEmail( EmailMessage message ) {
			EmailStatics.SendEmail( message );
		}

		[ Obsolete( "Guaranteed through 31 July 2015. Please use EmailStatics.GetAdministratorEmailAddresses instead." ) ]
		public static List<EmailAddress> AdministratorEmailAddresses { get { return EmailStatics.GetAdministratorEmailAddresses().ToList(); } }
	}
}