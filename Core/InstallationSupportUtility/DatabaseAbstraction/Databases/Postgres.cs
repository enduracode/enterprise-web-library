using System.Data;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;

public class Postgres: Database {
	private readonly PostgresInfo info;

	public Postgres( PostgresInfo info ) {
		this.info = info;
	}

	DatabaseInfo Database.Info => info;

	string Database.SecondaryDatabaseName => ( (DatabaseInfo)info ).SecondaryDatabaseName;

	void Database.ExecuteSqlScriptInTransaction( string script ) => throw new NotSupportedException();
	int Database.GetLineMarker() => throw new NotSupportedException();
	void Database.UpdateLineMarker( int value ) => throw new NotSupportedException();

	void Database.ExportToFile( ExportFile file ) {
		if( file.IsAzureBlob )
			throw new NotSupportedException();
		file.TryGetFilePath( out var path );
		executeMethodWithDbExceptionHandling( () => {
			try {
				TewlContrib.ProcessTools.RunProgram(
					getPostgresProgram( "pg_dump" ),
					getConnectionArguments() + $"""
					                             --file="{path}" --format=custom
					                            """,
					null,
					null );
			}
			catch( Exception e ) {
				throw DataAccessMethods.CreateDbConnectionException( info, "exporting (to file)", e );
			}
		} );
	}

	void Database.DeleteAndReCreateFromFile(
		ExportFile file, IReadOnlyCollection<string> dataMigrationUsers, IReadOnlyCollection<string> dataModificationUsers ) {
		if( file.IsAzureBlob )
			throw new NotSupportedException();

		var ownerRole = PostgresInfo.GetOwnerRoleName( info.Database );
		executeDbMethodWithSpecifiedDatabaseInfo(
			new PostgresInfo( ( (DatabaseInfo)info ).SecondaryDatabaseName, info.Host, info.Port, "postgres", false ),
			connection => {
				var roles = new HashSet<string>( StringComparer.Ordinal );
				new InlineSelect( [ "rolname" ], "FROM pg_roles", false ).Execute(
					connection,
					reader => {
						while( reader.Read() )
							roles.Add( reader.GetString( 0 ) );
					} );

				foreach( var role in dataMigrationUsers.Concat( dataModificationUsers ).Select( getWindowsRoleName ) )
					if( roles.Add( role ) )
						executeCommand( connection, $"CREATE ROLE {getDelimitedIdentifier( role )} LOGIN" );

				if( roles.Add( ownerRole ) )
					executeCommand( connection, $"CREATE ROLE {getDelimitedIdentifier( ownerRole )} NOLOGIN" );
				foreach( var role in dataMigrationUsers.Select( getWindowsRoleName ) )
					executeCommand( connection, $"GRANT {getDelimitedIdentifier( ownerRole )} TO {getDelimitedIdentifier( role )}" );

				executeCommand( connection, $"DROP DATABASE IF EXISTS {getDelimitedIdentifier( info.Database )} WITH ( FORCE )" );
				executeCommand(
					connection,
					$"CREATE DATABASE {getDelimitedIdentifier( info.Database )} OWNER {getDelimitedIdentifier( ownerRole )} TEMPLATE template0 ENCODING 'UTF8' LC_COLLATE 'C' LC_CTYPE 'C'" );
			} );

		if( file.TryGetFilePath( out var filePath ) )
			executeMethodWithDbExceptionHandling( () => {
				try {
					TewlContrib.ProcessTools.RunProgram(
						getPostgresProgram( "pg_restore" ),
						getConnectionArguments() + $"""
						                             --exit-on-error --no-owner --no-privileges --role="{ownerRole}" "{filePath}"
						                            """,
						null,
						null );
				}
				catch( Exception e ) {
					throw DataAccessMethods.CreateDbConnectionException( info, "re-creating (from file)", e );
				}
			} );
		else
			ExecuteDbMethod( connection => {
				executeCommand( connection, $"SET ROLE {getDelimitedIdentifier( ownerRole )}" );
				executeCommand( connection, "CREATE SEQUENCE main_sequence AS integer" );
			} );

		ExecuteDbMethod( connection => {
			var schemas = new List<string>();
			new InlineSelect( [ "nspname" ], "FROM pg_namespace WHERE nspname != 'information_schema' AND NOT starts_with( nspname, 'pg_' )", false ).Execute(
				connection,
				reader => {
					while( reader.Read() )
						schemas.Add( reader.GetString( 0 ) );
				} );

			foreach( var role in dataModificationUsers.Select( getWindowsRoleName ) ) {
				executeCommand( connection, $"GRANT CONNECT ON DATABASE {getDelimitedIdentifier( info.Database )} TO {getDelimitedIdentifier( role )}" );
				foreach( var schema in schemas ) {
					executeCommand( connection, $"GRANT USAGE ON SCHEMA {getDelimitedIdentifier( schema )} TO {getDelimitedIdentifier( role )}" );
					executeCommand(
						connection,
						$"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA {getDelimitedIdentifier( schema )} TO {getDelimitedIdentifier( role )}" );
					executeCommand(
						connection,
						$"GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA {getDelimitedIdentifier( schema )} TO {getDelimitedIdentifier( role )}" );
				}
				executeCommand(
					connection,
					$"ALTER DEFAULT PRIVILEGES FOR ROLE {getDelimitedIdentifier( ownerRole )} GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {getDelimitedIdentifier( role )}" );
				executeCommand(
					connection,
					$"ALTER DEFAULT PRIVILEGES FOR ROLE {getDelimitedIdentifier( ownerRole )} GRANT USAGE, SELECT ON SEQUENCES TO {getDelimitedIdentifier( role )}" );
				executeCommand(
					connection,
					$"ALTER DEFAULT PRIVILEGES FOR ROLE {getDelimitedIdentifier( ownerRole )} GRANT USAGE ON SCHEMAS TO {getDelimitedIdentifier( role )}" );
			}
		} );
		return;

		static string getWindowsRoleName( string user ) => user[ ( user.LastIndexOf( '\\' ) + 1 ).. ];
	}

