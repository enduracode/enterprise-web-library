using System;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A transaction performed by a user.
	/// </summary>
	public class UserTransaction {
		private readonly int userTransactionId;
		private readonly DateTime transactionDateTime;
		private readonly int? userId;

		/// <summary>
		/// Creates a user transaction.
		/// </summary>
		public UserTransaction( int userTransactionId, DateTime transactionDateTime, int? userId ) {
			this.userTransactionId = userTransactionId;
			this.transactionDateTime = transactionDateTime;
			this.userId = userId;
		}

		/// <summary>
		/// Gets the transaction's ID.
		/// </summary>
		public int UserTransactionId { get { return userTransactionId; } }

		/// <summary>
		/// Gets the transaction's date/time.
		/// </summary>
		public DateTime TransactionDateTime { get { return transactionDateTime; } }

		/// <summary>
		/// Gets the transaction's user ID.
		/// </summary>
		public int? UserId { get { return userId; } }
	}
}