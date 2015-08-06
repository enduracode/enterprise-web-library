using EnterpriseWebLibrary;

namespace @@BaseNamespace.Library.Configuration.Providers {
	internal class General: SystemGeneralProvider {
		string SystemGeneralProvider.AsposeLicenseName { get { return ""; } }
		string SystemGeneralProvider.IntermediateLogInPassword { get { return "your-password"; } }
		string SystemGeneralProvider.FormsLogInEmail { get { return ""; } }
		string SystemGeneralProvider.FormsLogInPassword { get { return ""; } }
		string SystemGeneralProvider.EmailDefaultFromName { get { return "Organization Name"; } }
		string SystemGeneralProvider.EmailDefaultFromAddress { get { return "contact@example.com"; } }
	}
}