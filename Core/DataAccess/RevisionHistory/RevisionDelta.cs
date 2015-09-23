namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision and the previous revision, if one exists.
	/// </summary>
	public class RevisionDelta<RevisionDataType, UserType> {
		private readonly RevisionDataType newRevision;
		private readonly RevisionDataType oldRevision;
		private readonly UserTransaction oldTransaction;
		private readonly UserType oldUser;

		internal RevisionDelta( RevisionDataType newRevision, RevisionDataType oldRevision, UserTransaction oldTransaction, UserType oldUser ) {
			this.newRevision = newRevision;
			this.oldRevision = oldRevision;
			this.oldTransaction = oldTransaction;
			this.oldUser = oldUser;
		}

		/// <summary>
		/// Gets the revision.
		/// </summary>
		public RevisionDataType New { get { return newRevision; } }

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasOld { get { return !EwlStatics.AreEqual( oldRevision, default( RevisionDataType ) ); } }

		/// <summary>
		/// Gets the previous revision, if one exists.
		/// </summary>
		public RevisionDataType Old { get { return oldRevision; } }

		/// <summary>
		/// Gets the previous revision's transaction, if a previous revision exists.
		/// </summary>
		public UserTransaction OldTransaction { get { return oldTransaction; } }

		/// <summary>
		/// Gets the previous revision's user, if a previous revision exists.
		/// </summary>
		public UserType OldUser { get { return oldUser; } }
	}
}