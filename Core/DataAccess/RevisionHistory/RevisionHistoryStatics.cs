﻿using System.Collections.Immutable;
using JetBrains.Annotations;
using MoreLinq;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory;

[ PublicAPI ]
public static class RevisionHistoryStatics {
	private class TransactionListEntityData {
		public int EntityId { get; }
		public IEnumerable<Tuple<IEnumerable<RevisionId>, IEnumerable<Revision>>> RevisionIdListAndRevisionSetPairs { get; }
		public IEnumerable<Tuple<IEnumerable<EventId>, IEnumerable<int>>> EventIdListAndEventIdSetPairs { get; }

		public TransactionListEntityData( int entityId, IEnumerable<( IEnumerable<RevisionId>, Revision )> revisionIdListAndRevisionPairs ) {
			EntityId = entityId;
			RevisionIdListAndRevisionSetPairs = from i in revisionIdListAndRevisionPairs
			                                    group i.Item2 by i.Item1
			                                    into grouping
			                                    select Tuple.Create( grouping.Key, grouping.AsEnumerable() );
			EventIdListAndEventIdSetPairs = Enumerable.Empty<Tuple<IEnumerable<EventId>, IEnumerable<int>>>();
		}

		public TransactionListEntityData( int entityId, IEnumerable<( IEnumerable<EventId>, int )> eventIdListAndEventIdPairs ) {
			EntityId = entityId;
			RevisionIdListAndRevisionSetPairs = Enumerable.Empty<Tuple<IEnumerable<RevisionId>, IEnumerable<Revision>>>();
			EventIdListAndEventIdSetPairs = from i in eventIdListAndEventIdPairs
			                                group i.Item2 by i.Item1
			                                into grouping
			                                select Tuple.Create( grouping.Key, grouping.AsEnumerable() );
		}

		public TransactionListEntityData( TransactionListEntityData revisionData, TransactionListEntityData eventData ) {
			EntityId = revisionData.EntityId;
			RevisionIdListAndRevisionSetPairs = revisionData.RevisionIdListAndRevisionSetPairs;
			EventIdListAndEventIdSetPairs = eventData.EventIdListAndEventIdSetPairs;
		}
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	public static RevisionHistoryProvider SystemProvider => (RevisionHistoryProvider)DataAccessStatics.SystemProvider;

