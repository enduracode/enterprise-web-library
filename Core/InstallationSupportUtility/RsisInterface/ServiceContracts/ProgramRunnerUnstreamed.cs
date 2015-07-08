using System.ServiceModel;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.ServiceContracts {
	[ ServiceContract ]
	public interface ProgramRunnerUnstreamed {
		[ OperationContract ]
		string GetSystemList( string authenticationKey );
	}
}