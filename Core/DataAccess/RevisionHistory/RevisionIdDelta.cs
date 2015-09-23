using System;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision identifier and and the previous revision's identifier, if a previous revision exists.
	/// </summary>
	public class RevisionIdDelta<UserType> {
		private readonly RevisionDelta<int?, UserType> revisionDelta;

		internal RevisionIdDelta( int newRevisionId, int? oldRevisionId, UserTransaction oldTransaction, UserType oldUser ) {
			revisionDelta = new RevisionDelta<int?, UserType>( newRevisionId, oldRevisionId, oldTransaction, oldUser );
		}

		/// <summary>
		/// Gets the revision identifier.
		/// </summary>
		public int New { get { return revisionDelta.New.Value; } }

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasOld { get { return revisionDelta.HasOld; } }

		/// <summary>
		/// Gets the previous revision's identifier, if a previous revision exists.
		/// </summary>
		public int Old { get { return revisionDelta.Old.Value; } }

		/// <summary>
		/// Gets the previous revision's transaction, if a previous revision exists.
		/// </summary>
		public UserTransaction OldTransaction { get { return revisionDelta.OldTransaction; } }

		/// <summary>
		/// Gets the previous revision's user, if a previous revision exists.
		/// </summary>
		public UserType OldUser { get { return revisionDelta.OldUser; } }

		/// <summary>
		/// Returns a full revision-delta object that is created using the specified revision-data selector.
		/// </summary>
		public RevisionDelta<RevisionDataType, UserType> ToFullDelta<RevisionDataType>( Func<int, RevisionDataType> revisionDataSelector ) {
			return new RevisionDelta<RevisionDataType, UserType>(
				revisionDataSelector( New ),
				HasOld ? revisionDataSelector( Old ) : default( RevisionDataType ),
				OldTransaction,
				OldUser );
		}
	}
}