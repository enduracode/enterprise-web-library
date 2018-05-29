namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// An identifier for an event.
	/// </summary>
	public class EventId {
		private readonly int id;
		private readonly int userTransactionId;
		private readonly int conceptualEntityId;

		/// <summary>
		/// Creates an event identifier object.
		/// </summary>
		/// <param name="id">The event identifier.</param>
		/// <param name="userTransactionId">The transaction ID.</param>
		/// <param name="conceptualEntityId">The conceptual-entity identifier, i.e. the latest-revision ID of the main entity.</param>
		public EventId( int id, int userTransactionId, int conceptualEntityId ) {
			this.id = id;
			this.userTransactionId = userTransactionId;
			this.conceptualEntityId = conceptualEntityId;
		}

		internal int Id => id;
		internal int UserTransactionId => userTransactionId;
		internal int ConceptualEntityId => conceptualEntityId;
	}
}