using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases {
	internal class NoDatabase: Database {
		string Database.SecondaryDatabaseName { get { throw new NotSupportedException(); } }

		void Database.ExecuteSqlScriptInTransaction( string script ) {
			throw new NotSupportedException();
		}

		int Database.GetLineMarker() {
			// We can't throw an exception here because the update data operation uses this method to determine if the database is available.
			// NOTE: Can't we, if we make DatabaseOps.WaitForDatabaseRecovery do an if( !NoDatabase) condition?
			return 0;
		}

		void Database.UpdateLineMarker( int value ) {
			throw new NotSupportedException();
		}

		void Database.ExportToFile( string filePath ) {}

		void Database.DeleteAndReCreateFromFile( string filePath, bool keepDbInStandbyMode ) {}

		void Database.BackupTransactionLog( string folderPath ) {
			throw new NotImplementedException();
		}

		void Database.RestoreNewTransactionLogs( string folderPath ) {
			throw new NotImplementedException();
		}

		public string GetLogSummary( string folderPath ) {
			throw new NotImplementedException();
		}

		List<string> Database.GetTables() {
			throw new NotSupportedException();
		}

		List<string> Database.GetProcedures() {
			throw new NotSupportedException();
		}

		List<ProcedureParameter> Database.GetProcedureParameters( string procedure ) {
			throw new NotSupportedException();
		}

		void Database.PerformMaintenance() {}

		void Database.ShrinkAfterPostUpdateDataCommands() {}

		public void ExecuteDbMethod( DbMethod method ) {
			throw new NotSupportedException();
		}
	}
}