using EnterpriseWebLibrary.DataAccess;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

[ PublicAPI ]
public interface Database {
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
	IEnumerable<string> GetTables();
	IEnumerable<string> GetProcedures();
	IEnumerable<ProcedureParameter> GetProcedureParameters( string procedure );
	void PerformMaintenance();
	void ShrinkAfterPostUpdateDataCommands();

	/// <summary>
	/// Executes the given method inside a DBConnection for this Database.
	/// </summary>
	void ExecuteDbMethod( Action<DBConnection> method );
}