namespace EnterpriseWebLibrary.Configuration.Providers {
	internal class General: SystemGeneralProvider {
		string SystemGeneralProvider.IntermediateLogInPassword => GlobalStatics.IntermediateLogInPassword;
		string SystemGeneralProvider.EmailDefaultFromName => GlobalStatics.EmailDefaultFromName;
		string SystemGeneralProvider.EmailDefaultFromAddress => GlobalStatics.EmailDefaultFromAddress;
	}
}