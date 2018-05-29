using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A transaction for a conceptual data entity.
	/// </summary>
	public class TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType> {
		private readonly int conceptualEntityId;

		private readonly Lazy<ImmutableDictionary<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<Revision, UserTransaction, UserType>>>>
			revisionDictionariesByEntityType;

		private readonly Lazy<ConceptualEntityStateType> conceptualEntityState;
		private readonly Lazy<ConceptualEntityActivityType> conceptualEntityActivity;
		private readonly UserTransaction transaction;
		private readonly UserType user;
		private readonly TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType> previous;

		internal TransactionListItem(
			int conceptualEntityId, IEnumerable<Tuple<IEnumerable<RevisionId>, IEnumerable<Revision>>> entityTypeAndRevisionSetPairs,
			IEnumerable<Tuple<IEnumerable<EventId>, IEnumerable<int>>> eventListTypeAndEventIdSetPairs,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<int>>, ConceptualEntityStateType> conceptualEntityStateSelector,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<RevisionIdDelta<UserType>>>, Func<IEnumerable<EventId>, IEnumerable<int>>, ConceptualEntityActivityType>
				conceptualEntityActivitySelector, UserTransaction transaction, UserType user,
			TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType> previous ) {
			this.conceptualEntityId = conceptualEntityId;

			var cachedEntityTypeAndRevisionSetPairs =
				new Lazy<IReadOnlyCollection<Tuple<IEnumerable<RevisionId>, IEnumerable<Revision>>>>( () => entityTypeAndRevisionSetPairs.ToImmutableArray() );

			revisionDictionariesByEntityType =
				new Lazy<ImmutableDictionary<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<Revision, UserTransaction, UserType>>>>(
					() => {
						if( previous == null )
							return cachedEntityTypeAndRevisionSetPairs.Value.ToImmutableDictionary(
								entityTypeAndRevisions => entityTypeAndRevisions.Item1,
								entityTypeAndRevisions => entityTypeAndRevisions.Item2.ToImmutableDictionary( i => i.LatestRevisionId, i => Tuple.Create( i, transaction, user ) ) );

						var newEntityTypeAndRevisionDictionaryPairs =
							new List<KeyValuePair<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<Revision, UserTransaction, UserType>>>>(
								cachedEntityTypeAndRevisionSetPairs.Value.Count );
						foreach( var entityTypeAndRevisions in cachedEntityTypeAndRevisionSetPairs.Value ) {
							var revisionsByLatestRevisionId = previous.revisionDictionariesByEntityType.Value.GetValueOrDefault(
								entityTypeAndRevisions.Item1,
								ImmutableDictionary<int, Tuple<Revision, UserTransaction, UserType>>.Empty );
							newEntityTypeAndRevisionDictionaryPairs.Add(
								new KeyValuePair<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<Revision, UserTransaction, UserType>>>(
									entityTypeAndRevisions.Item1,
									revisionsByLatestRevisionId.SetItems(
										entityTypeAndRevisions.Item2.Select(
											i => new KeyValuePair<int, Tuple<Revision, UserTransaction, UserType>>( i.LatestRevisionId, Tuple.Create( i, transaction, user ) ) ) ) ) );
						}
						return previous.revisionDictionariesByEntityType.Value.SetItems( newEntityTypeAndRevisionDictionaryPairs );
					} );

			conceptualEntityState = new Lazy<ConceptualEntityStateType>(
				() => conceptualEntityStateSelector(
					entityType => revisionDictionariesByEntityType.Value
						.GetValueOrDefault( entityType, ImmutableDictionary<int, Tuple<Revision, UserTransaction, UserType>>.Empty )
						.Values.Select( i => i.Item1.RevisionId ) ) );

			conceptualEntityActivity = new Lazy<ConceptualEntityActivityType>(
				() => {
					var revisionSetsByEntityType = cachedEntityTypeAndRevisionSetPairs.Value.ToImmutableDictionary( i => i.Item1, i => i.Item2 );
					var eventIdSetsByEventListType = eventListTypeAndEventIdSetPairs.ToImmutableDictionary( i => i.Item1, i => i.Item2 );
					return conceptualEntityActivitySelector(
						entityType => revisionSetsByEntityType.GetValueOrDefault( entityType, Enumerable.Empty<Revision>() )
							.Select(
								revision => {
									Tuple<Revision, UserTransaction, UserType> previousRevisionAndTransactionAndUser = null;
									if( previous != null ) {
										var previousRevisionsByLatestRevisionId = previous.revisionDictionariesByEntityType.Value.GetValueOrDefault( entityType );
										if( previousRevisionsByLatestRevisionId != null )
											previousRevisionAndTransactionAndUser = previousRevisionsByLatestRevisionId.GetValueOrDefault( revision.LatestRevisionId );
									}
									return previousRevisionAndTransactionAndUser == null
										       ? new RevisionIdDelta<UserType>( revision.RevisionId, null )
										       : new RevisionIdDelta<UserType>(
											       revision.RevisionId,
											       Tuple.Create(
												       previousRevisionAndTransactionAndUser.Item1.RevisionId,
												       previousRevisionAndTransactionAndUser.Item2,
												       previousRevisionAndTransactionAndUser.Item3 ) );
								} ),
						eventListType => eventIdSetsByEventListType.GetValueOrDefault( eventListType, Enumerable.Empty<int>() ) );
				} );

			this.transaction = transaction;
			this.user = user;
			this.previous = previous;
		}

		/// <summary>
		/// Gets the conceptual-entity ID, i.e. the latest-revision ID of the main entity.
		/// </summary>
		public int ConceptualEntityId => conceptualEntityId;

		/// <summary>
		/// Gets the conceptual-entity state. This can be null if you are using that to represent no data at a particular transaction.
		/// </summary>
		public ConceptualEntityStateType ConceptualEntityState => conceptualEntityState.Value;

		/// <summary>
		/// Gets the conceptual-entity-activity object. This can be null if you are using that to represent no data in a particular transaction.
		/// </summary>
		public ConceptualEntityActivityType ConceptualEntityActivity => conceptualEntityActivity.Value;

		/// <summary>
		/// Gets the transaction.
		/// </summary>
		public UserTransaction Transaction => transaction;

		/// <summary>
		/// Gets the user.
		/// </summary>
		public UserType User => user;

		/// <summary>
		/// Gets whether there is a previous transaction for the same conceptual entity.
		/// </summary>
		public bool HasPrevious => previous != null;

		/// <summary>
		/// Gets the previous transaction for the same conceptual entity, or null if this is the first.
		/// </summary>
		public TransactionListItem<ConceptualEntityStateType, ConceptualEntityActivityType, UserType> Previous => previous;
	}
}