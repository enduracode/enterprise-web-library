using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.Sqlite.Serilog;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Serilog;
using Tewl.IO;

namespace EnterpriseWebLibrary.Sqlite;

public class SqliteProvider: ExternalSqliteProvider {
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
		loggerConfiguration.WriteTo.SQLite( filePath );

	void ExternalSqliteProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddSQLite();
	}
}