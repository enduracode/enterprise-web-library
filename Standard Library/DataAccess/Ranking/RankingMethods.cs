namespace RedStapler.StandardLibrary.DataAccess.Ranking {
	/// <summary>
	/// A collection of static methods related to ranking.
	/// </summary>
	public static class RankingMethods {
		/// <summary>
		/// Inserts a new rank and returns its ID.
		/// </summary>
		public static int InsertRank( DBConnection cn ) {
			var rankingSetup = AppTools.SystemLogic as RankingSetup;
			var id = rankingSetup.GetNextMainSequenceValue( cn );
			rankingSetup.InsertRank( cn, id, id );
			return id;
		}

		/// <summary>
		/// Swaps the values of the specified ranks.
		/// </summary>
		internal static void SwapRanks( DBConnection cn, int rank1Id, int rank2Id ) {
			DataAccessMethods.ExecuteInTransaction( cn,
			                                        delegate {
			                                        	var rankingSetup = AppTools.SystemLogic as RankingSetup;

			                                        	var rank1Value = rankingSetup.GetRank( cn, rank1Id );
			                                        	var rank2Value = rankingSetup.GetRank( cn, rank2Id );

			                                        	rankingSetup.UpdateRank( cn, rank1Id, rank2Value );
			                                        	rankingSetup.UpdateRank( cn, rank2Id, rank1Value );
			                                        } );
		}
	}
}