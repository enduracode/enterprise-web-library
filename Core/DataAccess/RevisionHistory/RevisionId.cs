namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// An identifier for a revision of a data entity.
	/// </summary>
	public class RevisionId {
		private readonly int id;
		private readonly int? conceptualEntityId;

		/// <summary>
		/// Creates a revision identifier object.
		/// </summary>
		/// <param name="id">The revision identifier.</param>
		/// <param name="conceptualEntityId">The conceptual-entity identifier, i.e. the latest-revision ID of the main entity. If this revision is itself a revision
		/// of the main entity, you can pass null to automatically use the latest-revision ID.</param>
		public RevisionId( int id, int? conceptualEntityId ) {
			this.id = id;
			this.conceptualEntityId = conceptualEntityId;
		}

		internal int Id { get { return id; } }
		internal int? ConceptualEntityId { get { return conceptualEntityId; } }
	}
}