using System;
using System.Security.Principal;
using System.Web;
using RedStapler.StandardLibrary.Configuration;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// An HTTP Module that authenticates users based on client certificates.
	/// </summary>
	public class CertificateAuthenticationModule: IHttpModule {
		internal const string CertificateAuthenticationType = "Certificate";

		void IHttpModule.Init( HttpApplication app ) {
			app.AuthenticateRequest += app_AuthenticateRequest;
		}

		private static void app_AuthenticateRequest( object sender, EventArgs e ) {
			// Make the request's principal be the owner of the certificate.
			var certificateEmailAddress = getEmailAddressFromClientCertificate();
			if( certificateEmailAddress.Length > 0 )
				HttpContext.Current.User = new GenericPrincipal( new GenericIdentity( certificateEmailAddress, CertificateAuthenticationType ), new string[] { } );
		}

		/// <summary>
		/// Returns the email address from the client certificate. Returns the empty string if there is no certificate available and no override address has been
		/// specified. Also returns the empty string if there is an override address and the connection is not secure, since real certificates are only transmitted
		/// over secure connections.
		/// </summary>
		private static string getEmailAddressFromClientCertificate() {
			return ConfigurationStatics.CertificateEmailAddressOverride.Length > 0
				       ? ( HttpContext.Current.Request.IsSecureConnection ? ConfigurationStatics.CertificateEmailAddressOverride : "" )
				       : getValueFromClientCertificate( "SUBJECTEMAIL" );
		}

		/// <summary>
		/// Returns the email address from the client certificate. Returns the empty string if there is no certificate available and no override address has been specified.
		/// </summary>
		public static string GetEmailAddressFromClientCertificate() {
			return HttpContext.Current.User.Identity.IsAuthenticated ? HttpContext.Current.User.Identity.Name : "";
		}

		/// <summary>
		/// Returns the common name from the client certificate. Returns the empty string if there is no certificate available.
		/// </summary>
		public static string GetCommonNameFromClientCertificate() {
			return getValueFromClientCertificate( "SUBJECTCN" );
		}

		/// <summary>
		/// Returns the empty string if there is no certificate available.
		/// </summary>
		private static string getValueFromClientCertificate( string key ) {
			var cert = HttpContext.Current.Request.ClientCertificate;
			if( cert.IsPresent && cert.IsValid )
				return cert[ key ];
			return "";
		}

		void IHttpModule.Dispose() {}
	}
}