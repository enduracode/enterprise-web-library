namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision of a data entity, including its transaction and user.
	/// </summary>
	public class Revision<UserType, EntityRevisionType> where UserType: class {
		private readonly RevisionRow revisionRow;
		private readonly UserTransaction transaction;
		private readonly UserType user;
		private readonly EntityRevisionType entityRevision;

		internal Revision( RevisionRow revisionRow, UserTransaction transaction, UserType user, EntityRevisionType entityRevision ) {
			this.revisionRow = revisionRow;
			this.transaction = transaction;
			this.user = user;
			this.entityRevision = entityRevision;
		}

		/// <summary>
		/// Gets the revision row.
		/// </summary>
		public RevisionRow RevisionRow { get { return revisionRow; } }

		/// <summary>
		/// Gets the transaction.
		/// </summary>
		public UserTransaction Transaction { get { return transaction; } }

		/// <summary>
		/// Gets the user.
		/// </summary>
		public UserType User { get { return user; } }

		/// <summary>
		/// Gets the entity-specific revision data. This can be null if the entity allows deletion but does not use a deleted flag.
		/// </summary>
		public EntityRevisionType EntityRevision { get { return entityRevision; } }
	}
}