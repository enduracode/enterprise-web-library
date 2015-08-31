using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.DataAccess.RevisionHistory {
	/// <summary>
	/// A revision of a conceptual data entity, including its transaction and user.
	/// </summary>
	public class ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType> {
		private readonly int conceptualEntityId;

		private readonly Lazy<ImmutableDictionary<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<RevisionRow, UserTransaction, UserType>>>>
			revisionDictionariesByEntityType;

		private readonly Lazy<ConceptualEntityStateType> conceptualEntityState;
		private readonly Lazy<ConceptualEntityDeltaType> conceptualEntityDelta;
		private readonly UserTransaction transaction;
		private readonly UserType user;
		private readonly ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType> previous;

		internal ConceptualRevision(
			int conceptualEntityId, IEnumerable<Tuple<IEnumerable<RevisionId>, IEnumerable<RevisionRow>>> entityTypeAndRevisionSetPairs,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<int>>, ConceptualEntityStateType> conceptualEntityStateSelector,
			Func<Func<IEnumerable<RevisionId>, IEnumerable<RevisionIdDelta<UserType>>>, ConceptualEntityDeltaType> conceptualEntityDeltaSelector,
			UserTransaction transaction, UserType user, ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType> previous ) {
			this.conceptualEntityId = conceptualEntityId;

			var cachedEntityTypeAndRevisionSetPairs =
				new Lazy<IReadOnlyCollection<Tuple<IEnumerable<RevisionId>, IEnumerable<RevisionRow>>>>( () => entityTypeAndRevisionSetPairs.ToImmutableArray() );

			revisionDictionariesByEntityType =
				new Lazy<ImmutableDictionary<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<RevisionRow, UserTransaction, UserType>>>>(
					() => {
						if( previous == null ) {
							return cachedEntityTypeAndRevisionSetPairs.Value.ToImmutableDictionary(
								entityTypeAndRevisions => entityTypeAndRevisions.Item1,
								entityTypeAndRevisions => entityTypeAndRevisions.Item2.ToImmutableDictionary( i => i.LatestRevisionId, i => Tuple.Create( i, transaction, user ) ) );
						}

						var newEntityTypeAndRevisionDictionaryPairs =
							new List<KeyValuePair<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<RevisionRow, UserTransaction, UserType>>>>(
								cachedEntityTypeAndRevisionSetPairs.Value.Count );
						foreach( var entityTypeAndRevisions in cachedEntityTypeAndRevisionSetPairs.Value ) {
							var revisionsByLatestRevisionId = previous.revisionDictionariesByEntityType.Value.GetValueOrDefault(
								entityTypeAndRevisions.Item1,
								ImmutableDictionary<int, Tuple<RevisionRow, UserTransaction, UserType>>.Empty );
							newEntityTypeAndRevisionDictionaryPairs.Add(
								new KeyValuePair<IEnumerable<RevisionId>, ImmutableDictionary<int, Tuple<RevisionRow, UserTransaction, UserType>>>(
									entityTypeAndRevisions.Item1,
									revisionsByLatestRevisionId.SetItems(
										entityTypeAndRevisions.Item2.Select(
											i => new KeyValuePair<int, Tuple<RevisionRow, UserTransaction, UserType>>( i.LatestRevisionId, Tuple.Create( i, transaction, user ) ) ) ) ) );
						}
						return previous.revisionDictionariesByEntityType.Value.SetItems( newEntityTypeAndRevisionDictionaryPairs );
					} );

			conceptualEntityState =
				new Lazy<ConceptualEntityStateType>(
					() =>
					conceptualEntityStateSelector(
						entityType =>
						revisionDictionariesByEntityType.Value.GetValueOrDefault( entityType, ImmutableDictionary<int, Tuple<RevisionRow, UserTransaction, UserType>>.Empty )
							.Values.Select( i => i.Item1.RevisionId ) ) );

			conceptualEntityDelta = new Lazy<ConceptualEntityDeltaType>(
				() => {
					var revisionSetsByEntityType = cachedEntityTypeAndRevisionSetPairs.Value.ToImmutableDictionary( i => i.Item1, i => i.Item2 );
					return conceptualEntityDeltaSelector(
						entityType => revisionSetsByEntityType.GetValueOrDefault( entityType, new RevisionRow[ 0 ] ).Select(
							revision => {
								Tuple<RevisionRow, UserTransaction, UserType> previousRevisionAndTransactionAndUser = null;
								if( previous != null ) {
									var previousRevisionsByLatestRevisionId = previous.revisionDictionariesByEntityType.Value.GetValueOrDefault( entityType );
									if( previousRevisionsByLatestRevisionId != null )
										previousRevisionAndTransactionAndUser = previousRevisionsByLatestRevisionId.GetValueOrDefault( revision.LatestRevisionId );
								}
								return previousRevisionAndTransactionAndUser == null
									       ? new RevisionIdDelta<UserType>( revision.RevisionId, null, null, default( UserType ) )
									       : new RevisionIdDelta<UserType>(
										         revision.RevisionId,
										         previousRevisionAndTransactionAndUser.Item1.RevisionId,
										         previousRevisionAndTransactionAndUser.Item2,
										         previousRevisionAndTransactionAndUser.Item3 );
							} ) );
				} );

			this.transaction = transaction;
			this.user = user;
			this.previous = previous;
		}

		/// <summary>
		/// Gets the revision's conceptual-entity ID, i.e. the latest-revision ID of the main entity.
		/// </summary>
		public int ConceptualEntityId { get { return conceptualEntityId; } }

		/// <summary>
		/// Gets the conceptual-entity state at this revision. This can be null if you are using that to represent no data at a particular revision.
		/// </summary>
		public ConceptualEntityStateType ConceptualEntityState { get { return conceptualEntityState.Value; } }

		/// <summary>
		/// Gets the conceptual-entity-delta object for this revision and the previous revision, if one exists. This can be null if you are using that to represent
		/// no data for a particular revision.
		/// </summary>
		public ConceptualEntityDeltaType ConceptualEntityDelta { get { return conceptualEntityDelta.Value; } }

		/// <summary>
		/// Gets the transaction.
		/// </summary>
		public UserTransaction Transaction { get { return transaction; } }

		/// <summary>
		/// Gets the user.
		/// </summary>
		public UserType User { get { return user; } }

		/// <summary>
		/// Gets whether there is a previous revision.
		/// </summary>
		public bool HasPrevious { get { return previous != null; } }

		/// <summary>
		/// Gets the previous revision, or null if this is the first revision.
		/// </summary>
		public ConceptualRevision<ConceptualEntityStateType, ConceptualEntityDeltaType, UserType> Previous { get { return previous; } }
	}
}