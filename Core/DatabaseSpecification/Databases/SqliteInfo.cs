using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using FluentMigrator.Runner;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases;

/// <summary>
/// Contains information about a SQLite database.
/// </summary>
public class SqliteInfo: DatabaseInfo {
	private static Lazy<ExternalSqliteProvider>? provider;

	internal static void Init( Func<ExternalSqliteProvider> providerGetter ) {
		provider = new Lazy<ExternalSqliteProvider>( providerGetter );
	}

	string DatabaseInfo.SecondaryDatabaseName => throw new NotImplementedException();

	string DatabaseInfo.GetDelimitedIdentifier( string databaseObject ) {
		throw new NotImplementedException();
	}

	string DatabaseInfo.ParameterPrefix => throw new NotImplementedException();
	string DatabaseInfo.LastAutoIncrementValueExpression => throw new NotImplementedException();
	string DatabaseInfo.QueryCacheHint => throw new NotImplementedException();

	string DatabaseInfo.GetConnectionString( int timeout ) {
		throw new NotImplementedException();
	}

	DbConnection DatabaseInfo.CreateConnection( string connectionString ) {
		throw new NotImplementedException();
	}

	DbCommand DatabaseInfo.CreateCommand() {
		throw new NotImplementedException();
	}

	DbParameter DatabaseInfo.CreateParameter() {
		throw new NotImplementedException();
	}

	string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) {
		throw new NotImplementedException();
	}

	void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
		throw new NotImplementedException();
	}

	void DatabaseInfo.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		provider!.Value.RegisterDependencyInjectionServicesForMigration( builder );
	}
}