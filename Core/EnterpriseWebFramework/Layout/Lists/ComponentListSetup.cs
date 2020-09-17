using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The general configuration for a component list.
	/// </summary>
	public class ComponentListSetup {
		private class CustomFlowComponent: FlowComponent {
			private readonly IReadOnlyCollection<FlowComponentOrNode> children;

			public CustomFlowComponent( IEnumerable<FlowComponentOrNode> children ) {
				this.children = children.ToImmutableArray();
			}

			IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
		}

		private readonly Func<ElementClassSet, IEnumerable<Tuple<ComponentListItem, FlowComponentOrNode>>, IReadOnlyCollection<FlowComponentOrNode>>
			componentGetter;

		/// <summary>
		/// Creates a component-list setup object.
		/// </summary>
		/// <param name="displaySetup"></param>
		/// <param name="isOrdered">Pass true if the list items have been intentionally ordered, such that changing the order would change the meaning of the page.</param>
		/// <param name="classes">The classes on the list.</param>
		/// <param name="lastItemAutofocusCondition">Pass a value to wrap the last list item in an autofocus region with the specified condition.</param>
		/// <param name="tailUpdateRegions">The tail update regions.</param>
		/// <param name="itemInsertionUpdateRegions"></param>
		/// <param name="etherealContent"></param>
		public ComponentListSetup(
			DisplaySetup displaySetup = null, bool isOrdered = false, ElementClassSet classes = null, AutofocusCondition lastItemAutofocusCondition = null,
			IEnumerable<TailUpdateRegion> tailUpdateRegions = null, IEnumerable<ItemInsertionUpdateRegion> itemInsertionUpdateRegions = null,
			IReadOnlyCollection<EtherealComponent> etherealContent = null ) {
			componentGetter = ( listTypeClasses, items ) => {
				items = items.ToImmutableArray();

				var itemComponents = items.Select( i => i.Item2 ).ToImmutableArray();
				var itemComponentsById = items.Where( i => i.Item1.Id.Any() ).ToDictionary( i => i.Item1.Id, i => i.Item2 );

				return new IdentifiedFlowComponent(
					() => new IdentifiedComponentData<FlowComponentOrNode>(
						"",
						new[]
							{
								new UpdateRegionLinker(
									"tail",
									from region in tailUpdateRegions ?? ImmutableArray<TailUpdateRegion>.Empty
									let staticItemCount = items.Count() - region.UpdatingItemCount
									select new PreModificationUpdateRegion( region.Sets, () => itemComponents.Skip( staticItemCount ), staticItemCount.ToString ),
									arg => itemComponents.Skip( int.Parse( arg ) ) ),
								new UpdateRegionLinker(
									"add",
									from region in itemInsertionUpdateRegions ?? ImmutableArray<ItemInsertionUpdateRegion>.Empty
									select new PreModificationUpdateRegion(
										region.Sets,
										() => ImmutableArray<PageComponent>.Empty,
										() => StringTools.ConcatenateWithDelimiter( ",", region.NewItemIdGetter().ToArray() ) ),
									arg => arg.Separate( ",", false ).Where( itemComponentsById.ContainsKey ).Select( i => itemComponentsById[ i ] ) ),
								new UpdateRegionLinker(
									"remove",
									items.Select(
										( item, index ) => new PreModificationUpdateRegion(
											item.Item1.RemovalUpdateRegionSets,
											() => itemComponents.ElementAt( index ).ToCollection(),
											() => "" ) ),
									arg => ImmutableArray<PageComponent>.Empty )
							},
						new ErrorSourceSet(),
						errorsBySource => new DisplayableElement(
							context => {
								return new DisplayableElementData(
									displaySetup,
									() => new DisplayableElementLocalData( isOrdered ? "ol" : "ul" ),
									classes: CssElementCreator.AllListsClass.Add( listTypeClasses ).Add( classes ?? ElementClassSet.Empty ),
									children: itemComponents.Select(
											( component, index ) => index == itemComponents.Length - 1 && lastItemAutofocusCondition != null
												                        ? new FlowAutofocusRegion(
													                        lastItemAutofocusCondition,
													                        new CustomFlowComponent( component.ToCollection() ).ToCollection() )
												                        : component )
										.Materialize(),
									etherealChildren: etherealContent );
							} ).ToCollection() ) ).ToCollection();
			};
		}

		internal IReadOnlyCollection<FlowComponentOrNode> GetComponents(
			ElementClassSet classes, IEnumerable<Tuple<ComponentListItem, FlowComponentOrNode>> items ) {
			return componentGetter( classes, items );
		}
	}
}