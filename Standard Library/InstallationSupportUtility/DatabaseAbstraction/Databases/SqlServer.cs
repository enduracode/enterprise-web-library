using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DataAccess.CommandWriting;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.Commands;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases {
	internal class SqlServer: Database {
		private static readonly string sqlServerFilesFolderPath = StandardLibraryMethods.CombinePaths( AppTools.RedStaplerFolderPath, "SQL Server Databases" );

		private readonly SqlServerInfo info;
		private readonly string dataLogicalFileName;
		private readonly string logLogicalFileName;

		internal SqlServer( SqlServerInfo info, string dataLogicalFileName, string logLogicalFileName ) {
			this.info = info;
			this.dataLogicalFileName = dataLogicalFileName;
			this.logLogicalFileName = logLogicalFileName;
		}

		string Database.SecondaryDatabaseName { get { return ( info as DatabaseInfo ).SecondaryDatabaseName; } }

		void Database.ExecuteSqlScriptInTransaction( string script ) {
			executeMethodWithDbExceptionHandling( delegate {
				try {
					StandardLibraryMethods.RunProgram( "sqlcmd",
					                                   ( info.Server != null ? "-S " + info.Server : "" ) + "-d " + info.Database + " -e -b",
					                                   "BEGIN TRAN" + Environment.NewLine + "GO" + Environment.NewLine + script + "COMMIT TRAN" + Environment.NewLine + "GO" +
					                                   Environment.NewLine + "EXIT" + Environment.NewLine,
					                                   true );
				}
				catch( Exception e ) {
					throw DataAccessMethods.CreateDbConnectionException( info, "updating logic in", e );
				}
			} );
		}

		int Database.GetLineMarker() {
			var value = 0;
			ExecuteDbMethod( delegate( DBConnection cn ) {
				var cmd = cn.DatabaseInfo.CreateCommand();
				cmd.CommandText = "SELECT ParameterValue FROM GlobalInts WHERE ParameterName = 'LineMarker'";
				value = (int)cn.ExecuteScalarCommand( cmd );
			} );
			return value;
		}

		void Database.UpdateLineMarker( int value ) {
			ExecuteDbMethod( delegate( DBConnection cn ) {
				var command = new InlineUpdate( "GlobalInts" );
				command.AddColumnModification( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( value ) ) );
				command.AddCondition( new EqualityCondition( new InlineDbCommandColumnValue( "ParameterName", new DbParameterValue( "LineMarker" ) ) ) );
				command.Execute( cn );
			} );
		}

		void Database.ExportToFile( string filePath ) {
			ExecuteDbMethod( cn => executeLongRunningCommand( cn, "BACKUP DATABASE " + info.Database + " TO DISK = '" + filePath + "'" ) );
		}

		void Database.DeleteAndReCreateFromFile( string filePath, bool keepDbInStandbyMode ) {
			executeDbMethodAgainstMaster( cn => deleteAndReCreateFromFile( cn, filePath, keepDbInStandbyMode ) );
		}

		private void deleteAndReCreateFromFile( DBConnection cn, string filePath, bool keepDbInStandbyMode ) {
			// NOTE: Instead of catching exceptions, figure out if the database exists by querying.
			try {
				// Gets rid of existing connections. This doesn't need to be executed against the master database, but it's convenient because it saves us from needing
				// a second database connection.
				executeLongRunningCommand( cn, "ALTER DATABASE " + info.Database + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE" );

				executeLongRunningCommand( cn, "DROP DATABASE " + info.Database );
			}
			catch( Exception ) {
				// The database did not exist. That's fine.
			}

			Directory.CreateDirectory( sqlServerFilesFolderPath );

			try {
				// WITH MOVE is required so that multiple instances of the same system's database (RsisDev and RsisTesting, for example) can exist on the same machine
				// without their physical files colliding.
				var restoreCommand = "RESTORE DATABASE " + info.Database + " FROM DISK = '" + filePath + "'" + " WITH MOVE '" + dataLogicalFileName + "' TO '" +
				                     StandardLibraryMethods.CombinePaths( sqlServerFilesFolderPath, info.Database + ".mdf" ) + "', MOVE '" + logLogicalFileName + "' TO '" +
				                     StandardLibraryMethods.CombinePaths( sqlServerFilesFolderPath, info.Database + ".ldf" ) + "'";

				if( keepDbInStandbyMode )
					restoreCommand += ", STANDBY = '" + getStandbyFilePath() + "'";

				executeLongRunningCommand( cn, restoreCommand );
			}
			catch( Exception e ) {
				throw new UserCorrectableException( "Failed to create database from file. Please try the operation again after obtaining a new database file.", e );
			}
		}

		void Database.BackupTransactionLog( string folderPath ) {
			Directory.CreateDirectory( folderPath );
			ExecuteDbMethod( cn => {
				var newId = new InlineInsert( "MainSequence" ).Execute( cn );
				var newTransactionLogBackupFileName = newId + " " + StandardLibraryMethods.GetLocalHostName();


				// We definitely do not want the following statements in a transaction because we want to be sure that the insert is included in the backup. Also, if
				// the insert succeeds and the backup fails, that is totally fine.

				var insert = new InlineInsert( "RsisLogBackups" );
				insert.AddColumnModification( new InlineDbCommandColumnValue( "RsisLogBackupId", new DbParameterValue( newId, "Int" ) ) );
				insert.AddColumnModification( new InlineDbCommandColumnValue( "DateAndTimeSaved", new DbParameterValue( DateTime.Now, "DateTime2" ) ) );
				insert.AddColumnModification( new InlineDbCommandColumnValue( "TransactionLogFileName", new DbParameterValue( newTransactionLogBackupFileName, "VarChar" ) ) );
				insert.Execute( cn );

				var filePath = StandardLibraryMethods.CombinePaths( folderPath, newTransactionLogBackupFileName );
				executeLongRunningCommand( cn, "BACKUP LOG " + info.Database + " TO DISK = '" + filePath + "'" );
			} );
		}

		void Database.RestoreNewTransactionLogs( string folderPath ) {
			var lastRestoredTransactionLogFileName = "";
			ExecuteDbMethod( cn => {
				var command = new InlineSelect( "select TransactionLogFileName from RsisLogBackups", orderByClause: "order by RsisLogBackupId desc" );
				command.Execute( cn,
				                 r => {
				                 	if( r.Read() )
				                 		lastRestoredTransactionLogFileName = r.GetString( 0 );
				                 } );
			} );

			// We want all logs whose ID is greater than that of the last restored log file.
			var newLogFileNames = GetLogFilesOrderedByNewest( folderPath, lastRestoredTransactionLogFileName );

			// The following commands must be executed against the master database because there can't be any active connections when doing a restore (including the connection you are using
			// to run the command).
			executeDbMethodAgainstMaster( cn => {
				try {
					executeLongRunningCommand( cn, "ALTER DATABASE " + info.Database + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE" );

					foreach( var logFileName in newLogFileNames.Reverse() ) {
						var filePath = StandardLibraryMethods.CombinePaths( folderPath, logFileName );

						// We do not want this to be in a transaction (it probably wouldn't even work because having a transaction writes to the transaction log, and we can't write anything to a
						// SQL Server database in standby mode.
						try {
							executeLongRunningCommand( cn, "RESTORE LOG " + info.Database + " FROM DISK = '" + filePath + "' WITH STANDBY = '" + getStandbyFilePath() + "'" );
						}
						catch( Exception e ) {
							var sqlException = e.GetBaseException() as SqlException;
							if( sqlException != null ) {
								// 3117: The log or differential backup cannot be restored because no files are ready to rollforward.
								if( sqlException.Number == 3117 ) {
									throw new UserCorrectableException(
										"Failed to restore log, probably because we failed to do a full restore with force new package download after creating a log-shipping-enabled backup on the live server.",
										e );
								}

								// 4305: The log in this backup set begins at LSN X, which is too recent to apply to the database. An earlier log backup that includes LSN Y can be restored. 
								if( sqlException.Number == 4305 ) {
									throw new UserCorrectableException(
										"Failed to restore log because the oldest log available for download from the live server is still too new to restore on this standby server. This happens if the standby server falls so far behind that the live server starts cleaning up old logs before they are downloaded.",
										e );
								}
							}

							throw;
						}
					}
				}
				finally {
					// Sometimes the database isn't ready to go yet and this command will fail. So, we retry.
					StandardLibraryMethods.Retry( () => executeLongRunningCommand( cn, "ALTER DATABASE " + info.Database + " SET MULTI_USER" ),
					                              "Database is in Restoring state and is not recovering." );
				}
			} );
		}

		/// <summary>
		/// Returns the list of file names in the given folder, ordered with the newest log file first (according to ID).
		/// The predicate uses the database ID to determine if the file should be included in the results.
		/// </summary>
		public static IEnumerable<string> GetLogFilesOrderedByNewest( string folderPath, string includeOnlyFilesNewerThanThisLogFileName = "" ) {
			int? onlyGetFilesNewerThanThisId = null;
			if( includeOnlyFilesNewerThanThisLogFileName.Length > 0 )
				onlyGetFilesNewerThanThisId = getLogIdFromFileName( includeOnlyFilesNewerThanThisLogFileName );

			return from fileName in IoMethods.GetFileNamesInFolder( folderPath )
			       let logId = getLogIdFromFileName( fileName )
			       where onlyGetFilesNewerThanThisId == null || logId > onlyGetFilesNewerThanThisId
			       orderby logId descending
			       select fileName;
		}

		private static int getLogIdFromFileName( string logFileName ) {
			return int.Parse( logFileName.Separate().First() );
		}

		private string getStandbyFilePath() {
			return StandardLibraryMethods.CombinePaths( sqlServerFilesFolderPath, info.Database + "Standby.ldf" );
		}

		public string GetLogSummary( string folderPath ) {
			var summary = "";
			ExecuteDbMethod( cn => {
				var cutOffDateTime = DateTime.Now.AddHours( -24 );

				var command = new InlineSelect( "select count( * ) from RsisLogBackups" );
				command.AddCondition( new InequalityCondition( InequalityCondition.Operator.GreaterThan,
				                                               new InlineDbCommandColumnValue( "DateAndTimeSaved", new DbParameterValue( cutOffDateTime, "DateTime2" ) ) ) );
				var numberOfLogsRestored = 0;
				command.Execute( cn,
				                 r => {
				                 	r.Read();
				                 	numberOfLogsRestored = r.GetInt32( 0 );
				                 } );

				summary = "In the last 24 hours, " + numberOfLogsRestored + " logs were successfully restored.";
				if( Directory.Exists( folderPath ) ) {
					var logsDownloaded = new DirectoryInfo( folderPath ).GetFiles().Where( f => f.LastWriteTime > cutOffDateTime ).ToList();
					var totalSizeInBytes = logsDownloaded.Sum( f => f.Length );
					summary += " " + logsDownloaded.Count() + " logs were downloaded, with a total size of " + FormattingMethods.GetFormattedBytes( totalSizeInBytes ) + ".";
				}
			} );
			return summary;
		}

		public List<string> GetTables() {
			var tables = new List<string>();
			ExecuteDbMethod( delegate( DBConnection cn ) {
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE Table_Type = 'Base Table'";
				cn.ExecuteReaderCommand( command,
				                         reader => {
				                         	while( reader.Read() )
				                         		tables.Add( reader.GetString( 0 ) );
				                         } );
			} );
			return tables;
		}

		List<string> Database.GetProcedures() {
			throw new NotSupportedException();
		}

		List<ProcedureParameter> Database.GetProcedureParameters( string procedure ) {
			throw new NotSupportedException();
		}

		void Database.PerformMaintenance() {
			ExecuteDbMethod( delegate( DBConnection cn ) {
				foreach( var tableName in GetTables() ) {
					executeLongRunningCommand( cn, "ALTER INDEX ALL ON " + tableName + " REBUILD" );
					executeLongRunningCommand( cn, "UPDATE STATISTICS " + tableName );
				}
			} );
		}

		void Database.ShrinkAfterPostUpdateDataCommands() {
			// Give SQL Server a chance to clean up ghost records that may have been generated by post update data commands.
			// To determine how long this takes for a specified database, repeatedly refresh the Disk Usage by Table report while the ISU is running and watch the
			// Data(KB) column values drop for your LOB tables after the post update data commands have executed.
			StatusStatics.SetStatus( "Waiting for ghost record cleanup." );
			Thread.Sleep( TimeSpan.FromMinutes( 5 ) );

			ExecuteDbMethod( cn => executeLongRunningCommand( cn, "DBCC SHRINKDATABASE( 0 )" ) );
		}

		private static void executeLongRunningCommand( DBConnection cn, string commandText ) {
			var command = cn.DatabaseInfo.CreateCommand();
			command.CommandTimeout = 0; // This means the command can take as much time as it needs.
			command.CommandText = commandText;

			// NOTE: Not sure if this is the right execute method to use. NOTE: I think at this point we have to assume we are right to use this method.
			cn.ExecuteNonQueryCommand( command );
		}

		/*
		private static void setupNewDatabase() {
			string script = @"
		 * 
		 * USE Master

CREATE DATABASE [DatabaseName] ON (
NAME = Data,
    FILENAME = 'C:\...\SQL Server Data\[SystemName]Data.mdf',
    SIZE = 100MB,
    FILEGROWTH = 15% )
LOG ON
( NAME = Log,
    FILENAME = 'C:\...\SQL Server Data\[SystemName].ldf',
    SIZE = 10MB,
    MAXSIZE = 1000MB,
    FILEGROWTH = 100MB );
GO

USE [DatabaseName]

ALTER DATABASE [DatabaseName] SET PAGE_VERIFY CHECKSUM
ALTER DATABASE [DatabaseName] SET AUTO_UPDATE_STATISTICS_ASYNC ON
ALTER DATABASE [DatabaseName] SET ALLOW_SNAPSHOT_ISOLATION ON
ALTER DATABASE [DatabaseName] SET READ_COMMITTED_SNAPSHOT ON
GO

CREATE TABLE GlobalInts(
	ParameterName varchar( 50 )
		NOT NULL
		CONSTRAINT GlobalIntsPk PRIMARY KEY,
	ParameterValue int
		NOT NULL
)

INSERT INTO GlobalInts VALUES( 'LineMarker', 0 )
GO

CREATE TABLE MainSequence(
	MainSequenceId int NOT NULL IDENTITY CONSTRAINT MainSequencePk PRIMARY KEY
)
";
		}
		Make sure the data file and the log file as shrunken, then set the log autogrowth to 100MB (not 10%, or 1MB, or anything small). If you know it will
		 *be large, set it large initially. The fewer times it autogrows, the faster log restores will be.
		 *NOTE: Come up with guidelines for this. 100MB growth is too big for AFI. Also, some systems are setup with unrestricted growth. Others are set with some limit (2GB). We should
		 * decide what to do for this on all databases. What if someone just keeps importing huge book databases or uploading huge binaries to AFI?
		 *
		  CREATE TABLE RsisLogBackups(
		  RsisLogBackupId int NOT NULL CONSTRAINT pkRsisLogbackups PRIMARY KEY,
		  DateAndTimeSaved datetime2 NOT NULL,
		  TransactionLogFileName varchar( 50 ) NOT NULL CONSTRAINT uniqueTransactionLogFileName UNIQUE
		  )
		  GO
 */

		public void ExecuteDbMethod( DbMethod method ) {
			executeDbMethodWithSpecifiedDatabaseInfo( info, method );
		}

		private void executeDbMethodAgainstMaster( DbMethod method ) {
			executeDbMethodWithSpecifiedDatabaseInfo(
				new SqlServerInfo( ( info as DatabaseInfo ).SecondaryDatabaseName,
				                   info.Server,
				                   info.LoginName,
				                   info.Password,
				                   "master",
				                   info.SupportsConnectionPooling,
				                   info.FullTextCatalog ),
				method );
		}

		private static void executeDbMethodWithSpecifiedDatabaseInfo( SqlServerInfo info, DbMethod method ) {
			executeMethodWithDbExceptionHandling(
				() =>
				DataAccessMethods.ExecuteDbMethod(
					new SqlServerInfo( ( info as DatabaseInfo ).SecondaryDatabaseName, info.Server, info.LoginName, info.Password, info.Database, false, info.FullTextCatalog ),
					method ) );
		}

		private static void executeMethodWithDbExceptionHandling( Action method ) {
			try {
				method();
			}
			catch( DbConnectionFailureException e ) {
				throw new UserCorrectableException( "Failed to connect to SQL Server.", e );
			}
			catch( DbCommandTimeoutException e ) {
				throw new UserCorrectableException( "A SQL Server command timeout occurred.", e );
			}
		}
	}
}