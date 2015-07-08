namespace RedStapler.StandardLibrary.DataAccess {
	/// <summary>
	/// System-specific data-access logic.
	/// </summary>
	public interface SystemDataAccessProvider {
		/// <summary>
		/// Retrieves the next value from the system's main sequence.
		/// </summary>
		int GetNextMainSequenceValue();
	}
}