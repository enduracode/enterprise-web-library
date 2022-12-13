using System.Net.Http;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using EnterpriseWebLibrary.IO;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public class DataUpdateStatics {
		public static Action DownloadDataPackageAndGetDataUpdateMethod(
			ExistingInstallation installation, bool installationIsStandbyDb, RsisInstallation source, bool forceNewPackageDownload,
			OperationResult operationResult ) {
			var recognizedInstallation = installation as RecognizedInstallation;

			string packageZipFilePath;
			if( recognizedInstallation != null )
				packageZipFilePath = getDataPackage( source, forceNewPackageDownload, operationResult );
			else {
				var path = EwlStatics.CombinePaths(
					getDownloadedPackagesFolderPath(),
					installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + FileExtensions.Zip );
				packageZipFilePath = File.Exists( path ) ? path : "";
			}

			return () => {
				IoMethods.ExecuteWithTempFolder(
					tempFolderPath => {
						var packageFolderPath = EwlStatics.CombinePaths( tempFolderPath, "Package" );
						if( packageZipFilePath.Any() )
							ZipOps.UnZipFileAsFolder( packageZipFilePath, packageFolderPath );

						// Delete and re-create databases.
						DatabaseOps.DeleteAndReCreateDatabaseFromFile(
							installation.ExistingInstallationLogic.Database,
							databaseHasMinimumDataRevision( installation.ExistingInstallationLogic.RuntimeConfiguration.PrimaryDatabaseSystemConfiguration ),
							packageFolderPath );
						if( recognizedInstallation != null )
							foreach( var secondaryDatabase in recognizedInstallation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
								DatabaseOps.DeleteAndReCreateDatabaseFromFile(
									secondaryDatabase,
									databaseHasMinimumDataRevision(
										installation.ExistingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseSystemConfiguration(
											secondaryDatabase.SecondaryDatabaseName ) ),
									packageFolderPath );
					} );

				DatabaseOps.WaitForDatabaseRecovery( installation.ExistingInstallationLogic.Database );
				if( recognizedInstallation != null )
					recompileProceduresInSecondaryOracleDatabases( recognizedInstallation );

				if( !installationIsStandbyDb ) {
					// Bring database logic up to date with the rest of the logic in this installation. In other words, reapply changes lost when we deleted the database.
					StatusStatics.SetStatus( "Updating database logic..." );
					DatabaseOps.UpdateDatabaseLogicIfUpdateFileExists(
						installation.ExistingInstallationLogic.Database,
						installation.ExistingInstallationLogic.DatabaseUpdateFilePath,
						installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType == InstallationType.Development );
				}

				// If we're an intermediate installation and we are getting data from a live installation, sanitize the data and do other conversion commands.
				if( installation is RecognizedInstalledInstallation recognizedInstalledInstallation &&
				    recognizedInstalledInstallation.KnownInstallationLogic.RsisInstallation.InstallationTypeElements is IntermediateInstallationElements &&
				    source.InstallationTypeElements is LiveInstallationElements ) {
					StatusStatics.SetStatus( "Executing live -> intermediate conversion commands..." );
					doDatabaseLiveToIntermediateConversionIfCommandsExist(
						installation.ExistingInstallationLogic.Database,
						installation.ExistingInstallationLogic.RuntimeConfiguration.PrimaryDatabaseSystemConfiguration );
					foreach( var secondaryDatabase in recognizedInstallation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
						doDatabaseLiveToIntermediateConversionIfCommandsExist(
							secondaryDatabase,
							installation.ExistingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseSystemConfiguration( secondaryDatabase.SecondaryDatabaseName ) );
				}
			};
		}

		/// <summary>
		/// Gets a data package, either by downloading one or using the last one that was downloaded. Returns the path to the ZIP file for the package. Also
		/// archives downloaded data packages and deletes those that are too old to be useful. Installation Support Utility use only.
		/// </summary>
		private static string getDataPackage( RsisInstallation installation, bool forceNewPackageDownload, OperationResult operationResult ) {
			var dataExportToRsisWebSiteNotPermitted = installation.InstallationTypeElements is LiveInstallationElements liveInstallationElements &&
			                                          liveInstallationElements.DataExportToRsisWebSiteNotPermitted;
			if( dataExportToRsisWebSiteNotPermitted
				    ? !File.Exists( IsuStatics.GetDataPackageZipFilePath( installation.FullName ) )
				    : !installation.DataPackageSize.HasValue )
				return "";

			var downloadedPackagesFolder = EwlStatics.CombinePaths( getDownloadedPackagesFolderPath(), installation.FullName );

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
					IoMethods.CopyFile( IsuStatics.GetDataPackageZipFilePath( installation.FullName ), packageZipFilePath );
				else
					operationResult.TimeSpentWaitingForNetwork = EwlStatics.ExecuteTimedRegion(
						() => operationResult.NumberOfBytesTransferred = downloadDataPackage( installation, packageZipFilePath ) );
			}

			deleteOldFiles( downloadedPackagesFolder, installation.InstallationTypeElements is LiveInstallationElements );
			return packageZipFilePath;
		}

		private static string getDownloadedPackagesFolderPath() => EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "Downloaded Data Packages" );

		private static long downloadDataPackage( RsisInstallation installation, string packageZipFilePath ) {
			using var fileWriteStream = IoMethods.GetFileStreamForWrite( packageZipFilePath );

			SystemManagerConnectionStatics.ExecuteActionWithSystemManagerClient(
				"data package download",
				client => Task.Run(
						async () => {
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
		private static void deleteOldFiles( string folderPath, bool keepHistoricalArchive ) {
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

		private static bool databaseHasMinimumDataRevision( Configuration.SystemGeneral.Database database ) =>
			( database?.MinimumDataRevisionSpecified ?? false ) && database.MinimumDataRevision > 0;

		/// <summary>
		/// Recompile procedures in secondary Oracle databases in case there are inter-database dependencies that prevented the procedures from being valid when the
		/// database was created.
		/// </summary>
		private static void recompileProceduresInSecondaryOracleDatabases( RecognizedInstallation installation ) {
			foreach( var secondaryDatabase in installation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages )
				if( secondaryDatabase is DatabaseAbstraction.Databases.Oracle )
					secondaryDatabase.ExecuteDbMethod(
						cn => {
							foreach( var procedure in secondaryDatabase.GetProcedures() ) {
								var command = cn.DatabaseInfo.CreateCommand();
								command.CommandText = "ALTER PROCEDURE " + procedure + " COMPILE";
								cn.ExecuteNonQueryCommand( command );
							}
						} );
		}

		private static void doDatabaseLiveToIntermediateConversionIfCommandsExist( Database database, Configuration.SystemGeneral.Database configuration ) {
			if( !( configuration?.LiveToIntermediateConversionCommands ?? Enumerable.Empty<string>() ).Any() )
				return;

			database.ExecuteDbMethod(
				cn => {
					foreach( var commandText in configuration.LiveToIntermediateConversionCommands ) {
						var cmd = cn.DatabaseInfo.CreateCommand();
						cmd.CommandText = commandText;
						cn.ExecuteNonQueryCommand( cmd, isLongRunning: true );
					}
				} );
			database.ShrinkAfterPostUpdateDataCommands();
		}
	}
}