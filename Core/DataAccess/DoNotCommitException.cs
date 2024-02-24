namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// Use in ExecuteInTransaction to rollback the transaction instead of committing it.
/// </summary>
public class DoNotCommitException: Exception;