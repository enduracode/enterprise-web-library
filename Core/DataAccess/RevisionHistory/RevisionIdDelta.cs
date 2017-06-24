using System;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision identifier and and the previous revision's identifier, if a previous revision exists.
	/// </summary>
	public class RevisionIdDelta<UserType> {
		private readonly RevisionDelta<int, UserType> revisionDelta;

		internal RevisionIdDelta( int newRevisionId, Tuple<int, UserTransaction, UserType> oldRevisionIdAndTransactionAndUser ) {
			revisionDelta = new RevisionDelta<int, UserType>( newRevisionId, oldRevisionIdAndTransactionAndUser );
		}

		/// <summary>
		/// Gets the revision identifier.
		/// </summary>
		public int New => revisionDelta.New;

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasOld => revisionDelta.HasOld;

		/// <summary>
		/// Gets the previous revision's identifier, if a previous revision exists.
		/// </summary>
		public int Old => revisionDelta.Old;

		/// <summary>
		/// Gets the previous revision's transaction, if a previous revision exists.
		/// </summary>
		public UserTransaction OldTransaction => revisionDelta.OldTransaction;

		/// <summary>
		/// Gets the previous revision's user, if a previous revision exists.
		/// </summary>
		public UserType OldUser => revisionDelta.OldUser;

		/// <summary>
		/// Returns a full revision-delta object that is created using the specified revision-data selector.
		/// </summary>
		public RevisionDelta<RevisionDataType, UserType> ToFullDelta<RevisionDataType>( Func<int, RevisionDataType> revisionDataSelector ) {
			return new RevisionDelta<RevisionDataType, UserType>(
				revisionDataSelector( New ),
				HasOld ? Tuple.Create( revisionDataSelector( Old ), OldTransaction, OldUser ) : null );
		}
	}
}