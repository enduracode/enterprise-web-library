namespace EnterpriseWebLibrary.Configuration.Providers {
	internal class General: SystemGeneralProvider {
		protected override string IntermediateLogInPassword => GlobalStatics.IntermediateLogInPassword;
		protected override string EmailDefaultFromName => GlobalStatics.EmailDefaultFromName;
		protected override string EmailDefaultFromAddress => GlobalStatics.EmailDefaultFromAddress;
	}
}