	/// <summary>
	/// Returns a list of revisions and events that are related to the specified IDs. This includes revisions that match the IDs as well as all others that
	/// share a latest revision ID. The revisions and events within a user transaction that have the same "conceptual entity" ID are grouped into a single item.
	/// The list is ordered by transaction date/time, descending.
	/// </summary>
	/// <param name="entityTypeRevisionIdLists">The revision-ID lists. Use a separate list for each database entity type that you would like to aggregate into
	/// the conceptual entity.</param>
	/// <param name="entityTypeEventIdLists">The event-ID lists. Use a separate list for each event-list type that you would like to aggregate into the
	/// conceptual-entity activity.</param>
	/// <param name="conceptualEntityStateSelector">A function that uses a revision-ID lookup function to return a representation of the conceptual entity's
	/// state at a particular transaction. The lookup function takes one of the revision-ID-list references from the first parameter and returns the set of
	/// revision IDs for that entity type that were effective at the transaction. You can use this to create a conceptual-entity-specific object, maybe with an
	/// anonymous class. If it's convenient, you may also return null if there is no data at the transaction. Do not pass null.</param>
	/// <param name="conceptualEntityActivitySelector">A function that uses a revision-ID lookup function and an event-ID lookup function to return a
	/// representation of the conceptual entity's activity in a particular transaction. The revision-ID lookup function takes one of the revision-ID-list
	/// references from the first parameter and returns a set of revision-ID-delta objects for that entity type. The event-ID lookup function takes one of the
	/// event-ID-list references from the second parameter and returns the set of event IDs for that list type. You can use these functions to create a
	/// conceptual-entity-specific object, maybe with an anonymous class. If it's convenient, you may also return null if there is no data in the transaction.
	/// Do not pass null.</param>
	/// <param name="userSelector">A function that takes a user ID and returns the corresponding user object. Do not pass null.</param>
	public static IEnumerable<TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType>>
		GetTransactionList<ConceptualEntityStateType, ConceptualEntityActivityType, UserType>(
			IEnumerable<IEnumerable<RevisionId>> entityTypeRevisionIdLists, IEnumerable<IEnumerable<EventId>> entityTypeEventIdLists,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<int>>, ConceptualEntityStateType> conceptualEntityStateSelector,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<RevisionIdDelta<UserType>>>, Func<IEnumerable<EventId>, IEnumerable<int>>, ConceptualEntityActivityType>
				conceptualEntityActivitySelector, Func<int, UserType> userSelector ) {
		using( MiniProfiler.Current.Step( "{0} Data Access - Build transaction list".FormatWith( EwlStatics.EwlInitialism ) ) ) {
			var revisionsById = RevisionsById;
			var entityIdsAndRevisionIdListsByLatestRevisionId = entityTypeRevisionIdLists.SelectMany(
					i => i,
					( list, revisionId ) => {
						var revision = revisionsById[ revisionId.Id ];
						return new { revision.LatestRevisionId, ConceptualEntityId = revisionId.ConceptualEntityId ?? revision.LatestRevisionId, RevisionIdList = list };
					} )
				.GroupBy( i => i.LatestRevisionId )
				.ToImmutableDictionary(
					i => i.Key,
					grouping => {
						var cachedGrouping = grouping.ToImmutableArray();
						return Tuple.Create(
							new HashSet<int>( cachedGrouping.Select( i => i.ConceptualEntityId ) ),
							new HashSet<IEnumerable<RevisionId>>( cachedGrouping.Select( i => i.RevisionIdList ) ) );
					} );

			var eventIdAndListPairsByUserTransactionId = entityTypeEventIdLists.SelectMany( i => i, ( list, eventId ) => new { eventId, list } )
				.ToLookup( i => i.eventId.UserTransactionId );

			// Pre-filter user transactions to avoid having to sort the full list below.
			var revisionsByLatestRevisionId = RevisionsByLatestRevisionId;
			var userTransactionsById = UserTransactionsById;
			var userTransactions = entityIdsAndRevisionIdListsByLatestRevisionId.Keys.SelectMany( i => revisionsByLatestRevisionId[ i ] )
				.Select( i => i.UserTransactionId )
				.Concat( eventIdAndListPairsByUserTransactionId.Select( i => i.Key ) )
				.Select( i => userTransactionsById[ i ] )
				.Distinct();

			var revisionsByUserTransactionId = RevisionsByUserTransactionId;
			var entityTransactions = from transaction in from i in userTransactions orderby i.TransactionDateTime, i.UserTransactionId select i
			                         let user = transaction.UserId.HasValue ? userSelector( transaction.UserId.Value ) : default( UserType )
			                         let revisionEntities =
				                         from revision in revisionsByUserTransactionId[ transaction.UserTransactionId ]
				                         let entityIdsAndRevisionIdLists = entityIdsAndRevisionIdListsByLatestRevisionId.GetValueOrDefault( revision.LatestRevisionId )
				                         where entityIdsAndRevisionIdLists != null
				                         from entityId in entityIdsAndRevisionIdLists.Item1
				                         from revisionIdList in entityIdsAndRevisionIdLists.Item2
				                         group ( revisionIdList, revision ) by entityId
				                         into grouping
				                         select new TransactionListEntityData( grouping.Key, grouping )
			                         let eventEntities =
				                         from eventIdAndList in eventIdAndListPairsByUserTransactionId[ transaction.UserTransactionId ]
				                         group ( eventIdAndList.list, eventIdAndList.eventId.Id ) by eventIdAndList.eventId.ConceptualEntityId
				                         into grouping
				                         select new TransactionListEntityData( grouping.Key, grouping )
			                         from entityData in revisionEntities.FullJoin(
					                         eventEntities,
					                         i => i.EntityId,
					                         i => i,
					                         i => i,
					                         ( r, e ) => new TransactionListEntityData( r, e ) )
				                         .OrderBy( i => i.EntityId )
			                         select new
				                         {
					                         entityData.EntityId,
					                         entityData.RevisionIdListAndRevisionSetPairs,
					                         entityData.EventIdListAndEventIdSetPairs,
					                         transaction,
					                         user
				                         };

			var listItems = new List<TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType>>();
			var lastListItemsByEntityId = new Dictionary<int, TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType>>();
			foreach( var entityTransaction in entityTransactions ) {
				lastListItemsByEntityId.TryGetValue( entityTransaction.EntityId, out var lastListItem );

				var newListItem = new TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType>(
					entityTransaction.EntityId,
					entityTransaction.RevisionIdListAndRevisionSetPairs,
					entityTransaction.EventIdListAndEventIdSetPairs,
					conceptualEntityStateSelector,
					conceptualEntityActivitySelector,
					entityTransaction.transaction,
					entityTransaction.user,
					lastListItem );
				listItems.Add( newListItem );
				lastListItemsByEntityId[ entityTransaction.EntityId ] = newListItem;
			}

			return listItems.AsEnumerable().Reverse();
		}
	}

