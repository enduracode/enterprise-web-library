using System.Data;
using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;

public class MySql: Database {
	private readonly MySqlInfo info;

	public MySql( MySqlInfo info ) {
		this.info = info;
	}

	DatabaseInfo Database.Info => info;

	string Database.SecondaryDatabaseName => ( info as DatabaseInfo ).SecondaryDatabaseName;

	void Database.ExecuteSqlScriptInTransaction( string script ) {
		using var sw = new StringWriter();
		sw.WriteLine( "START TRANSACTION;" );
		sw.Write( script );
		sw.WriteLine( "COMMIT;" );
		sw.WriteLine( "quit" );

		executeMethodWithDbExceptionHandling(
			delegate {
				try {
					TewlContrib.ProcessTools.RunProgram(
						getMySqlProgram( "mysql" ),
						getHostAndAuthenticationArguments() + " " + info.Database + " --disable-reconnect --batch --disable-auto-rehash",
						sw.ToString(),
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
			delegate( DatabaseConnection cn ) {
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText = "SELECT ParameterValue FROM global_ints WHERE ParameterName = 'LineMarker'";
				value = (int)cn.ExecuteScalarCommand( command )!;
			} );
		return value;
	}

	void Database.UpdateLineMarker( int value ) {
		ExecuteDbMethod(
			delegate( DatabaseConnection cn ) {
				var command = new InlineUpdate( "global_ints" );
				command.AddColumnModifications( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( value ) ).ToCollection() );
				command.AddConditions(
					new EqualityCondition( new InlineDbCommandColumnValue( "ParameterName", new DbParameterValue( "LineMarker" ) ) ).ToCollection() );
				command.Execute( cn );
			} );
	}

	void Database.ExportToFile( ExportFile file ) {
		executeMethodWithDbExceptionHandling(
			delegate {
				try {
					if( file.IsAzureBlob ) {
						file.TryGetAzureBlob( out var containerUrl, out var blobName );
						var blobClient = new BlockBlobClient( new Uri( $"{containerUrl}/{blobName}" ), new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned ) );
						using var stream = blobClient.OpenWrite( true );
						runMySqlProgramInAzure(
							"mysqldump",
							$"""
							 --single-transaction --hex-blob --set-gtid-purged=OFF "{info.Database}"
							 """,
							null,
							stream );
					}
					else {
						file.TryGetFilePath( out var filePath );

						// The --hex-blob option prevents certain BLOBs from causing errors during database re-creation.
						TewlContrib.ProcessTools.RunProgram(
							getMySqlProgram( "mysqldump" ),
							getHostAndAuthenticationArguments() + " --single-transaction --hex-blob --set-gtid-purged=OFF --result-file=\"{0}\" ".FormatWith( filePath ) +
							info.Database,
							"",
							true );
					}
				}
				catch( Exception e ) {
					throw DataAccessMethods.CreateDbConnectionException( info, "exporting (to file)", e );
				}
			} );
	}

	void Database.DeleteAndReCreateFromFile(
		ExportFile file, IReadOnlyCollection<string> dataMigrationUsers, IReadOnlyCollection<string> dataModificationUsers ) {
		using( var sw = new StringWriter() ) {
			sw.WriteLine( "DROP DATABASE IF EXISTS {0};".FormatWith( getDelimitedIdentifier( info.Database ) ) );
			sw.WriteLine( "CREATE DATABASE {0};".FormatWith( getDelimitedIdentifier( info.Database ) ) );
			if( !file.IsAzureBlob )
				sw.WriteLine( "quit" );

			executeMethodWithDbExceptionHandling( () => {
				const string arguments = "--disable-reconnect --batch --disable-auto-rehash";
				try {
					if( file.IsAzureBlob ) {
						var bomlessEncoding = new UTF8Encoding( false );
						using var stream = new MemoryStream( bomlessEncoding.GetBytes( sw.ToString() ) );
						runMySqlProgramInAzure( "mysql", arguments, stream, null );
					}
					else
						TewlContrib.ProcessTools.RunProgram( getMySqlProgram( "mysql" ), getHostAndAuthenticationArguments() + " " + arguments, sw.ToString(), true );
				}
				catch( Exception e ) {
					throw DataAccessMethods.CreateDbConnectionException( info, "re-creating (from file)", e );
				}
			} );
		}

		if( file.IsAzureBlob ) {
			if( file.TryGetAzureBlob( out var containerUrl, out var blobName ) ) {
				var blobClient = new BlobClient( new Uri( $"{containerUrl}/{blobName}" ), new ManagedIdentityCredential( ManagedIdentityId.SystemAssigned ) );
				using var stream = blobClient.OpenRead();
				executeMethodWithDbExceptionHandling( () => {
					try {
						runMySqlProgramInAzure(
							"mysql",
							$"""
							 --disable-reconnect --batch --disable-auto-rehash "{info.Database}"
							 """,
							stream,
							null );
					}
					catch( Exception e ) {
						if( e.Message.Contains( "ERROR" ) && e.Message.Contains( "at line" ) )
							throw new UserCorrectableException(
								"Failed to create database from file. Please try the operation again after obtaining a new database file.",
								e );
						throw DataAccessMethods.CreateDbConnectionException( info, "re-creating (from file)", e );
					}
				} );
			}
			else
				ExecuteDbMethod( initDatabase );

			ExecuteDbMethod( cn => {
				var existingUsers = new HashSet<string>( StringComparer.Ordinal );
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText = "SELECT User FROM mysql.user WHERE Host = '%' AND plugin = 'aad_auth'";
				cn.ExecuteReaderCommand(
					command,
					reader => {
						while( reader.Read() )
							existingUsers.Add( reader.GetString( 0 ) );
					} );

				foreach( var userAndPrivileges in dataMigrationUsers.Select( i => ( user: i, privileges: "ALL PRIVILEGES" ) )
					        .Concat( dataModificationUsers.Select( i => ( user: i, privileges: "SELECT, INSERT, UPDATE, DELETE, EXECUTE" ) ) ) ) {
					if( !existingUsers.Contains( MySqlInfo.GetValidUsername( userAndPrivileges.user ) ) )
						executeCommand(
							cn,
							$"CREATE AADUSER '{escapeString( userAndPrivileges.user )}' AS '{escapeString( MySqlInfo.GetValidUsername( userAndPrivileges.user ) )}'" );

					executeCommand(
						cn,
						$"GRANT {userAndPrivileges.privileges} ON {getDelimitedIdentifier( info.Database )}.* TO '{escapeString( MySqlInfo.GetValidUsername( userAndPrivileges.user ) )}'@'%'" );
					continue;

					static string escapeString( string value ) => value.Replace( "'", "''", StringComparison.Ordinal );
				}
			} );

			return;
		}

		if( file.TryGetFilePath( out var filePath ) ) {
			using var sw = new StringWriter();
			sw.WriteLine( "source {0}".FormatWith( filePath ) );
			sw.WriteLine( "quit" );

			executeMethodWithDbExceptionHandling( () => {
				try {
					TewlContrib.ProcessTools.RunProgram(
						getMySqlProgram( "mysql" ),
						$"""
						 {getHostAndAuthenticationArguments()} --disable-reconnect --batch --disable-auto-rehash "{info.Database}"
						 """,
						sw.ToString(),
						true );
				}
				catch( Exception e ) {
					if( e.Message.Contains( "ERROR" ) && e.Message.Contains( "at line" ) )
						throw new UserCorrectableException( "Failed to create database from file. Please try the operation again after obtaining a new database file.", e );
					throw DataAccessMethods.CreateDbConnectionException( info, "re-creating (from file)", e );
				}
			} );
		}
		else
			ExecuteDbMethod( initDatabase );
	}

	private void initDatabase( DatabaseConnection cn ) {
		var globalIntsCreate = cn.DatabaseInfo.CreateCommand();
		globalIntsCreate.CommandText = @"CREATE TABLE global_ints(
ParameterName VARCHAR( 50 )
	PRIMARY KEY,
ParameterValue INT
	NOT NULL
)";
		cn.ExecuteNonQueryCommand( globalIntsCreate );

		var lineMarkerInsert = new InlineInsert( "global_ints" );
		lineMarkerInsert.AddColumnModifications( new InlineDbCommandColumnValue( "ParameterName", new DbParameterValue( "LineMarker" ) ).ToCollection() );
		lineMarkerInsert.AddColumnModifications( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( 0 ) ).ToCollection() );
		lineMarkerInsert.Execute( cn );

