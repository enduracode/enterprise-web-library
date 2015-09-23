using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// Defines how revision history operations will be carried out against the database for a particular system.
	/// </summary>
	public interface RevisionHistoryProvider: SystemDataAccessProvider {
		/// <summary>
		/// Retrieves all user transactions.
		/// </summary>
		IEnumerable<UserTransaction> GetAllUserTransactions();

		/// <summary>
		/// Inserts a new user transaction and returns the ID.
		/// </summary>
		void InsertUserTransaction( int userTransactionId, DateTime transactionDateTime, int? userId );

		/// <summary>
		/// Retrieves all revisions.
		/// </summary>
		IEnumerable<Revision> GetAllRevisions();

		/// <summary>
		/// Retrieves the revision with the specified ID.
		/// </summary>
		Revision GetRevision( int revisionId );

		/// <summary>
		/// Inserts a new revision with the specified parameters.
		/// </summary>
		void InsertRevision( int revisionId, int latestRevisionId, int userTransactionId );

		/// <summary>
		/// Updates the existing revision with the specified ID using the specified parameters.
		/// </summary>
		void UpdateRevision( int revisionId, int latestRevisionId, int userTransactionId, int revisionIdForWhereClause );

		/// <summary>
		/// Retrieves a query like "SELECT RevisionId FROM Revisions WHERE RevisionId = LatestRevisionId", but with system-specific table and column names.
		/// </summary>
		string GetLatestRevisionsQuery();
	}
}