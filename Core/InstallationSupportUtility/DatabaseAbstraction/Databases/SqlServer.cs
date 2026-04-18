using System.Data;
using System.Threading;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using Serilog;
using Tewl.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;

public class SqlServer: Database {
	private readonly SqlServerInfo info;
	private readonly string dataLogicalFileName;
	private readonly string logLogicalFileName;

	public SqlServer( SqlServerInfo info, string dataLogicalFileName, string logLogicalFileName ) {
		this.info = info;
		this.dataLogicalFileName = dataLogicalFileName;
		this.logLogicalFileName = logLogicalFileName;
	}

	DatabaseInfo Database.Info => info;

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
			delegate( DatabaseConnection cn ) {
				var cmd = cn.DatabaseInfo.CreateCommand();
				cmd.CommandText = "SELECT ParameterValue FROM GlobalInts WHERE ParameterName = 'LineMarker'";
				value = (int)cn.ExecuteScalarCommand( cmd )!;
			} );
		return value;
	}

	void Database.UpdateLineMarker( int value ) {
		ExecuteDbMethod(
			delegate( DatabaseConnection cn ) {
				var command = new InlineUpdate( "GlobalInts" );
				command.AddColumnModifications( new InlineDbCommandColumnValue( "ParameterValue", new DbParameterValue( value ) ).ToCollection() );
				command.AddConditions(
					new EqualityCondition( new InlineDbCommandColumnValue( "ParameterName", new DbParameterValue( "LineMarker" ) ) ).ToCollection() );
				command.Execute( cn );
			} );
	}

	void Database.ExportToFile( ExportFile file ) {
		if( file.IsAzureBlob ) {
			file.TryGetAzureBlob( out var containerUrl, out var blobName );
			executeDbMethodAgainstMaster( cn => createManagedIdentityCredentialIfNecessary( cn, containerUrl ) );
			ExecuteDbMethod( cn => executeLongRunningCommand( cn, $"BACKUP DATABASE {info.Database} TO URL = '{containerUrl}/{blobName}' WITH COPY_ONLY, FORMAT" ) );
		}
		else {
			file.TryGetFilePath( out var filePath );
			try {
				ExecuteDbMethod( cn => executeLongRunningCommand(
					cn,
					"BACKUP DATABASE " + info.Database + " TO DISK = '" + getSqlServerFilePath( backupFilePath ) + "'" ) );
				IoMethods.CopyFile( backupFilePath, filePath );
			}
			finally {
				IoMethods.DeleteFile( backupFilePath );
			}
		}
	}

	void Database.DeleteAndReCreateFromFile(
		ExportFile file, IReadOnlyCollection<string> dataMigrationUsers, IReadOnlyCollection<string> dataModificationUsers ) {
		bool? fileExisted = null;
		executeDbMethodAgainstMaster( cn => fileExisted = deleteAndReCreateFromFile( cn, file ) );
		ExecuteDbMethod( cn => {
			if( !fileExisted!.Value ) {
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
			}

			if( file.IsAzureBlob ) {
				var oldUsers = new List<string>();
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText = "SELECT name FROM sys.database_principals WHERE type = 'E'";
				cn.ExecuteReaderCommand(
					command,
					reader => {
						while( reader.Read() ) {
							var user = reader.GetString( 0 );
							if( !dataMigrationUsers.Contains( user, StringComparer.Ordinal ) && !dataModificationUsers.Contains( user, StringComparer.Ordinal ) )
								oldUsers.Add( user );
						}
					} );
				foreach( var user in oldUsers )
					executeLongRunningCommand( cn, $"DROP USER [{user}]" );

				foreach( var user in dataMigrationUsers ) {
					executeLongRunningCommand(
						cn,
						$"IF NOT EXISTS ( SELECT * FROM sys.database_principals WHERE name = '{user}' ) CREATE USER [{user}] FROM EXTERNAL PROVIDER" );
					executeLongRunningCommand( cn, $"ALTER ROLE db_owner ADD MEMBER [{user}]" );
				}
				foreach( var user in dataModificationUsers ) {
					executeLongRunningCommand(
						cn,
						$"IF NOT EXISTS ( SELECT * FROM sys.database_principals WHERE name = '{user}' ) CREATE USER [{user}] FROM EXTERNAL PROVIDER" );
					executeLongRunningCommand( cn, $"ALTER ROLE db_datareader ADD MEMBER [{user}]" );
					executeLongRunningCommand( cn, $"ALTER ROLE db_datawriter ADD MEMBER [{user}]" );
				}
			}
			else if( !fileExisted.Value ) {
				const string userName = @"NT AUTHORITY\NETWORK SERVICE";
				executeLongRunningCommand( cn, "CREATE USER [{0}]".FormatWith( userName ) );
				executeLongRunningCommand( cn, "ALTER ROLE db_datareader ADD MEMBER [{0}]".FormatWith( userName ) );
				executeLongRunningCommand( cn, "ALTER ROLE db_datawriter ADD MEMBER [{0}]".FormatWith( userName ) );
			}
		} );
	}

	private bool deleteAndReCreateFromFile( DatabaseConnection cn, ExportFile file ) {
		// NOTE: Instead of catching exceptions, figure out if the database exists by querying.
		try {
			// Gets rid of existing connections. These don't need to be executed against the master database, but it's convenient because it saves us from needing
			// a second database connection.
			executeLongRunningCommand( cn, "ALTER DATABASE {0} SET AUTO_UPDATE_STATISTICS_ASYNC OFF".FormatWith( info.Database ) );
			if( !file.IsAzureBlob )
				executeLongRunningCommand( cn, "ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE".FormatWith( info.Database ) );

			executeLongRunningCommand( cn, "DROP DATABASE " + info.Database );
		}
		catch( Exception ) {
			// The database did not exist. That's fine.
		}

		if( file.IsAzureBlob ) {
			if( file.TryGetAzureBlob( out var containerUrl, out var blobName ) ) {
				createManagedIdentityCredentialIfNecessary( cn, containerUrl );
				try {
					executeLongRunningCommand( cn, $"RESTORE DATABASE [{info.Database}] FROM URL = '{containerUrl}/{blobName}'" );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "Failed to restore database from URL. Please try the operation again after obtaining a new backup.", e );
				}
				return true;
			}

			// Azure SQL MI manages file storage internally; CREATE DATABASE with file specifications is not supported.
			executeLongRunningCommand( cn, $"CREATE DATABASE [{info.Database}]" );
			return false;
		}

		var sqlServerFilesFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, "SQL Server Databases" );
		Directory.CreateDirectory( sqlServerFilesFolderPath );

		// Delete container folders from the existing database. The new database may not use them, and even if it does, SQL Server won’t overwrite existing folders.
		foreach( var containerFolderName in IoMethods.GetFolderNamesInFolder( sqlServerFilesFolderPath )
			        .Where( i => i.StartsWith( getContainerFolderPrefix(), StringComparison.Ordinal ) ) )
			IoMethods.DeleteFolder( EwlStatics.CombinePaths( sqlServerFilesFolderPath, containerFolderName ) );

		var dataFilePath = EwlStatics.CombinePaths( sqlServerFilesFolderPath, info.Database + ".mdf" );
		var logFilePath = EwlStatics.CombinePaths( sqlServerFilesFolderPath, info.Database + ".ldf" );
		if( file.TryGetFilePath( out var filePath ) ) {
			try {
				IoMethods.CopyFile( filePath, backupFilePath );
				var restoreLogic = getRestoreLogic( cn, sqlServerFilesFolderPath, dataFilePath, logFilePath );
				try {
					// WITH MOVE is required so that multiple instances of the same system’s database (RsisDev and RsisTesting, for example) can exist on the same machine
					// without their physical files colliding.
					executeLongRunningCommand(
						cn,
						"RESTORE DATABASE " + info.Database + " FROM DISK = '" + getSqlServerFilePath( backupFilePath ) + "' WITH " + StringTools.ConcatenateWithDelimiter(
							", ",
							restoreLogic.filePaths.Select( i => $"MOVE '{i.logicalName}' TO '{getSqlServerFilePath( i.path )}'" ) ) );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "Failed to create database from file. Please try the operation again after obtaining a new database file.", e );
				}

				foreach( var i in restoreLogic.fileRenameCommands )
					executeLongRunningCommand( cn, i );
			}
			finally {
				IoMethods.DeleteFile( backupFilePath );
			}
			return true;
		}

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
)".FormatWith( info.Database, dataLogicalFileName, getSqlServerFilePath( dataFilePath ), logLogicalFileName, getSqlServerFilePath( logFilePath ) ) );
		return false;
	}

	private void createManagedIdentityCredentialIfNecessary( DatabaseConnection connection, string containerUrl ) {
		executeLongRunningCommand(
			connection,
			$"IF NOT EXISTS ( SELECT * FROM sys.credentials WHERE name = '{containerUrl}' ) CREATE CREDENTIAL [{containerUrl}] WITH IDENTITY = 'MANAGED IDENTITY'" );
	}

	private ( IReadOnlyCollection<( string logicalName, string path )> filePaths, IReadOnlyCollection<string> fileRenameCommands ) getRestoreLogic(
		DatabaseConnection cn, string sqlServerFilesFolderPath, string dataFilePath, string logFilePath ) {
		var dataFile = "";
		var logFile = "";
		var containerFiles = new List<string>();
		var fileListCommand = cn.DatabaseInfo.CreateCommand();
		fileListCommand.CommandText = $"RESTORE FILELISTONLY FROM DISK = '{getSqlServerFilePath( backupFilePath )}'";
		cn.ExecuteReaderCommand(
			fileListCommand,
			reader => {
				while( reader.Read() ) {
					var type = reader.GetString( "Type" );
					var name = reader.GetString( "LogicalName" );
					if( type.Equals( "D", StringComparison.Ordinal ) ) {
						if( dataFile.Length > 0 )
							throw new UserCorrectableException( "The database contains multiple data files." );
						dataFile = name;
					}
					else if( type.Equals( "L", StringComparison.Ordinal ) ) {
						if( logFile.Length > 0 )
							throw new UserCorrectableException( "The database contains multiple log files." );
						logFile = name;
					}
					else {
						if( !type.Equals( "S", StringComparison.Ordinal ) )
							throw new UserCorrectableException( $"The database contains a file with an unsupported type: '{type}'." );
						containerFiles.Add( name );
					}
				}
			} );
		if( dataFile.Length == 0 )
			throw new UserCorrectableException( "The database does not contain a data file." );
		if( logFile.Length == 0 )
			throw new UserCorrectableException( "The database does not contain a log file." );

		var filePaths = new List<( string, string )>();
		filePaths.Add( ( dataFile, dataFilePath ) );
		filePaths.Add( ( logFile, logFilePath ) );
		filePaths.AddRange( containerFiles.Select( name => ( name, EwlStatics.CombinePaths( sqlServerFilesFolderPath, getContainerFolderPrefix() + name ) ) ) );

		var fileRenameCommands = new List<string>();
		if( !dataFile.Equals( dataLogicalFileName, StringComparison.Ordinal ) )
			fileRenameCommands.Add( getRename( dataFile, dataLogicalFileName ) );
		if( !logFile.Equals( logLogicalFileName, StringComparison.Ordinal ) )
			fileRenameCommands.Add( getRename( logFile, logLogicalFileName ) );

		return ( filePaths, fileRenameCommands );

		string getRename( string oldName, string newName ) => $"ALTER DATABASE {info.Database} MODIFY FILE ( NAME = N'{oldName}', NEWNAME = N'{newName}' )";
	}

	private string getContainerFolderPrefix() => info.Database + "_";

	// Use the EWL folder for all backup/restore operations because the SQL Server account probably already has access to it.
	private string backupFilePath => EwlStatics.CombinePaths( ConfigurationStatics.EwlFolderPath, info.Database + ".bak" );

	private string getSqlServerFilePath( string path ) => path.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );

	IEnumerable<DataRow> Database.GetDataTypes() => throw new NotSupportedException();

	string Database.GetDefaultSchema() => "dbo";

	IEnumerable<DatabaseTable> Database.GetTables() {
		var tables = new List<DatabaseTable>();
		ExecuteDbMethod(
			delegate( DatabaseConnection cn ) {
				var command = cn.DatabaseInfo.CreateCommand();
				command.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE Table_Type = 'Base Table'";
				cn.ExecuteReaderCommand(
					command,
					reader => {
						while( reader.Read() )
							tables.Add( new DatabaseTable( reader.GetString( 0 ), reader.GetString( 1 ) ) );
					} );
			} );
		return tables;
	}

	IEnumerable<string> Database.GetProcedures() => throw new NotSupportedException();

	IEnumerable<DataRow> Database.GetProcedureParameters( string procedure ) => throw new NotSupportedException();

	void Database.PerformMaintenance() {
		ExecuteDbMethod(
			delegate( DatabaseConnection cn ) {
				foreach( var i in DatabaseOps.GetDatabaseTables( this ) ) {
					executeLongRunningCommand( cn, "ALTER INDEX ALL ON " + i.tableName.QualifiedName + " REBUILD" );
					executeLongRunningCommand( cn, "UPDATE STATISTICS " + i.tableName.QualifiedName );
				}
			} );
	}

	void Database.ShrinkAfterPostUpdateDataCommands( bool databaseInAzure ) {
		// Give SQL Server a chance to clean up ghost records that may have been generated by post update data commands.
		// To determine how long this takes for a specified database, repeatedly refresh the Disk Usage by Table report while the ISU is running and watch the
		// Data(KB) column values drop for your LOB tables after the post update data commands have executed.
		Log.Information( "Waiting for ghost record cleanup." );
		Thread.Sleep( TimeSpan.FromMinutes( 5 ) );

		ExecuteDbMethod( cn => {
			executeLongRunningCommand( cn, "ALTER DATABASE {0} SET AUTO_UPDATE_STATISTICS_ASYNC OFF".FormatWith( info.Database ) );
			if( !databaseInAzure )
				executeLongRunningCommand( cn, "ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE".FormatWith( info.Database ) );

			ExceptionHandlingTools.Retry(
				() => {
					// This sometimes fails with "A severe error occurred on the current command."
					executeLongRunningCommand( cn, "DBCC SHRINKDATABASE( {0}, 10 )".FormatWith( info.Database ) );
				},
				"Failed to shrink database.",
				maxAttempts: 10,
				retryIntervalMs: 30000 );

			if( !databaseInAzure )
				executeLongRunningCommand( cn, "ALTER DATABASE {0} SET MULTI_USER".FormatWith( info.Database ) );
			executeLongRunningCommand( cn, "ALTER DATABASE {0} SET AUTO_UPDATE_STATISTICS_ASYNC ON".FormatWith( info.Database ) );
		} );
	}

	private void executeLongRunningCommand( DatabaseConnection cn, string commandText ) {
		var command = cn.DatabaseInfo.CreateCommand();
		command.CommandText = commandText;
		cn.ExecuteNonQueryCommand( command, isLongRunning: true );
	}

	public void ExecuteDbMethod( Action<DatabaseConnection> method ) {
		executeDbMethodWithSpecifiedDatabaseInfo( info, method );
	}

	private void executeDbMethodAgainstMaster( Action<DatabaseConnection> method ) {
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

	private void executeDbMethodWithSpecifiedDatabaseInfo( SqlServerInfo info, Action<DatabaseConnection> method ) {
		executeMethodWithDbExceptionHandling( () => {
			var connection = new DatabaseConnection(
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