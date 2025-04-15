using System.Data;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

[ PublicAPI ]
public interface Database {
	/// <summary>
	/// Gets the database information object for this database.
	/// </summary>
	DatabaseInfo? Info { get; }

	/// <summary>
	/// Returns the empty string if this is the primary database.
	/// </summary>
	string SecondaryDatabaseName { get; }


	// Script execution

	/// <summary>
	/// The specified script is expected to either be the empty string or end with the line terminator string.
	/// </summary>
	void ExecuteSqlScriptInTransaction( string script );


	// Line marker retrieval and modification
	int GetLineMarker();
	void UpdateLineMarker( int value );

	// Data package support
	void ExportToFile( string filePath );
	void DeleteAndReCreateFromFile( string filePath );

	// Other
	IEnumerable<DataRow> GetDataTypes();
	IEnumerable<string> GetTables();
	IEnumerable<string> GetProcedures();
	IEnumerable<ProcedureParameter> GetProcedureParameters( string procedure );
	void PerformMaintenance();
	void ShrinkAfterPostUpdateDataCommands();

	/// <summary>
	/// Executes the given method inside a connection for this database.
	/// </summary>
	void ExecuteDbMethod( Action<DatabaseConnection> method );
}