namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision and the previous revision, if one exists.
	/// </summary>
	public class RevisionDelta<RevisionDataType, UserType> where UserType: class {
		private readonly Revision<RevisionDataType, UserType> newRevision;
		private readonly Revision<RevisionDataType, UserType> oldRevision;

		internal RevisionDelta( Revision<RevisionDataType, UserType> newRevision, Revision<RevisionDataType, UserType> oldRevision ) {
			this.newRevision = newRevision;
			this.oldRevision = oldRevision;
		}

		/// <summary>
		/// Gets the revision.
		/// </summary>
		public Revision<RevisionDataType, UserType> New { get { return newRevision; } }

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasOld { get { return oldRevision != null; } }

		/// <summary>
		/// Gets the previous revision, if one exists.
		/// </summary>
		public Revision<RevisionDataType, UserType> Old { get { return oldRevision; } }
	}
}