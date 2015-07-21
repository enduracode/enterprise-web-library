using System.ServiceModel;

namespace EnterpriseWebLibrary.InstallationSupportUtility.RsisInterface.ServiceContracts {
	[ ServiceContract ]
	public interface ProgramRunnerUnstreamed {
		[ OperationContract ]
		string GetSystemList( string authenticationKey );
	}
}