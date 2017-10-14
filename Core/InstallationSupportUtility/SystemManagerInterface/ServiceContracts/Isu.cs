using System.IO;
using System.ServiceModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages;

namespace EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.ServiceContracts {
	[ ServiceContract ]
	public interface Isu {
		[ OperationContract ]
		Stream DownloadServerConfiguration( string authenticationKey, string hostName );

		[ OperationContract ]
		void UploadBuild( BuildUploadMessage message );

		[ OperationContract ]
		Stream DownloadServerSideLogicPackage( string authenticationKey, int buildId );

		[ OperationContract ]
		Stream DownloadInstallationConfigurationPackage( string authenticationKey, int buildId, int installationId );

		[ OperationContract ]
		void UploadDataPackage( DataPackageUploadMessage dataPackage );

		[ OperationContract ]
		Stream DownloadDataPackage( string authenticationKey, int installationId );
	}
}