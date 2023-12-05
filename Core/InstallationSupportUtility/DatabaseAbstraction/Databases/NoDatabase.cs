using EnterpriseWebLibrary.DataAccess;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;

internal class NoDatabase: Database {
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

	IEnumerable<string> Database.GetTables() => Enumerable.Empty<string>();

	IEnumerable<string> Database.GetProcedures() {
		throw new NotSupportedException();
	}

	IEnumerable<ProcedureParameter> Database.GetProcedureParameters( string procedure ) {
		throw new NotSupportedException();
	}

	void Database.PerformMaintenance() {}
	void Database.ShrinkAfterPostUpdateDataCommands() {}

	void Database.ExecuteDbMethod( Action<DBConnection> method ) {
		throw new NotSupportedException();
	}
}