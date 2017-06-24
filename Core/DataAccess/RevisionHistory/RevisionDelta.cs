using System;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision and the previous revision, if one exists.
	/// </summary>
	public class RevisionDelta<RevisionDataType, UserType> {
		private readonly RevisionDataType newRevision;
		private readonly Tuple<RevisionDataType, UserTransaction, UserType> oldRevisionAndTransactionAndUser;

		internal RevisionDelta( RevisionDataType newRevision, Tuple<RevisionDataType, UserTransaction, UserType> oldRevisionAndTransactionAndUser ) {
			this.newRevision = newRevision;
			this.oldRevisionAndTransactionAndUser = oldRevisionAndTransactionAndUser;
		}

		/// <summary>
		/// Gets the revision.
		/// </summary>
		public RevisionDataType New => newRevision;

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasOld => oldRevisionAndTransactionAndUser != null;

		/// <summary>
		/// Gets the previous revision, if one exists.
		/// </summary>
		public RevisionDataType Old => oldRevisionAndTransactionAndUser.Item1;

		/// <summary>
		/// Gets the previous revision's transaction, if a previous revision exists.
		/// </summary>
		public UserTransaction OldTransaction => oldRevisionAndTransactionAndUser.Item2;

		/// <summary>
		/// Gets the previous revision's user, if a previous revision exists.
		/// </summary>
		public UserType OldUser => oldRevisionAndTransactionAndUser.Item3;
	}
}