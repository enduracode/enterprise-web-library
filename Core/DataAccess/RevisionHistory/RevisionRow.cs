namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision of a data entity.
	/// </summary>
	public class RevisionRow {
		private readonly int revisionId;
		private readonly int latestRevisionId;
		private readonly int userTransactionId;

		/// <summary>
		/// Creates a revision row object.
		/// </summary>
		public RevisionRow( int revisionId, int latestRevisionId, int userTransactionId ) {
			this.revisionId = revisionId;
			this.latestRevisionId = latestRevisionId;
			this.userTransactionId = userTransactionId;
		}

		/// <summary>
		/// Gets the revision's ID.
		/// </summary>
		public int RevisionId { get { return revisionId; } }

		/// <summary>
		/// Gets the revision's latest revision ID.
		/// </summary>
		public int LatestRevisionId { get { return latestRevisionId; } }

		/// <summary>
		/// Gets the revision's user transaction ID.
		/// </summary>
		public int UserTransactionId { get { return userTransactionId; } }
	}
}