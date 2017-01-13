using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a component list.
	/// </summary>
	public class ComponentListItem {
		private readonly Func<ElementClassSet, FlowComponentOrNode> componentGetter;
		internal readonly string Id;
		internal readonly IEnumerable<UpdateRegionSet> RemovalUpdateRegionSets;

		/// <summary>
		/// Creates a list item.
		/// </summary>
		/// <param name="children">The item content.</param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the item.</param>
		/// <param name="visualOrderRank"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this item will be a part of.</param>
		/// <param name="etherealChildren"></param>
		public ComponentListItem(
			IEnumerable<FlowComponentOrNode> children, DisplaySetup displaySetup = null, ElementClassSet classes = null, int? visualOrderRank = null,
			IEnumerable<UpdateRegionSet> updateRegionSets = null, IEnumerable<EtherealComponentOrElement> etherealChildren = null )
			: this(
				children,
				"",
				displaySetup: displaySetup,
				classes: classes,
				visualOrderRank: visualOrderRank,
				updateRegionSets: updateRegionSets,
				etherealChildren: etherealChildren ) {}

		/// <summary>
		/// Creates a list item.
		/// </summary>
		/// <param name="children">The item content.</param>
		/// <param name="id">The ID of the item. This is required if you're adding the item on an intermediate post-back or want to remove the item on an
		/// intermediate post-back. Do not pass null or the empty string.</param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the item.</param>
		/// <param name="visualOrderRank"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this item will be a part of.</param>
		/// <param name="removalUpdateRegionSets">The intermediate-post-back update-region sets that this item's removal will be a part of.</param>
		/// <param name="etherealChildren"></param>
		public ComponentListItem(
			IEnumerable<FlowComponentOrNode> children, string id, DisplaySetup displaySetup = null, ElementClassSet classes = null, int? visualOrderRank = null,
			IEnumerable<UpdateRegionSet> updateRegionSets = null, IEnumerable<UpdateRegionSet> removalUpdateRegionSets = null,
			IEnumerable<EtherealComponentOrElement> etherealChildren = null ) {
			componentGetter = itemTypeClasses => {
				FlowComponentOrNode component = null;
				component =
					new IdentifiedFlowComponent(
						() =>
						new IdentifiedComponentData<FlowComponentOrNode>(
							id,
							new UpdateRegionLinker(
							"",
							new PreModificationUpdateRegion( updateRegionSets, component.ToCollection, () => "" ).ToCollection(),
							arg => component.ToCollection() ).ToCollection(),
							ImmutableArray<EwfValidation>.Empty,
							errorsByValidation => new DisplayableElement(
								                      context => {
									                      var attributes = new List<Tuple<string, string>>();
									                      if( visualOrderRank.HasValue )
										                      attributes.Add( Tuple.Create( "style", "order: {0}".FormatWith( visualOrderRank.Value ) ) );

									                      return new DisplayableElementData(
										                      displaySetup,
										                      () =>
										                      new DisplayableElementLocalData(
											                      "li",
											                      classes: CssElementCreator.AllItemAlignmentsClass.Union( itemTypeClasses ).Union( classes ?? ElementClassSet.Empty ),
											                      additionalAttributes: attributes ),
										                      children: children,
										                      etherealChildren: etherealChildren );
								                      } ).ToCollection() ) );
				return component;
			};

			Id = id;
			RemovalUpdateRegionSets = removalUpdateRegionSets;
		}

		internal Tuple<ComponentListItem, FlowComponentOrNode> GetItemAndComponent( ElementClassSet classes ) {
			return Tuple.Create( this, componentGetter( classes ) );
		}
	}
}