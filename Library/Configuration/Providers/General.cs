using System;
using System.Net.Mail;

namespace RedStapler.StandardLibrary.Configuration.Providers {
	internal class General: SystemGeneralProvider {
		string SystemGeneralProvider.AsposeLicenseName { get { return ""; } }
		string SystemGeneralProvider.IntermediateLogInPassword { get { return "password"; } }
		string SystemGeneralProvider.FormsLogInEmail { get { return ""; } }
		string SystemGeneralProvider.FormsLogInPassword { get { return ""; } }

		SmtpClient SystemGeneralProvider.CreateClientSideAppSmtpClient() {
			throw new ApplicationException( "not implemented" );
		}
	}
}