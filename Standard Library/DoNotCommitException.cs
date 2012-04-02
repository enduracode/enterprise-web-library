using System;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Use in AppTools.ExecuteInTransaction to roll back the transaction instead of committing it.
	/// NOTE: Move to DataAccess subsystem.
	/// </summary>
	public class DoNotCommitException: ApplicationException {}
}