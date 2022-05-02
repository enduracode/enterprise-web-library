using System;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using Humanizer;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage {
	public partial class RsisInstallation {
		public string FullName =>
			InstallationConfiguration.GetFullNameFromSystemAndInstallationNames(
				SystemManagerConnectionStatics.SystemList.GetSystemByInstallationId( Id ).Name,
				Name );

		public string FullShortName =>
			InstallationConfiguration.GetFullShortNameFromSystemAndInstallationNames(
				SystemManagerConnectionStatics.SystemList.GetSystemByInstallationId( Id ).ShortName,
				ShortName );

		/// <summary>
		/// Gets a data package, either by downloading one or using the last one that was downloaded. Returns the path to the ZIP file for the package. Also
		/// archives downloaded data packages and deletes those that are too old to be useful. Installation Support Utility use only.
		/// </summary>
		public string GetDataPackage( bool forceNewPackageDownload, OperationResult operationResult ) {
			var dataExportToRsisWebSiteNotPermitted = InstallationTypeElements is LiveInstallationElements liveInstallationElements &&
			                                          liveInstallationElements.DataExportToRsisWebSiteNotPermitted;
			if( dataExportToRsisWebSiteNotPermitted ? !File.Exists( IsuStatics.GetDataPackageZipFilePath( FullName ) ) : !DataPackageSize.HasValue )
				return "";

			var downloadedPackagesFolder = EwlStatics.CombinePaths( SystemManagerConnectionStatics.DownloadedDataPackagesFolderPath, FullName );

			var packageZipFilePath = "";
			// See if we can re-use an existing package.
			if( !forceNewPackageDownload && Directory.Exists( downloadedPackagesFolder ) ) {
				var downloadedPackages = IoMethods.GetFilePathsInFolder( downloadedPackagesFolder );
				if( downloadedPackages.Any() )
					packageZipFilePath = downloadedPackages.First();
			}

			// Download a package from RSIS if the user forces this behavior or if there is no package available on disk.
			if( forceNewPackageDownload || packageZipFilePath.Length == 0 ) {
				packageZipFilePath = EwlStatics.CombinePaths( downloadedPackagesFolder, "{0}-Package.zip".FormatWith( DateTime.Now.ToString( "yyyy-MM-dd" ) ) );

				// If the update data installation is a live installation for which data export to the RSIS web site is not permitted, get the data package from disk.
				if( dataExportToRsisWebSiteNotPermitted )
					IoMethods.CopyFile( IsuStatics.GetDataPackageZipFilePath( FullName ), packageZipFilePath );
				else
					operationResult.TimeSpentWaitingForNetwork =
						EwlStatics.ExecuteTimedRegion( () => operationResult.NumberOfBytesTransferred = downloadDataPackage( packageZipFilePath ) );
			}

			deleteOldFiles( downloadedPackagesFolder, InstallationTypeElements is LiveInstallationElements );
			return packageZipFilePath;
		}

		private long downloadDataPackage( string packageZipFilePath ) {
			using( var fileWriteStream = IoMethods.GetFileStreamForWrite( packageZipFilePath ) ) {
				SystemManagerConnectionStatics.ExecuteIsuServiceMethod(
					channel => {
						using( var networkStream = channel.DownloadDataPackage( SystemManagerConnectionStatics.SystemManagerAccessToken, Id ) )
							networkStream.CopyTo( fileWriteStream );
					},
					"data package download" );
				return fileWriteStream.Length;
			}
		}

		/// <summary>
		/// Deletes all (but one - the last file is never deleted) *.zip files in the given folder that are old, but keeps increasingly sparse archive packages alive.
		/// </summary>
		private void deleteOldFiles( string folderPath, bool keepHistoricalArchive ) {
			if( !Directory.Exists( folderPath ) )
				return;
			var files = IoMethods.GetFilePathsInFolder( folderPath, "*.zip" );
			// Never delete the last (most recent) file. It makes it really inconvenient for developers if this happens.
			foreach( var fileName in files.Skip( 1 ) ) {
				var creationTime = File.GetCreationTime( fileName );
				// We will delete everything more than 2 months old, keep saturday backups between 1 week and 2 months old, and keep everything less than 4 days old.
				if( !keepHistoricalArchive || creationTime < DateTime.Now.AddMonths( -2 ) ||
				    ( creationTime < DateTime.Now.AddDays( -4 ) && creationTime.DayOfWeek != DayOfWeek.Saturday ) )
					IoMethods.DeleteFile( fileName );
			}
		}

		public override string ToString() {
			return FullName;
		}
	}
}