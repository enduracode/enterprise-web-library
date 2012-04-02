namespace RedStapler.StandardLibrary.DataAccess {
	/// <summary>
	/// Defines how data access operations will be carried out for a particular system.
	/// </summary>
	public interface DataAccessSetup {
		/// <summary>
		/// Retrieves the next value from the system's main sequence.
		/// </summary>
		int GetNextMainSequenceValue( DBConnection cn );
	}
}