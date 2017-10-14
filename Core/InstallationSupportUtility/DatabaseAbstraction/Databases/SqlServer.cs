using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases {
	public class SqlServer: Database {
		private static readonly string sqlServerFilesFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, "SQL Server Databases" );

		private readonly SqlServerInfo info;
		private readonly string dataLogicalFileName;
		private readonly string logLogicalFileName;

		public SqlServer( SqlServerInfo info, string dataLogicalFileName, string logLogicalFileName ) {
			this.info = info;
			this.dataLogicalFileName = dataLogicalFileName;
			this.logLogicalFileName = logLogicalFileName;
		}

		string Database.SecondaryDatabaseName { get { return ( info as DatabaseInfo ).SecondaryDatabaseName; } }

		void Database.ExecuteSqlScriptInTransaction( string script ) {
			executeMethodWithDbExceptionHandling(
				delegate {
					try {
						EwlStatics.RunProgram(
							"sqlcmd",
							( info.Server != null ? "-S " + info.Server + " " : "" ) + "-d " + info.Database + " -e -b",
							"BEGIN TRAN" + Environment.NewLine + "GO" + Environment.NewLine + script + "COMMIT TRAN" + Environment.NewLine + "GO" + Environment.NewLine + "EXIT" +
							Environment.NewLine,
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
					command.AddColumnModification( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( value ) ) );
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
		}

		private void deleteAndReCreateFromFile( DBConnection cn, string filePath ) {
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
				IoMethods.CopyFile( filePath, backupFilePath );
				try {
					// WITH MOVE is required so that multiple instances of the same system's database (RsisDev and RsisTesting, for example) can exist on the same machine
					// without their physical files colliding.
					executeLongRunningCommand(
						cn,
						"RESTORE DATABASE " + info.Database + " FROM DISK = '" + backupFilePath + "'" + " WITH MOVE '" + dataLogicalFileName + "' TO '" +
						EwlStatics.CombinePaths( sqlServerFilesFolderPath, info.Database + ".mdf" ) + "', MOVE '" + logLogicalFileName + "' TO '" +
						EwlStatics.CombinePaths( sqlServerFilesFolderPath, info.Database + ".ldf" ) + "'" );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "Failed to create database from file. Please try the operation again after obtaining a new database file.", e );
				}
			}
			finally {
				IoMethods.DeleteFile( backupFilePath );
			}
		}

		// Use the Red Stapler folder for all backup/restore operations because the SQL Server account probably already has access to it.
		private string backupFilePath => EwlStatics.CombinePaths( ConfigurationStatics.RedStaplerFolderPath, info.Database + ".bak" );

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

			ExecuteDbMethod( cn => executeLongRunningCommand( cn, "DBCC SHRINKDATABASE( 0 )" ) );
		}

		private void executeLongRunningCommand( DBConnection cn, string commandText ) {
			var command = cn.DatabaseInfo.CreateCommand();
			command.CommandTimeout = 0; // This means the command can take as much time as it needs.
			command.CommandText = commandText;

			// NOTE: Not sure if this is the right execute method to use. NOTE: I think at this point we have to assume we are right to use this method.
			cn.ExecuteNonQueryCommand( command );
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