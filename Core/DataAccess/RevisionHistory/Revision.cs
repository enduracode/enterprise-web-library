namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision of a conceptual data entity, including its transaction and user.
	/// </summary>
	public class Revision<RevisionDataType, UserType> where UserType: class {
		private readonly int entityId;
		private readonly RevisionDataType revisionData;
		private readonly UserTransaction transaction;
		private readonly UserType user;

		internal Revision( int entityId, RevisionDataType revisionData, UserTransaction transaction, UserType user ) {
			this.entityId = entityId;
			this.revisionData = revisionData;
			this.transaction = transaction;
			this.user = user;
		}

		/// <summary>
		/// Gets the revision's conceptual entity ID, i.e. the latest-revision ID of the main entity.
		/// </summary>
		public int EntityId { get { return entityId; } }

		/// <summary>
		/// Gets the entity-specific revision data. This can be null you are using that to represent no data for this particular revision.
		/// </summary>
		public RevisionDataType RevisionData { get { return revisionData; } }

		/// <summary>
		/// Gets the transaction.
		/// </summary>
		public UserTransaction Transaction { get { return transaction; } }

		/// <summary>
		/// Gets the user.
		/// </summary>
		public UserType User { get { return user; } }
	}
}