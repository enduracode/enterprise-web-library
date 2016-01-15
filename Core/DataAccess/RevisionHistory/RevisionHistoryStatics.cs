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
		/// a latest revision ID. The revisions within a user transaction that have the same "conceptual entity" ID are grouped into a single "conceptual revision".
		/// The list is ordered by transaction date/time, descending.
		/// </summary>
		/// <param name="entityTypeRevisionIdLists">The revision-ID lists. Use a separate list for each database entity type that you would like to aggregate into
		/// the conceptual revisions.</param>
		/// <param name="conceptualEntityStateSelector">A function that uses a revision-ID lookup function to return a representation of the conceptual entity's
		/// state at a particular revision. The lookup function takes one of the revision-ID-list references from the first parameter and returns the set of
		/// revision IDs for that entity type that were effective at the revision. You can use this to create a conceptual-entity-specific object, maybe with an
		/// anonymous class. If it's convenient, you may also return null if there is no data at the revision. Do not pass null.</param>
		/// <param name="conceptualEntityDeltaSelector">A function that uses a revision-ID lookup function to return a representation of the conceptual entity's
		/// changes in a particular revision. The lookup function takes one of the revision-ID-list references from the first parameter and returns a set of
		/// revision-ID-delta objects for that entity type. You can use this to create a conceptual-entity-specific object, maybe with an anonymous class. If it's
		/// convenient, you may also return null if there is no data for the revision. Do not pass null.</param>
		/// <param name="userSelector">A function that takes a user ID and returns the corresponding user object. Do not pass null.</param>
		public static IEnumerable<ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType>> GetAllRelatedRevisions
			<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType>(
			IEnumerable<IEnumerable<RevisionId>> entityTypeRevisionIdLists,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<int>>, ConceptualEntityStateType> conceptualEntityStateSelector,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<RevisionIdDelta<UserType>>>, ConceptualEntityDeltaType> conceptualEntityDeltaSelector,
			Func<int, UserType> userSelector ) {
			var revisionsById = RevisionsById;
			var entityIdsAndRevisionIdListsByLatestRevisionId = entityTypeRevisionIdLists.SelectMany(
				i => i,
				( list, revisionId ) => {
					var revision = revisionsById[ revisionId.Id ];
					return new { revision.LatestRevisionId, ConceptualEntityId = revisionId.ConceptualEntityId ?? revision.LatestRevisionId, RevisionIdList = list };
				} ).GroupBy( i => i.LatestRevisionId ).ToDictionary(
					i => i.Key,
					grouping => {
						var cachedGrouping = grouping.ToArray();
						return Tuple.Create(
							new HashSet<int>( cachedGrouping.Select( i => i.ConceptualEntityId ) ),
							new HashSet<IEnumerable<RevisionId>>( cachedGrouping.Select( i => i.RevisionIdList ) ) );
					} );

			// Pre-filter user transactions to avoid having to sort the full list below.
			var revisionsByLatestRevisionId = RevisionsByLatestRevisionId;
			var userTransactionsById = UserTransactionsById;
			var userTransactions =
				entityIdsAndRevisionIdListsByLatestRevisionId.Keys.SelectMany( i => revisionsByLatestRevisionId[ i ] )
					.Select( i => userTransactionsById[ i.UserTransactionId ] )
					.Distinct();

			var revisionsByUserTransactionId = RevisionsByUserTransactionId;
			var entityIdAndRevisionIdListGetter = new Func<int, Tuple<HashSet<int>, HashSet<IEnumerable<RevisionId>>>>(
				latestRevisionId => {
					Tuple<HashSet<int>, HashSet<IEnumerable<RevisionId>>> val;
					entityIdsAndRevisionIdListsByLatestRevisionId.TryGetValue( latestRevisionId, out val );
					return val;
				} );
			var entityTransactions = from transaction in from i in userTransactions orderby i.TransactionDateTime, i.UserTransactionId select i
			                         let user = transaction.UserId.HasValue ? userSelector( transaction.UserId.Value ) : default( UserType )
			                         from entityGrouping in from revision in revisionsByUserTransactionId[ transaction.UserTransactionId ]
			                                                let entityIdsAndRevisionIdLists = entityIdAndRevisionIdListGetter( revision.LatestRevisionId )
			                                                where entityIdsAndRevisionIdLists != null
			                                                from entityId in entityIdsAndRevisionIdLists.Item1
			                                                from revisionIdList in entityIdsAndRevisionIdLists.Item2
			                                                group new { revisionIdList, revision } by entityId
			                                                into grouping
			                                                orderby grouping.Key
			                                                select grouping
			                         let entityId = entityGrouping.Key
			                         let revisionIdListAndRevisionSetPairs =
				                         from i in entityGrouping
				                         group i.revision by i.revisionIdList
				                         into grouping select Tuple.Create( grouping.Key, grouping.AsEnumerable() )
			                         select new { entityId, revisionIdListAndRevisionSetPairs, transaction, user };

			var conceptualRevisions = new List<ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType>>();
			var lastConceptualRevisionsByEntityId = new Dictionary<int, ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType>>();
			foreach( var entityTransaction in entityTransactions ) {
				ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType> lastConceptualRevision;
				lastConceptualRevisionsByEntityId.TryGetValue( entityTransaction.entityId, out lastConceptualRevision );

				var newConceptualRevision = new ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType>(
					entityTransaction.entityId,
					entityTransaction.revisionIdListAndRevisionSetPairs,
					conceptualEntityStateSelector,
					conceptualEntityDeltaSelector,
					entityTransaction.transaction,
					entityTransaction.user,
					lastConceptualRevision );
				conceptualRevisions.Add( newConceptualRevision );
				lastConceptualRevisionsByEntityId[ entityTransaction.entityId ] = newConceptualRevision;
			}

			return conceptualRevisions.AsEnumerable().Reverse();
		}

		/// <summary>
		/// Get a dictionary of all transactions by ID.
		/// </summary>
		public static Dictionary<int, UserTransaction> UserTransactionsById {
			get {
				return DataAccessState.Current.GetCacheValue(
					"ewl-userTransactionsById",
					() => SystemProvider.GetAllUserTransactions().ToDictionary( i => i.UserTransactionId ) );
			}
		}

		/// <summary>
		/// Gets a dictionary of all revisions by ID.
		/// </summary>
		public static Dictionary<int, Revision> RevisionsById {
			get { return DataAccessState.Current.GetCacheValue( "ewl-revisionsById", () => SystemProvider.GetAllRevisions().ToDictionary( i => i.RevisionId ) ); }
		}

		internal static ILookup<int, Revision> RevisionsByLatestRevisionId {
			get {
				return DataAccessState.Current.GetCacheValue(
					"ewl-revisionsByLatestRevisionId",
					() => SystemProvider.GetAllRevisions().ToLookup( i => i.LatestRevisionId ) );
			}
		}

		internal static ILookup<int, Revision> RevisionsByUserTransactionId {
			get {
				return DataAccessState.Current.GetCacheValue(
					"ewl-revisionsByUserTransactionId",
					() => SystemProvider.GetAllRevisions().ToLookup( i => i.UserTransactionId ) );
			}
		}
	}
}