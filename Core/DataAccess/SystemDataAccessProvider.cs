namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// System-specific data-access logic.
/// </summary>
public interface SystemDataAccessProvider {
	/// <summary>
	/// EWL use only.
	/// </summary>
	void InitRetrievalCaches();

	/// <summary>
	/// Retrieves the next value from the system’s main sequence.
	/// </summary>
	int GetNextMainSequenceValue();
}