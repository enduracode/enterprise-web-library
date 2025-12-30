using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.Sqlite.Serilog;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Serilog;
using Serilog.Events;
using Tewl.IO;

namespace EnterpriseWebLibrary.Sqlite;

public class SqliteProvider: ExternalSqliteProvider {
	void ExternalSqliteProvider.InitStatics( string debugLogTimeFormat ) {
		Sink.Init( debugLogTimeFormat );
	}

	string ExternalSqliteProvider.GetConnectionString( string filePath, bool useReadOnlyMode, int timeout ) {
		var builder = new SqliteConnectionStringBuilder();

		builder.DataSource = filePath;
		builder.Mode = useReadOnlyMode ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite;
		builder.DefaultTimeout = timeout;
		builder.Pooling = false;

		return builder.ConnectionString;
	}

	DbConnection ExternalSqliteProvider.CreateConnection( string connectionString ) => new SqliteConnection( connectionString );

	DbCommand ExternalSqliteProvider.CreateCommand() => new SqliteCommand();

	DbParameter ExternalSqliteProvider.CreateParameter() => new SqliteParameter();

	void ExternalSqliteProvider.DeleteDatabaseAndReCreateFile( string filePath ) {
		IoMethods.DeleteFile( filePath );

		var builder = new SqliteConnectionStringBuilder { DataSource = filePath };
		var connection = new SqliteConnection( builder.ConnectionString );
		connection.Open();
		try {
			var command = new SqliteCommand( "VACUUM" );
			command.Connection = connection;
			command.ExecuteNonQuery();
		}
		finally {
			connection.Close();
		}
	}

	LoggerConfiguration ExternalSqliteProvider.AddDatabaseAsLogSink( LoggerConfiguration loggerConfiguration, string filePath ) =>
		loggerConfiguration.WriteTo.Sink( new Sink( filePath ), LevelAlias.Minimum );

	void ExternalSqliteProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddSQLite();
	}
}