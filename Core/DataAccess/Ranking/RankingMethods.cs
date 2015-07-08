namespace RedStapler.StandardLibrary.DataAccess.Ranking {
	/// <summary>
	/// A collection of static methods related to ranking.
	/// </summary>
	public static class RankingMethods {
		/// <summary>
		/// Inserts a new rank and returns its ID.
		/// </summary>
		public static int InsertRank() {
			var rankingSetup = (RankingProvider)DataAccessStatics.SystemProvider;
			var id = rankingSetup.GetNextMainSequenceValue();
			rankingSetup.InsertRank( id, id );
			return id;
		}

		/// <summary>
		/// Swaps the values of the specified ranks.
		/// </summary>
		internal static void SwapRanks( int rank1Id, int rank2Id ) {
			DataAccessState.Current.PrimaryDatabaseConnection.ExecuteInTransaction( delegate {
				var rankingSetup = (RankingProvider)DataAccessStatics.SystemProvider;

				var rank1Value = rankingSetup.GetRank( rank1Id );
				var rank2Value = rankingSetup.GetRank( rank2Id );

				rankingSetup.UpdateRank( rank1Id, rank2Value );
				rankingSetup.UpdateRank( rank2Id, rank1Value );
			} );
		}
	}
}