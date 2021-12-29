using EnterpriseWebLibrary;

namespace @@BaseNamespace.Library.Configuration.Providers {
	internal class General: SystemGeneralProvider {
		protected override string IntermediateLogInPassword => "your-password";
		protected override string EmailDefaultFromName => "Organization Name";
		protected override string EmailDefaultFromAddress => "contact@example.com";
	}
}