	private string getDelimitedIdentifier( string identifier ) => ( (DatabaseInfo)info ).GetDelimitedIdentifier( identifier );

	private string getPostgresProgram( string name ) =>
		OperatingSystem.IsWindows()
			? EwlStatics.CombinePaths( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles ), "PostgreSQL", "18", "bin", name )
			: name;

	private string getConnectionArguments() =>
		$"""
		 --host="{( info.Host.Length > 0 ? info.Host : "localhost" )}"{info.Port.ToString()!.PrependDelimiter( " --port=" )} --no-password --dbname="{info.Database}"
		 """;

	IEnumerable<DataRow> Database.GetDataTypes() {
		IReadOnlyCollection<DataRow> rows = [ ];
		ExecuteDbMethod( connection => rows = connection.GetSchema( "DataTypes" ).Rows.Cast<DataRow>().Materialize() );
		return rows;
	}

	string Database.GetDefaultSchema() => "public";

	IEnumerable<DatabaseTable> Database.GetTables() {
		var tables = new List<DatabaseTable>();
		ExecuteDbMethod( connection => {
			new InlineSelect(
					[ "n.nspname", "c.relname" ],
				"FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relkind IN ( 'r', 'p' ) AND n.nspname != 'information_schema' AND NOT starts_with( n.nspname, 'pg_' )",
				false ).Execute(
				connection,
				reader => {
					while( reader.Read() )
						tables.Add( new DatabaseTable( reader.GetString( 0 ), reader.GetString( 1 ) ) );
				} );
		} );
		return tables;
	}

	IReadOnlyCollection<InferredParameter> Database.InferStatementParameterTypes( DatabaseConnection connection, string statement ) =>
		info.InferStatementParameterTypes( connection, statement );

	IEnumerable<string> Database.GetProcedures() => throw new NotImplementedException();

	IEnumerable<DataRow> Database.GetProcedureParameters( string procedure ) => throw new NotImplementedException();

	void Database.PerformMaintenance() => ExecuteDbMethod( connection => executeCommand( connection, "VACUUM ( ANALYZE )", isLongRunning: true ) );

	void Database.ShrinkAfterPostUpdateDataCommands( bool databaseInAzure ) =>
		ExecuteDbMethod( connection => executeCommand( connection, "VACUUM ( FULL, ANALYZE )", isLongRunning: true ) );

	private void executeCommand( DatabaseConnection connection, string commandText, bool isLongRunning = false ) {
		using var command = connection.DatabaseInfo.CreateCommand();
		command.CommandText = commandText;
		connection.ExecuteNonQueryCommand( command, isLongRunning );
	}

	public void ExecuteDbMethod( Action<DatabaseConnection> method ) => executeDbMethodWithSpecifiedDatabaseInfo( info, method );

	private void executeDbMethodWithSpecifiedDatabaseInfo( PostgresInfo info, Action<DatabaseConnection> method ) {
		executeMethodWithDbExceptionHandling( () => {
			var connection = new DatabaseConnection( new PostgresInfo( ( (DatabaseInfo)info ).SecondaryDatabaseName, info.Host, info.Port, info.Database, false ) );
			connection.ExecuteWithConnectionOpen( () => method( connection ) );
		} );
	}

	private void executeMethodWithDbExceptionHandling( Action method ) {
		try {
			method();
		}
		catch( DbConnectionFailureException e ) {
			throw new UserCorrectableException( "Failed to connect to PostgreSQL.", e );
		}
	}
}