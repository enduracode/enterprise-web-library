namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision of a data entity, including its transaction and user.
	/// </summary>
	public class Revision<UserType> where UserType: class {
		private readonly RevisionRow revisionRow;
		private readonly UserTransaction transaction;
		private readonly UserType user;

		internal Revision( RevisionRow revisionRow, UserTransaction transaction, UserType user ) {
			this.revisionRow = revisionRow;
			this.transaction = transaction;
			this.user = user;
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
	}
}