namespace EnterpriseWebLibrary.DataAccess.Ranking {
	/// <summary>
	/// Defines how ranking operations will be carried out against the database for a particular system.
	/// </summary>
	public interface RankingProvider: SystemDataAccessProvider {
		/// <summary>
		/// Retrieves the rank matching the specified ID.
		/// </summary>
		int GetRank( int rankId );

		/// <summary>
		/// Inserts a new rank.
		/// </summary>
		void InsertRank( int rankId, int rank );

		/// <summary>
		/// Updates the rank matching the specified ID.
		/// </summary>
		void UpdateRank( int rankId, int rank );
	}
}