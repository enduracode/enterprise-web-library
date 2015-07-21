using System;

namespace EnterpriseWebLibrary.DataAccess {
	/// <summary>
	/// Use in DataAccessMethods.ExecuteInTransaction to roll back the transaction instead of committing it.
	/// </summary>
	public class DoNotCommitException: ApplicationException {}
}