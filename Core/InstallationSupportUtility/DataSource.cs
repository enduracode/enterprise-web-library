using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using JetBrains.Annotations;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

[ PublicAPI ]
public class DataSource {
	internal static string GetDownloadedPackagesFolderPath() => EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "Downloaded Data Packages" );

	internal InstallationType InstallationType { get; }

	private readonly RsisInstallation? systemManagerInstallation;

	private readonly string? azureTenantId;
	private readonly string? azureContainerUrl;
	private readonly string? azureBlobPrefix;

	public DataSource( RsisInstallation systemManagerInstallation ) {
		InstallationType = systemManagerInstallation.InstallationTypeElements is LiveInstallationElements ? InstallationType.Live : InstallationType.Intermediate;

		this.systemManagerInstallation = systemManagerInstallation;
	}

	public DataSource( ExistingInstallation installation, InstallationStandardConfigurationInstalledInstallation sourceAzureInstallation ) {
		InstallationType = sourceAzureInstallation.InstallationTypeConfiguration is LiveInstallationConfiguration
			                   ? InstallationType.Live
			                   : InstallationType.Intermediate;

		azureTenantId = sourceAzureInstallation.AzureHosting.TenantId;
		azureContainerUrl = AzureStatics.GetDataPackageContainerUrl(
			AzureStatics.DiscoverGeneralStorageAccountName(
				new DefaultAzureCredential( new DefaultAzureCredentialOptions { TenantId = sourceAzureInstallation.AzureHosting.TenantId } ) ),
			installation,
			InstallationType );
		azureBlobPrefix = AzureStatics.GetDataBlobPrefix( sourceAzureInstallation.shortName );
	}

	internal bool IsAzureInstallation => azureTenantId is not null;

	internal string BlobPrefix => azureBlobPrefix ?? throw new InvalidOperationException();

	/// <summary>
	/// Gets a data package, either by downloading one or using the last one that was downloaded. Returns the path to the package, which may be a ZIP file. Also
	/// archives downloaded data packages and deletes those that are too old to be useful. Installation Support Utility use only.
	/// </summary>
	internal string GetDataPackage( bool forceNewPackageDownload, OperationResult operationResult ) {
		var dataExportToRsisWebSiteNotPermitted = systemManagerInstallation.InstallationTypeElements is LiveInstallationElements
			{
				DataExportToRsisWebSiteNotPermitted: true
			};
		if( dataExportToRsisWebSiteNotPermitted
			    ? !File.Exists( DataStatics.GetPackageZipFilePath( systemManagerInstallation.FullName ) )
			    : !systemManagerInstallation.DataPackageSize.HasValue )
			return "";

		var downloadedPackagesFolder = EwlStatics.CombinePaths( GetDownloadedPackagesFolderPath(), systemManagerInstallation.FullName );

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

			// If the update data installation is a live installation for which data export to the RSIS website is not permitted, get the data package from disk.
			if( dataExportToRsisWebSiteNotPermitted )
				IoMethods.CopyFile( DataStatics.GetPackageZipFilePath( systemManagerInstallation.FullName ), packageZipFilePath );
			else
				operationResult.TimeSpentWaitingForNetwork = EwlStatics.ExecuteTimedRegion( () =>
					operationResult.NumberOfBytesTransferred = downloadDataPackage( systemManagerInstallation, packageZipFilePath ) );
		}

		deleteOldFiles( downloadedPackagesFolder, InstallationType == InstallationType.Live );
		return packageZipFilePath;
	}

	private long downloadDataPackage( RsisInstallation installation, string packageZipFilePath ) {
		using var fileWriteStream = IoMethods.GetFileStreamForWrite( packageZipFilePath );

		SystemManagerConnectionStatics.ExecuteActionWithSystemManagerClient(
			"data package download",
			client => Task.Run( async () => {
					using var response = await client.GetAsync(
						                     $"{SystemManagerConnectionStatics.InstallationsUrlSegment}/{installation.Id}/{SystemManagerConnectionStatics.DataPackageUrlSegment}",
						                     HttpCompletionOption.ResponseHeadersRead );
					response.EnsureSuccessStatusCode();
					await ( await response.Content.ReadAsStreamAsync() ).CopyToAsync( fileWriteStream );
				} )
				.Wait(),
			supportLargePayload: true );

		return fileWriteStream.Length;
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
			// We will delete everything except Saturday backups less than 45 days old and any backup less than 3 days old.
			if( !keepHistoricalArchive || creationTime < DateTime.Now.AddDays( -45 ) ||
			    ( creationTime < DateTime.Now.AddDays( -3 ) && creationTime.DayOfWeek != DayOfWeek.Saturday ) )
				IoMethods.DeleteFile( fileName );
		}
	}
}