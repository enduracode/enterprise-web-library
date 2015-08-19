namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision of a conceptual data entity.
	/// </summary>
	public class ConceptualRevision {
		/// <summary>
		/// Creates a conceptual revision object.
		/// </summary>
		/// <param name="entityId">The conceptual entity ID, i.e. the latest-revision ID of the main entity.</param>
		/// <param name="revisionData">The entity-specific revision data. If it's convenient, you may pass null if there is no data for this particular revision.
		/// </param>
		public static ConceptualRevision<RevisionDataType> Create<RevisionDataType>( int entityId, RevisionDataType revisionData ) {
			return new ConceptualRevision<RevisionDataType>( entityId, revisionData );
		}
	}

	/// <summary>
	/// A revision of a conceptual data entity.
	/// </summary>
	public class ConceptualRevision<RevisionDataType> {
		private readonly int entityId;
		private readonly RevisionDataType revisionData;

		internal ConceptualRevision( int entityId, RevisionDataType revisionData ) {
			this.entityId = entityId;
			this.revisionData = revisionData;
		}

		internal int EntityId { get { return entityId; } }
		internal RevisionDataType RevisionData { get { return revisionData; } }
	}
}