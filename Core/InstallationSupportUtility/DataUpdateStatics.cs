using System;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public class DataUpdateStatics {
		public static Action DownloadDataPackageAndGetDataUpdateMethod(
			ExistingInstallation installation, bool installationIsStandbyDb, RsisInstallation source, bool forceNewPackageDownload,
			OperationResult operationResult ) {
			var packageZipFilePath = source.GetDataPackage( forceNewPackageDownload, operationResult );
			return () => {
				var recognizedInstallation = installation as RecognizedInstallation;
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
							foreach( var secondaryDatabase in recognizedInstallation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages ) {
								DatabaseOps.DeleteAndReCreateDatabaseFromFile(
									secondaryDatabase,
									databaseHasMinimumDataRevision(
										installation.ExistingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseSystemConfiguration(
											secondaryDatabase.SecondaryDatabaseName ) ),
									packageFolderPath );
							}
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

				// If we are getting data from a live installation, we're not a dev installation (which shouldn't happen anyway, but...), and we're
				// an intermediate installation, sanitize the data and do other conversion commands.
				if( source.InstallationTypeElements is LiveInstallationElements && installation is RecognizedInstalledInstallation recognizedInstalledInstallation &&
				    recognizedInstalledInstallation.KnownInstallationLogic.RsisInstallation.InstallationTypeElements is IntermediateInstallationElements ) {
					StatusStatics.SetStatus( "Executing live -> intermediate conversion commands..." );
					doDatabaseLiveToIntermediateConversionIfCommandsExist(
						installation.ExistingInstallationLogic.Database,
						installation.ExistingInstallationLogic.RuntimeConfiguration.PrimaryDatabaseSystemConfiguration );
					foreach( var secondaryDatabase in recognizedInstallation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages ) {
						doDatabaseLiveToIntermediateConversionIfCommandsExist(
							secondaryDatabase,
							installation.ExistingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseSystemConfiguration( secondaryDatabase.SecondaryDatabaseName ) );
					}
				}
			};
		}

		private static bool databaseHasMinimumDataRevision( Configuration.SystemGeneral.Database database ) =>
			( database?.MinimumDataRevisionSpecified ?? false ) && database.MinimumDataRevision > 0;

		/// <summary>
		/// Recompile procedures in secondary Oracle databases in case there are inter-database dependencies that prevented the procedures from being valid when the
		/// database was created.
		/// </summary>
		private static void recompileProceduresInSecondaryOracleDatabases( RecognizedInstallation installation ) {
			foreach( var secondaryDatabase in installation.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages ) {
				if( secondaryDatabase is Oracle )
					secondaryDatabase.ExecuteDbMethod(
						cn => {
							foreach( var procedure in secondaryDatabase.GetProcedures() ) {
								var command = cn.DatabaseInfo.CreateCommand();
								command.CommandText = "ALTER PROCEDURE " + procedure + " COMPILE";
								cn.ExecuteNonQueryCommand( command );
							}
						} );
			}
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