		var mainSequenceCreate = cn.DatabaseInfo.CreateCommand();
		mainSequenceCreate.CommandText = @"CREATE TABLE main_sequence(
MainSequenceId INT
	AUTO_INCREMENT
	PRIMARY KEY
)";
		cn.ExecuteNonQueryCommand( mainSequenceCreate );
	}

	private string getDelimitedIdentifier( string databaseObject ) => ( info as DatabaseInfo ).GetDelimitedIdentifier( databaseObject );

	private void runMySqlProgramInAzure( string program, string arguments, Stream? input, Stream? output ) {
		TewlContrib.ProcessTools.RunProgram(
			getMySqlProgram( program ),
			$"""--host={info.Server} --user="{info.GetUser()}" --enable-cleartext-plugin --ssl-mode=VERIFY_IDENTITY""" + arguments.PrependDelimiter( " " ),
			input,
			output,
			new OrderedDictionary<string, string> { { "MYSQL_PWD", info.GetPassword() } } );
	}

	private string getMySqlProgram( string name ) {
		if( !OperatingSystem.IsWindows() )
			return name;

		const string mySqlFolderPath = @"C:\Program Files\MySQL";
		return EwlStatics.CombinePaths(
			mySqlFolderPath,
			IoMethods.GetFolderNamesInFolder( mySqlFolderPath ).Single( i => i.StartsWith( "MySQL Server ", StringComparison.Ordinal ) ),
			"bin",
			name );
	}

	private string getHostAndAuthenticationArguments() {
		return "--host=localhost --user=root --password=password";
	}

	IEnumerable<DataRow> Database.GetDataTypes() => throw new NotSupportedException();

	IEnumerable<DatabaseTable> Database.GetTables() {
		var tables = new List<DatabaseTable>();
		ExecuteDbMethod(
			delegate( DatabaseConnection cn ) {
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText =
					"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{0}' AND TABLE_TYPE = 'BASE TABLE'".FormatWith( info.Database );
				cn.ExecuteReaderCommand(
					command,
					reader => {
						while( reader.Read() )
							tables.Add( new DatabaseTable( "", reader.GetString( 0 ) ) );
					} );
			} );
		return tables;
	}

	IEnumerable<string> Database.GetProcedures() => throw new NotSupportedException();

	IEnumerable<DataRow> Database.GetProcedureParameters( string procedure ) => throw new NotSupportedException();

	void Database.PerformMaintenance() {}

	void Database.ShrinkAfterPostUpdateDataCommands( bool databaseInAzure ) {}

	private void executeCommand( DatabaseConnection connection, string commandText ) {
		var command = connection.DatabaseInfo.CreateCommand();
		command.CommandText = commandText;
		connection.ExecuteNonQueryCommand( command );
	}

	public void ExecuteDbMethod( Action<DatabaseConnection> method ) {
		executeMethodWithDbExceptionHandling( () => {
			var connection = new DatabaseConnection( new MySqlInfo( ( info as DatabaseInfo ).SecondaryDatabaseName, info.Server, info.Database, false ) );
			connection.ExecuteWithConnectionOpen( () => method( connection ) );
		} );
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