namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision and the previous revision, if one exists.
	/// </summary>
	public class RevisionDelta<UserType, EntityRevisionType> where UserType: class {
		private readonly Revision<UserType, EntityRevisionType> newRevision;
		private readonly Revision<UserType, EntityRevisionType> oldRevision;

		internal RevisionDelta( Revision<UserType, EntityRevisionType> newRevision, Revision<UserType, EntityRevisionType> oldRevision ) {
			this.newRevision = newRevision;
			this.oldRevision = oldRevision;
		}

		/// <summary>
		/// Gets the revision.
		/// </summary>
		public Revision<UserType, EntityRevisionType> New { get { return newRevision; } }

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasOld { get { return oldRevision != null; } }

		/// <summary>
		/// Gets the previous revision, if one exists.
		/// </summary>
		public Revision<UserType, EntityRevisionType> Old { get { return oldRevision; } }
	}
}