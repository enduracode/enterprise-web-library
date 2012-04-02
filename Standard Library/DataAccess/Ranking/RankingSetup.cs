namespace RedStapler.StandardLibrary.DataAccess.Ranking {
	/// <summary>
	/// Defines how ranking operations will be carried out against the database for a particular system.
	/// The schema used for ranking should look like this:
	/// CREATE TABLE order_ranks(
	/// order_rank_id NUMBER CONSTRAINT order_ranks_pk PRIMARY KEY,
	/// rank NUMBER CONSTRAINT order_ranks_r_nn NOT NULL /*This would be unique but it can't be because the swap action would creating a key violation*/
	/// );
	/// </summary>
	public interface RankingSetup: DataAccessSetup {
		// NOTE: This should be a provider.

		/// <summary>
		/// Retrieves the rank matching the specified ID.
		/// </summary>
		int GetRank( DBConnection cn, int rankId );

		/// <summary>
		/// Inserts a new rank.
		/// </summary>
		void InsertRank( DBConnection cn, int rankId, int rank );

		/// <summary>
		/// Updates the rank matching the specified ID.
		/// </summary>
		void UpdateRank( DBConnection cn, int rankId, int rank );
	}
}