using EnterpriseWebLibrary;

namespace @@BaseNamespace.Library.Configuration.Providers {
	internal class General: SystemGeneralProvider {
		string SystemGeneralProvider.IntermediateLogInPassword => "your-password";
		string SystemGeneralProvider.EmailDefaultFromName => "Organization Name";
		string SystemGeneralProvider.EmailDefaultFromAddress => "contact@example.com";
	}
}