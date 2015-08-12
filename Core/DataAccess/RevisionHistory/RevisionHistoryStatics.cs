using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	public static class RevisionHistoryStatics {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public static RevisionHistoryProvider SystemProvider { get { return (RevisionHistoryProvider)DataAccessStatics.SystemProvider; } }

		/// <summary>
		/// Returns a list of the revisions that are related to the specified revision IDs. This includes those that match the IDs as well as all others that share
		/// a latest revision ID. The list is ordered by transaction date/time, descending.
		/// </summary>
		public static IEnumerable<Revision<UserType>> GetAllRelatedRevisions<UserType>( IEnumerable<int> revisionIds, Func<int, UserType> userSelector )
			where UserType: class {
			var revisionsById = RevisionsById;
			var latestRevisionIds = new HashSet<int>( revisionIds.Select( i => revisionsById[ i ].LatestRevisionId ) );

			// Pre-filter user transactions to avoid having to sort the full list below.
			var revisionsByLatestRevisionId = RevisionsByLatestRevisionId;
			var userTransactionsById = UserTransactionsById;
			var userTransactions =
				latestRevisionIds.SelectMany( i => revisionsByLatestRevisionId[ i ] ).Select( i => userTransactionsById[ i.UserTransactionId ] ).Distinct();

			var revisionsByUserTransactionId = RevisionsByUserTransactionId;
			return from userTransaction in from i in userTransactions orderby i.TransactionDateTime descending, i.UserTransactionId descending select i
			       let user = userTransaction.UserId.HasValue ? userSelector( userTransaction.UserId.Value ) : null
			       from revision in
				       revisionsByUserTransactionId[ userTransaction.UserTransactionId ].Where( i => latestRevisionIds.Contains( i.LatestRevisionId ) )
				       .OrderBy( i => i.LatestRevisionId )
			       select new Revision<UserType>( revision, userTransaction, user );
		}

		internal static Dictionary<int, UserTransaction> UserTransactionsById {
			get {
				return DataAccessState.Current.GetCacheValue(
					"ewl-userTransactionsById",
					() => SystemProvider.GetAllUserTransactions().ToDictionary( i => i.UserTransactionId ) );
			}
		}

		internal static Dictionary<int, RevisionRow> RevisionsById {
			get { return DataAccessState.Current.GetCacheValue( "ewl-revisionsById", () => SystemProvider.GetAllRevisions().ToDictionary( i => i.RevisionId ) ); }
		}

		internal static ILookup<int, RevisionRow> RevisionsByLatestRevisionId {
			get {
				return DataAccessState.Current.GetCacheValue(
					"ewl-revisionsByLatestRevisionId",
					() => SystemProvider.GetAllRevisions().ToLookup( i => i.LatestRevisionId ) );
			}
		}

		internal static ILookup<int, RevisionRow> RevisionsByUserTransactionId {
			get {
				return DataAccessState.Current.GetCacheValue(
					"ewl-revisionsByUserTransactionId",
					() => SystemProvider.GetAllRevisions().ToLookup( i => i.UserTransactionId ) );
			}
		}
	}
}