using System.Data;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;

internal class NoDatabase: Database {
	DatabaseInfo? Database.Info => null;

	string Database.SecondaryDatabaseName => throw new NotSupportedException();

	void Database.ExecuteSqlScriptInTransaction( string script ) {
		throw new NotSupportedException();
	}

	int Database.GetLineMarker() {
		throw new NotSupportedException();
	}

	void Database.UpdateLineMarker( int value ) {
		throw new NotSupportedException();
	}

	void Database.ExportToFile( string filePath ) {}
	void Database.DeleteAndReCreateFromFile( string filePath ) {}

	IEnumerable<DataRow> Database.GetDataTypes() => throw new NotSupportedException();
	IEnumerable<string> Database.GetTables() => [ ];
	IEnumerable<string> Database.GetProcedures() => throw new NotSupportedException();
	IEnumerable<DataRow> Database.GetProcedureParameters( string procedure ) => throw new NotSupportedException();
	void Database.PerformMaintenance() {}
	void Database.ShrinkAfterPostUpdateDataCommands() {}

	void Database.ExecuteDbMethod( Action<DatabaseConnection> method ) {
		throw new NotSupportedException();
	}
}