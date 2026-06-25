using System.Data.Common;
using FluentMigrator.Runner;

namespace EnterpriseWebLibrary.DatabaseSpecification.Databases;

/// <summary>
/// Contains information about a file-system folder that acts as a database.
/// </summary>
public class FolderDatabaseInfo: DatabaseInfo {
	private readonly string databaseName;

	/// <summary>
	/// Gets the folder path.
	/// </summary>
	public string FolderPath { get; }

	/// <summary>
	/// Creates a new folder-database information object.
	/// </summary>
	public FolderDatabaseInfo( string databaseName, string folderPath ) {
		this.databaseName = databaseName;
		FolderPath = folderPath;
	}

	string DatabaseInfo.SecondaryDatabaseName => databaseName;

	string DatabaseInfo.GetDelimitedIdentifier( string databaseObject ) => throw new NotSupportedException();
	string DatabaseInfo.ParameterPrefix => throw new NotSupportedException();
	string DatabaseInfo.LastAutoIncrementValueExpression => throw new NotSupportedException();
	string DatabaseInfo.QueryCacheHint => throw new NotSupportedException();

	string DatabaseInfo.GetConnectionString( int timeout, string clientIdOverride ) => throw new NotSupportedException();
	DbConnection DatabaseInfo.CreateConnection( string connectionString ) => throw new NotSupportedException();
	DbCommand DatabaseInfo.CreateCommand() => throw new NotSupportedException();
	DbParameter DatabaseInfo.CreateParameter() => throw new NotSupportedException();
	string DatabaseInfo.GetDbTypeString( object databaseSpecificType ) => throw new NotSupportedException();

	void DatabaseInfo.SetParameterType( DbParameter parameter, string dbTypeString ) {
		throw new NotSupportedException();
	}

	void DatabaseInfo.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {}
}