	/// <summary>
	/// Returns a dictionary containing the latest user transaction for each conceptual-entity ID in the specified transaction list. If an entity was active
	/// very recently, it will map to null since there's a chance that a concurrent transaction with a slightly earlier date/time could still commit. This would
	/// modify the entity state without changing the latest transaction.
	/// </summary>
	/// <param name="transactionList">A collection of transaction-list items that is ordered by transaction date/time, descending.</param>
	public static IReadOnlyDictionary<int, UserTransaction?> GetLatestTransactionsByEntityId<ConceptualEntityStateType, ConceptualEntityActivityType, UserType>(
		IEnumerable<TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType>> transactionList ) {
		var latestRevisionTransactionsByEntityId = ImmutableDictionary<int, UserTransaction?>.Empty.ToBuilder();
		var cutoffDateAndTime = DateTime.Now.AddMinutes( -5 );
		foreach( var i in transactionList ) {
			if( latestRevisionTransactionsByEntityId.ContainsKey( i.ConceptualEntityId ) )
				continue;
			var transaction = i.Transaction;
			latestRevisionTransactionsByEntityId.Add( i.ConceptualEntityId, transaction.TransactionDateTime < cutoffDateAndTime ? transaction : null );
		}
		return latestRevisionTransactionsByEntityId.ToImmutable();
	}

	/// <summary>
	/// Get a dictionary of all transactions by ID.
	/// </summary>
	public static Dictionary<int, UserTransaction> UserTransactionsById =>
		DataAccessState.Current.GetCacheValue( "ewl-userTransactionsById", () => SystemProvider.GetAllUserTransactions().ToDictionary( i => i.UserTransactionId ) );

	/// <summary>
	/// Gets a dictionary of all revisions by ID.
	/// </summary>
	public static Dictionary<int, Revision> RevisionsById =>
		DataAccessState.Current.GetCacheValue( "ewl-revisionsById", () => SystemProvider.GetAllRevisions().ToDictionary( i => i.RevisionId ) );

	internal static ILookup<int, Revision> RevisionsByLatestRevisionId =>
		DataAccessState.Current.GetCacheValue( "ewl-revisionsByLatestRevisionId", () => SystemProvider.GetAllRevisions().ToLookup( i => i.LatestRevisionId ) );

	internal static ILookup<int, Revision> RevisionsByUserTransactionId =>
		DataAccessState.Current.GetCacheValue( "ewl-revisionsByUserTransactionId", () => SystemProvider.GetAllRevisions().ToLookup( i => i.UserTransactionId ) );
}