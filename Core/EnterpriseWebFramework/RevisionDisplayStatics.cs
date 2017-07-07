using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnterpriseWebLibrary.DataAccess.RevisionHistory;
using MoreLinq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class RevisionDisplayStatics {
		public static IReadOnlyCollection<ComponentListItem> ToNewAndOldListItem<ValType>( this ValueDelta<ValType> valueDelta, Func<ValType, string> valueSelector ) {
			return valueDelta.ToNewAndOldListItem(
				i => {
					var value = valueSelector( i );
					return value.Any() ? valueSelector( i ).ToComponents() : null;
				} );
		}

		public static IReadOnlyCollection<ComponentListItem> ToNewAndOldListItem<ValType>(
			this ValueDelta<ValType> valueDelta, Func<ValType, IReadOnlyCollection<PhrasingComponent>> valueSelector ) {
			if( !valueDelta.ValueChanged )
				return ImmutableArray<ComponentListItem>.Empty;

			var components = new List<PhrasingComponent>();

			var newComponents = valueSelector( valueDelta.New );
			if( newComponents != null ) {
				components.Add( new ImportantContent( $"{valueDelta.ValueName.CapitalizeString()}:".ToComponents() ) );
				components.AddRange( " ".ToComponents() );
				components.Add( new ImportantContent( newComponents ) );
			}
			else
				components.Add( new ImportantContent( $"{valueDelta.ValueName.CapitalizeString()} cleared".ToComponents() ) );

			var oldComponents = valueSelector( valueDelta.Old );
			if( oldComponents != null ) {
				components.AddRange( " (from ".ToComponents() );
				components.AddRange( oldComponents );
				components.AddRange( ")".ToComponents() );
			}

			return components.ToComponentListItem().ToCollection();
		}

		public static IReadOnlyCollection<ComponentListItem> ToPhraseListItem( this ValueDelta<bool> valueDelta, string toTruePhrase, string toFalsePhrase ) {
			return valueDelta.ToPhraseListItem( toTruePhrase.CapitalizeString().ToComponents(), toFalsePhrase.CapitalizeString().ToComponents() );
		}

		public static IReadOnlyCollection<ComponentListItem> ToPhraseListItem(
			this ValueDelta<bool> valueDelta, IReadOnlyCollection<PhrasingComponent> toTruePhrase, IReadOnlyCollection<PhrasingComponent> toFalsePhrase ) {
			return valueDelta.ValueChanged
				       ? new ImportantContent( valueDelta.New ? toTruePhrase : toFalsePhrase ).ToCollection().ToComponentListItem().ToCollection()
				       : ImmutableArray<ComponentListItem>.Empty;
		}

		/// <summary>
		/// Creates a collection of single-line list items from these revision deltas. Does not identify entities: if a delta contains both new and old revisions,
		/// these will be displayed separately, as one "added" item and one "removed" item.
		/// </summary>
		/// <param name="deltas"></param>
		/// <param name="entityName"></param>
		/// <param name="valueSelector"></param>
		/// <param name="orderer">A function that orders a sequence of revisions.</param>
		public static IReadOnlyCollection<ComponentListItem> ToUnidentifiedSingleLineListItems<RevisionDataType, UserType>(
			this IReadOnlyCollection<RevisionDelta<RevisionDataType, UserType>> deltas, string entityName,
			Func<RevisionDataType, IReadOnlyCollection<PhrasingComponent>> valueSelector,
			Func<IReadOnlyCollection<RevisionDataType>, IEnumerable<RevisionDataType>> orderer ) {
			var listItems = from isNewRevision in new[] { true, false }
			                let valueComponentCollectionsByRevision = deltas.Where( i => i.HasOld || isNewRevision ).Select(
				                i => {
					                var revision = isNewRevision ? i.New : i.Old;
					                var valueComponents = valueSelector( revision );
					                return new { revision, valueComponents };
				                } ).Where( i => i.valueComponents != null ).ToImmutableDictionary( i => i.revision, i => i.valueComponents )
			                from revision in orderer( valueComponentCollectionsByRevision.Keys.ToImmutableArray() )
			                select
				                isNewRevision
					                ? new ImportantContent( $"{entityName.CapitalizeString()} added:".ToComponents() ).ToCollection<PhrasingComponent>()
						                  .Concat( " ".ToComponents() )
						                  .Concat( new ImportantContent( valueComponentCollectionsByRevision[ revision ] ) )
						                  .ToComponentListItem()
					                : $"{entityName.CapitalizeString()} removed:".ToComponents()
						                  .Concat( " ".ToComponents() )
						                  .Concat( valueComponentCollectionsByRevision[ revision ] )
						                  .ToComponentListItem();
			return listItems.ToImmutableArray();
		}

		/// <summary>
		/// Creates a collection of single-line list items from these revision deltas. Identifies entities: if a delta contains both new and old revisions, these
		/// will be displayed together in one item.
		/// </summary>
		/// <param name="deltas"></param>
		/// <param name="entityName"></param>
		/// <param name="valueSelector"></param>
		/// <param name="orderer">A function that orders a sequence of revisions. To access the entity ID, i.e. the latest-revision ID, use
		/// <see cref="RevisionHistoryStatics.RevisionsById"/>.</param>
		/// <param name="identifierSelector">A function that takes a revision and returns a collection of components that identify the entity. This should always
		/// return the same thing for new and old revisions within a delta.</param>
		public static IReadOnlyCollection<ComponentListItem> ToIdentifiedSingleLineListItems<RevisionDataType, UserType>(
			this IReadOnlyCollection<RevisionDelta<RevisionDataType, UserType>> deltas, string entityName,
			Func<RevisionDataType, IReadOnlyCollection<PhrasingComponent>> valueSelector,
			Func<IReadOnlyCollection<RevisionDataType>, IEnumerable<RevisionDataType>> orderer,
			Func<RevisionDataType, IReadOnlyCollection<PhrasingComponent>> identifierSelector ) {
			var newAndOldValueComponentCollectionsByRevision = deltas.Select(
				delta => {
					var newValueComponents = valueSelector( delta.New );
					var oldValueComponents = delta.HasOld ? valueSelector( delta.Old ) : null;
					return new { delta, newValueComponents, oldValueComponents };
				} )
				.Where( i => i.newValueComponents != null || i.oldValueComponents != null )
				.ToImmutableDictionary( i => i.newValueComponents != null ? i.delta.New : i.delta.Old, i => new { i.newValueComponents, i.oldValueComponents } );
			var listItems = from revision in orderer( newAndOldValueComponentCollectionsByRevision.Keys.ToImmutableArray() )
			                let componentCollectionPair = newAndOldValueComponentCollectionsByRevision[ revision ]
			                select
				                componentCollectionPair.newValueComponents != null && componentCollectionPair.oldValueComponents != null
					                ? new ImportantContent(
						                  $"{entityName.CapitalizeString()} ".ToComponents().Concat( identifierSelector( revision ) ).Concat( ":".ToComponents() ) )
						                  .ToCollection<PhrasingComponent>()
						                  .Concat( " ".ToComponents() )
						                  .Concat( new ImportantContent( componentCollectionPair.newValueComponents ) )
						                  .Concat( " (from ".ToComponents() )
						                  .Concat( componentCollectionPair.oldValueComponents )
						                  .Concat( ")".ToComponents() )
						                  .ToComponentListItem()
					                : componentCollectionPair.newValueComponents != null
						                  ? new ImportantContent(
							                    $"{entityName.CapitalizeString()} ".ToComponents().Concat( identifierSelector( revision ) ).Concat( " added:".ToComponents() ) )
							                    .ToCollection<PhrasingComponent>()
							                    .Concat( " ".ToComponents() )
							                    .Concat( new ImportantContent( componentCollectionPair.newValueComponents ) )
							                    .ToComponentListItem()
						                  : $"{entityName.CapitalizeString()} ".ToComponents()
							                    .Concat( identifierSelector( revision ) )
							                    .Concat( " removed:".ToComponents() )
							                    .Concat( " ".ToComponents() )
							                    .Concat( componentCollectionPair.oldValueComponents )
							                    .ToComponentListItem();
			return listItems.ToImmutableArray();
		}
	}
}