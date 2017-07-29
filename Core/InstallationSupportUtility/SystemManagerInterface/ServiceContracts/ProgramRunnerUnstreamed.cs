using System.ServiceModel;

namespace EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.ServiceContracts {
	[ ServiceContract ]
	public interface ProgramRunnerUnstreamed {
		[ OperationContract ]
		string GetSystemList( string authenticationKey );
	}
}