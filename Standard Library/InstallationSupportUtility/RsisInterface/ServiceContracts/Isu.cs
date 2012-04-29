using System.IO;
using System.ServiceModel;
using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.ServiceContracts {
	[ ServiceContract ]
	public interface Isu {
		[ OperationContract ]
		Stream DownloadServerSideLogicPackage( string authenticationKey, int buildId );

		[ OperationContract ]
		Stream DownloadInstallationConfigurationPackage( string authenticationKey, int buildId, int installationId );

		[ OperationContract ]
		void UploadDataPackage( DataPackageUploadMessage dataPackage );

		[ OperationContract ]
		Stream DownloadDataPackage( string authenticationKey, int installationId );

		/// <summary>
		/// Returns a zip file containing all new transaction log backups for the given installation. Returns null if there are no new transaction log files available.
		/// You can pass null for lastTransactionLogDownloaded to get all available log files.
		/// </summary>
		[ OperationContract ]
		Stream GetNewTransactionLogs( string authenticationKey, int installationId, string lastTransactionLogDownloaded );

		[ OperationContract ]
		void UploadBuild( Messages.BuildUploadMessage message );
	}
}