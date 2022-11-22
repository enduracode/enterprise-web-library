using System.Threading;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases {
	public class SqlServer: Database {
		private readonly SqlServerInfo info;
		private readonly string dataLogicalFileName;
		private readonly string logLogicalFileName;

		public SqlServer( SqlServerInfo info, string dataLogicalFileName, string logLogicalFileName ) {
			this.info = info;
			this.dataLogicalFileName = dataLogicalFileName;
			this.logLogicalFileName = logLogicalFileName;
		}

		string Database.SecondaryDatabaseName => ( info as DatabaseInfo ).SecondaryDatabaseName;

		void Database.ExecuteSqlScriptInTransaction( string script ) {
			executeMethodWithDbExceptionHandling(
				delegate {
					try {
						TewlContrib.ProcessTools.RunProgram(
							"sqlcmd",
							( info.Server != null ? "-S " + info.Server + " " : "" ) + "-d " + info.Database + " -e -b",
							"BEGIN TRAN" + Environment.NewLine + "GO" + Environment.NewLine + script + "COMMIT TRAN" + Environment.NewLine + "GO" + Environment.NewLine +
							"EXIT" + Environment.NewLine,
							true );
					}
					catch( Exception e ) {
						throw DataAccessMethods.CreateDbConnectionException( info, "updating logic in", e );
					}
				} );
		}

		int Database.GetLineMarker() {
			var value = 0;
			ExecuteDbMethod(
				delegate( DBConnection cn ) {
					var cmd = cn.DatabaseInfo.CreateCommand();
					cmd.CommandText = "SELECT ParameterValue FROM GlobalInts WHERE ParameterName = 'LineMarker'";
					value = (int)cn.ExecuteScalarCommand( cmd );
				} );
			return value;
		}

		void Database.UpdateLineMarker( int value ) {
			ExecuteDbMethod(
				delegate( DBConnection cn ) {
					var command = new InlineUpdate( "GlobalInts" );
					command.AddColumnModifications( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( value ) ).ToCollection() );
					command.AddCondition( new EqualityCondition( new InlineDbCommandColumnValue( "ParameterName", new DbParameterValue( "LineMarker" ) ) ) );
					command.Execute( cn );
				} );
		}

		void Database.ExportToFile( string filePath ) {
			try {
				ExecuteDbMethod( cn => executeLongRunningCommand( cn, "BACKUP DATABASE " + info.Database + " TO DISK = '" + backupFilePath + "'" ) );
				IoMethods.CopyFile( backupFilePath, filePath );
			}
			finally {
				IoMethods.DeleteFile( backupFilePath );
			}
		}

		void Database.DeleteAndReCreateFromFile( string filePath ) {
			executeDbMethodAgainstMaster( cn => deleteAndReCreateFromFile( cn, filePath ) );
			if( !filePath.Any() )
				ExecuteDbMethod(
					cn => {
						executeLongRunningCommand( cn, "ALTER DATABASE {0} SET PAGE_VERIFY CHECKSUM".FormatWith( info.Database ) );
						executeLongRunningCommand( cn, "ALTER DATABASE {0} SET AUTO_UPDATE_STATISTICS_ASYNC ON".FormatWith( info.Database ) );
						executeLongRunningCommand( cn, "ALTER DATABASE {0} SET ALLOW_SNAPSHOT_ISOLATION ON".FormatWith( info.Database ) );
						executeLongRunningCommand( cn, "ALTER DATABASE {0} SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE".FormatWith( info.Database ) );

						executeLongRunningCommand(
							cn,
							@"CREATE TABLE GlobalInts(
	ParameterName varchar( 50 )
		NOT NULL
		CONSTRAINT GlobalIntsPk PRIMARY KEY,
	ParameterValue int
		NOT NULL
)" );
						var lineMarkerInsert = new InlineInsert( "GlobalInts" );
						lineMarkerInsert.AddColumnModifications( new InlineDbCommandColumnValue( "ParameterName", new DbParameterValue( "LineMarker" ) ).ToCollection() );
						lineMarkerInsert.AddColumnModifications( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( 0 ) ).ToCollection() );
						lineMarkerInsert.Execute( cn );

						executeLongRunningCommand( cn, "CREATE SEQUENCE MainSequence AS int MINVALUE 1" );

						const string userName = @"NT AUTHORITY\NETWORK SERVICE";
						executeLongRunningCommand( cn, "CREATE USER [{0}]".FormatWith( userName ) );
						executeLongRunningCommand( cn, "ALTER ROLE db_datareader ADD MEMBER [{0}]".FormatWith( userName ) );
						executeLongRunningCommand( cn, "ALTER ROLE db_datawriter ADD MEMBER [{0}]".FormatWith( userName ) );
					} );
		}

		private void deleteAndReCreateFromFile( DBConnection cn, string filePath ) {
			// NOTE: Instead of catching exceptions, figure out if the database exists by querying.
			try {
				// Gets rid of existing connections. These don't need to be executed against the master database, but it's convenient because it saves us from needing
				// a second database connection.
				executeLongRunningCommand( cn, "ALTER DATABASE {0} SET AUTO_UPDATE_STATISTICS_ASYNC OFF".FormatWith( info.Database ) );
				executeLongRunningCommand( cn, "ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE".FormatWith( info.Database ) );

				executeLongRunningCommand( cn, "DROP DATABASE " + info.Database );
			}
			catch( Exception ) {
				// The database did not exist. That's fine.
			}

			var sqlServerFilesFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "SQL Server Databases" );
			Directory.CreateDirectory( sqlServerFilesFolderPath );

			var dataFilePath = EwlStatics.CombinePaths( sqlServerFilesFolderPath, info.Database + ".mdf" );
			var logFilePath = EwlStatics.CombinePaths( sqlServerFilesFolderPath, info.Database + ".ldf" );
			if( filePath.Any() )
				try {
					IoMethods.CopyFile( filePath, backupFilePath );
					try {
						// WITH MOVE is required so that multiple instances of the same system's database (RsisDev and RsisTesting, for example) can exist on the same machine
						// without their physical files colliding.
						executeLongRunningCommand(
							cn,
							"RESTORE DATABASE " + info.Database + " FROM DISK = '" + backupFilePath + "'" + " WITH MOVE '" + dataLogicalFileName + "' TO '" + dataFilePath +
							"', MOVE '" + logLogicalFileName + "' TO '" + logFilePath + "'" );
					}
					catch( Exception e ) {
						throw new UserCorrectableException( "Failed to create database from file. Please try the operation again after obtaining a new database file.", e );
					}
				}
				finally {
					IoMethods.DeleteFile( backupFilePath );
				}
			else
				executeLongRunningCommand(
					cn,
					@"CREATE DATABASE {0}
ON (
	NAME = {1},
	FILENAME = '{2}',
	SIZE = 100MB,
	FILEGROWTH = 15%
)
LOG ON (
	NAME = {3},
	FILENAME = '{4}',
	SIZE = 10MB,
	MAXSIZE = 1000MB,
	FILEGROWTH = 100MB
)".FormatWith( info.Database, dataLogicalFileName, dataFilePath, logLogicalFileName, logFilePath ) );
		}

		// Use the EWL folder for all backup/restore operations because the SQL Server account probably already has access to it.
		private string backupFilePath => EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, info.Database + ".bak" );

		List<string> Database.GetTables() {
			var tables = new List<string>();
			ExecuteDbMethod(
				delegate( DBConnection cn ) {
					var command = cn.DatabaseInfo.CreateCommand();
					command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE Table_Type = 'Base Table'";
					cn.ExecuteReaderCommand(
						command,
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
			ExecuteDbMethod(
				delegate( DBConnection cn ) {
					foreach( var tableName in DatabaseOps.GetDatabaseTables( this ) ) {
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

			ExecuteDbMethod(
				cn => {
					executeLongRunningCommand( cn, "ALTER DATABASE {0} SET AUTO_UPDATE_STATISTICS_ASYNC OFF".FormatWith( info.Database ) );
					executeLongRunningCommand( cn, "ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE".FormatWith( info.Database ) );
					ActionTools.Retry(
						() => {
							// This sometimes fails with "A severe error occurred on the current command."
							executeLongRunningCommand( cn, "DBCC SHRINKDATABASE( {0}, 10 )".FormatWith( info.Database ) );
						},
						"Failed to shrink database.",
						maxAttempts: 10,
						retryIntervalMs: 30000 );
					executeLongRunningCommand( cn, "ALTER DATABASE {0} SET MULTI_USER".FormatWith( info.Database ) );
					executeLongRunningCommand( cn, "ALTER DATABASE {0} SET AUTO_UPDATE_STATISTICS_ASYNC ON".FormatWith( info.Database ) );
				} );
		}

		private void executeLongRunningCommand( DBConnection cn, string commandText ) {
			var command = cn.DatabaseInfo.CreateCommand();
			command.CommandText = commandText;
			cn.ExecuteNonQueryCommand( command, isLongRunning: true );
		}

		public void ExecuteDbMethod( Action<DBConnection> method ) {
			executeDbMethodWithSpecifiedDatabaseInfo( info, method );
		}

		private void executeDbMethodAgainstMaster( Action<DBConnection> method ) {
			executeDbMethodWithSpecifiedDatabaseInfo(
				new SqlServerInfo(
					( info as DatabaseInfo ).SecondaryDatabaseName,
					info.Server,
					info.LoginName,
					info.Password,
					"master",
					info.SupportsConnectionPooling,
					info.FullTextCatalog ),
				method );
		}

		private void executeDbMethodWithSpecifiedDatabaseInfo( SqlServerInfo info, Action<DBConnection> method ) {
			executeMethodWithDbExceptionHandling(
				() => {
					var connection = new DBConnection(
						new SqlServerInfo(
							( info as DatabaseInfo ).SecondaryDatabaseName,
							info.Server,
							info.LoginName,
							info.Password,
							info.Database,
							false,
							info.FullTextCatalog ) );
					connection.ExecuteWithConnectionOpen( () => method( connection ) );
				} );
		}

		private void executeMethodWithDbExceptionHandling( Action method ) {
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