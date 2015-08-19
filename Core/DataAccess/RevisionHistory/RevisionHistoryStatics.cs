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
		/// <param name="revisionIds"></param>
		/// <param name="conceptualRevisionSelector">A function that takes the revision rows linked to a user transaction and returns an unordered list of
		/// conceptual revision objects. If all revision rows are for a single database entity, this can probably be a simple Select call that projects the rows
		/// into conceptual revisions, using the latest-revision ID as the entity ID and an entity-specific row object as the revision data. On the other hand, if
		/// multiple database entities are involved, you should declare and initialize a hash set of conceptual-entity IDs as well as a dictionary for each
		/// entity-specific type, keyed by conceptual-entity ID. Iterate through the revisions, and for each, add the conceptual-entity ID to the hash set and the
		/// entity-specific row object to the appropriate dictionary. Then project the conceptual-entity IDs into conceptual revisions, probably using an anonymous
		/// class for the revision data such that all entity-specific row objects can be included, and return the result. Do not pass or return null.</param>
		/// <param name="userSelector">A function that takes a user ID and returns the corresponding user object. Do not pass null.</param>
		public static IEnumerable<Revision<RevisionDataType, UserType>> GetAllRelatedRevisions<RevisionDataType, UserType>(
			IEnumerable<int> revisionIds, Func<IEnumerable<RevisionRow>, IEnumerable<ConceptualRevision<RevisionDataType>>> conceptualRevisionSelector,
			Func<int, UserType> userSelector ) where UserType: class {
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
			       let revisionRows = revisionsByUserTransactionId[ userTransaction.UserTransactionId ].Where( i => latestRevisionIds.Contains( i.LatestRevisionId ) )
			       from conceptualRevision in conceptualRevisionSelector( revisionRows ).OrderBy( i => i.EntityId )
			       select new Revision<RevisionDataType, UserType>( conceptualRevision.EntityId, conceptualRevision.RevisionData, userTransaction, user );
		}

		/// <summary>
		/// Returns a revision-delta object based on the specified revision and the previous revision, if one exists.
		/// </summary>
		/// <param name="revisionId"></param>
		/// <param name="revisionDataSelector">A function that takes a revision ID and returns the entity-specific revision data. Do not pass null. Return null if
		/// there is no entity-specific data, which can happen if the entity allows deletion but does not use a deleted flag.</param>
		/// <param name="userSelector">A function that takes a user ID and returns the corresponding user object. Do not pass null.</param>
		public static RevisionDelta<RevisionDataType, UserType> GetRevisionDelta<RevisionDataType, UserType>(
			int revisionId, Func<int, RevisionDataType> revisionDataSelector, Func<int, UserType> userSelector ) where UserType: class {
			var revisions =
				GetAllRelatedRevisions(
					revisionId.ToSingleElementArray(),
					revisionRows => revisionRows.Select( i => ConceptualRevision.Create( i.LatestRevisionId, i.RevisionId ) ),
					userSelector ).ToArray();
			var revisionIndex =
				revisions.Select( ( revision, index ) => new { revision, index } ).Where( i => i.revision.RevisionData == revisionId ).Select( i => i.index ).Single();
			var newRev = revisions[ revisionIndex ];
			var oldRev = revisionIndex + 1 < revisions.Count() ? revisions[ revisionIndex + 1 ] : null;
			return
				new RevisionDelta<RevisionDataType, UserType>(
					new Revision<RevisionDataType, UserType>( newRev.EntityId, revisionDataSelector( newRev.RevisionData ), newRev.Transaction, newRev.User ),
					oldRev != null
						? new Revision<RevisionDataType, UserType>( oldRev.EntityId, revisionDataSelector( oldRev.RevisionData ), oldRev.Transaction, oldRev.User )
						: null );
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