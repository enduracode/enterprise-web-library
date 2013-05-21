using System;
using System.Collections.Generic;
using System.IO;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DataAccess.CommandWriting;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.Commands;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases {
	public class MySql: Database {
		private const string binFolderPath = @"C:\Program Files\MySQL\MySQL Server 5.5\bin";

		private readonly MySqlInfo info;

		public MySql( MySqlInfo info ) {
			this.info = info;
		}

		string Database.SecondaryDatabaseName { get { return ( info as DatabaseInfo ).SecondaryDatabaseName; } }

		void Database.ExecuteSqlScriptInTransaction( string script ) {
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "START TRANSACTION;" );
				sw.Write( script );
				sw.WriteLine( "COMMIT;" );
				sw.WriteLine( "quit" );

				executeMethodWithDbExceptionHandling( delegate {
					try {
						StandardLibraryMethods.RunProgram( StandardLibraryMethods.CombinePaths( binFolderPath, "mysql" ),
						                                   getHostAndAuthenticationArguments() + " " + info.Database + " --disable-reconnect --batch --disable-auto-rehash",
						                                   sw.ToString(),
						                                   true );
					}
					catch( Exception e ) {
						throw DataAccessMethods.CreateDbConnectionException( info, "updating logic in", e );
					}
				} );
			}
		}

		int Database.GetLineMarker() {
			var value = 0;
			ExecuteDbMethod( delegate( DBConnection cn ) {
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText = "SELECT ParameterValue FROM global_ints WHERE ParameterName = 'LineMarker'";
				value = (int)cn.ExecuteScalarCommand( command );
			} );
			return value;
		}

		void Database.UpdateLineMarker( int value ) {
			ExecuteDbMethod( delegate( DBConnection cn ) {
				var command = new InlineUpdate( "global_ints" );
				command.AddColumnModification( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( value ) ) );
				command.AddCondition( new EqualityCondition( new InlineDbCommandColumnValue( "ParameterName", new DbParameterValue( "LineMarker" ) ) ) );
				command.Execute( cn );
			} );
		}

		void Database.ExportToFile( string filePath ) {
			executeMethodWithDbExceptionHandling( delegate {
				try {
					File.WriteAllText( filePath,
					                   StandardLibraryMethods.RunProgram( StandardLibraryMethods.CombinePaths( binFolderPath, "mysqldump" ),
					                                                      getHostAndAuthenticationArguments() + " --single-transaction " + info.Database,
					                                                      "",
					                                                      true ) );
				}
				catch( Exception e ) {
					throw DataAccessMethods.CreateDbConnectionException( info, "exporting (to file)", e );
				}
			} );
		}

		void Database.DeleteAndReCreateFromFile( string filePath, bool keepDbInStandbyMode ) {
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "DROP DATABASE IF EXISTS {0};".FormatWith( info.Database ) );
				sw.WriteLine( "CREATE DATABASE {0};".FormatWith( info.Database ) );
				sw.WriteLine( "use {0}".FormatWith( info.Database ) );
				sw.Write( File.ReadAllText( filePath ) );
				sw.WriteLine( "quit" );

				executeMethodWithDbExceptionHandling( delegate {
					try {
						StandardLibraryMethods.RunProgram( StandardLibraryMethods.CombinePaths( binFolderPath, "mysql" ),
						                                   getHostAndAuthenticationArguments() + " --disable-reconnect --batch --disable-auto-rehash",
						                                   sw.ToString(),
						                                   true );
					}
					catch( Exception e ) {
						throw DataAccessMethods.CreateDbConnectionException( info, "re-creating (from file)", e );
					}
				} );
			}
		}

		private string getHostAndAuthenticationArguments() {
			return "--host=localhost --user=root --password=password";
		}

		void Database.BackupTransactionLog( string folderPath ) {
			throw new NotSupportedException();
		}

		void Database.RestoreNewTransactionLogs( string folderPath ) {
			throw new NotSupportedException();
		}

		string Database.GetLogSummary( string folderPath ) {
			throw new NotSupportedException();
		}

		List<string> Database.GetTables() {
			var tables = new List<string>();
			ExecuteDbMethod( delegate( DBConnection cn ) {
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND TABLE_TYPE = 'BASE TABLE'".FormatWith( info.Database );
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

		void Database.PerformMaintenance() {}

		void Database.ShrinkAfterPostUpdateDataCommands() {}

		/*
		private void setupNewDatabase() {
			var script = @"
CREATE TABLE global_ints(
	ParameterName VARCHAR( 50 )
		PRIMARY KEY,
	ParameterValue INT
		NOT NULL
);

INSERT INTO global_ints VALUES( 'LineMarker', 0 );

CREATE TABLE main_sequence(
	MainSequenceId INT
		AUTO_INCREMENT
		PRIMARY KEY
);
";
		}
*/

		public void ExecuteDbMethod( DbMethod method ) {
			executeMethodWithDbExceptionHandling(
				() => DataAccessMethods.ExecuteDbMethod( new MySqlInfo( ( info as DatabaseInfo ).SecondaryDatabaseName, info.Database, false ), method ) );
		}

		private void executeMethodWithDbExceptionHandling( Action method ) {
			try {
				method();
			}
			catch( DbConnectionFailureException e ) {
				throw new UserCorrectableException( "Failed to connect to MySQL.", e );
			}
		}
	}
}