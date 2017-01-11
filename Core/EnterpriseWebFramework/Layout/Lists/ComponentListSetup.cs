using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The general configuration for a component list.
	/// </summary>
	public class ComponentListSetup {
		private readonly Func<ElementClassSet, IEnumerable<ComponentListItem>, IReadOnlyCollection<FlowComponentOrNode>> componentGetter;

		/// <summary>
		/// Creates a component-list setup object.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="isOrdered">Pass true if the list items have been intentionally ordered, such that changing the order would change the meaning of the page.</param>
		/// <param name="classes">The classes on the list.</param>
		/// <param name="tailUpdateRegions">The tail update regions.</param>
		/// <param name="itemInsertionUpdateRegions"></param>
		public ComponentListSetup(
			DisplaySetup displaySetup = null, bool isOrdered = false, ElementClassSet classes = null, IEnumerable<TailUpdateRegion> tailUpdateRegions = null,
			IEnumerable<ItemInsertionUpdateRegion> itemInsertionUpdateRegions = null ) {
			componentGetter = ( listTypeClasses, items ) => {
				items = items.ToImmutableArray();

				var itemComponents = new Lazy<IEnumerable<FlowComponentOrNode>>( () => items.Select( i => i.GetComponent() ).ToImmutableArray() );
				var itemComponentsById =
					new Lazy<Dictionary<string, FlowComponentOrNode>>(
						() =>
						itemComponents.Value.Select( ( component, index ) => new { items.ElementAt( index ).Id, component } )
							.Where( i => i.Id.Any() )
							.ToDictionary( i => i.Id, i => i.component ) );

				return
					new IdentifiedFlowComponent(
						() =>
						new IdentifiedComponentData<FlowComponentOrNode>(
							"",
							new[]
								{
									new UpdateRegionLinker(
							"tail",
							from region in tailUpdateRegions ?? ImmutableArray<TailUpdateRegion>.Empty
							let staticItemCount = items.Count() - region.UpdatingItemCount
							select new PreModificationUpdateRegion( region.Sets, () => itemComponents.Value.Skip( staticItemCount ), staticItemCount.ToString ),
							arg => itemComponents.Value.Skip( int.Parse( arg ) ) ),
									new UpdateRegionLinker(
							"add",
							from region in itemInsertionUpdateRegions ?? ImmutableArray<ItemInsertionUpdateRegion>.Empty
							select
								new PreModificationUpdateRegion(
								region.Sets,
								() => ImmutableArray<PageComponent>.Empty,
								() => StringTools.ConcatenateWithDelimiter( ",", region.NewItemIdGetter().ToArray() ) ),
							arg => arg.Separate( ",", false ).Where( itemComponentsById.Value.ContainsKey ).Select( i => itemComponentsById.Value[ i ] ) ),
									new UpdateRegionLinker(
							"remove",
							items.Select(
								( item, index ) =>
								new PreModificationUpdateRegion( item.RemovalUpdateRegionSets, () => itemComponents.Value.ElementAt( index ).ToCollection(), () => "" ) ),
							arg => ImmutableArray<PageComponent>.Empty )
								},
							ImmutableArray<EwfValidation>.Empty,
							errorsByValidation =>
							new DisplayableElement(
								context => {
									return new DisplayableElementData(
										displaySetup,
										() =>
										new DisplayableElementLocalData(
											isOrdered ? "ol" : "ul",
											classes: CssElementCreator.AllListsClass.Union( listTypeClasses ).Union( classes ?? ElementClassSet.Empty ) ),
										children: from i in items select i.GetComponent() );
								} ).ToCollection() ) ).ToCollection();
			};
		}

		internal IReadOnlyCollection<FlowComponentOrNode> GetComponents( ElementClassSet classes, IEnumerable<ComponentListItem> items ) {
			return componentGetter( classes, items );
		}
	}
}