using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for a component list.
	/// </summary>
	public class ComponentListItem {
		private readonly Func<bool, ElementClassSet, CssLength, FlowComponentOrNode> componentGetter;
		internal readonly string Id;
		internal readonly IEnumerable<UpdateRegionSet> RemovalUpdateRegionSets;

		internal ComponentListItem(
			Func<bool, ElementClassSet, CssLength, FlowComponentOrNode> componentGetter, string id, IEnumerable<UpdateRegionSet> removalUpdateRegionSets ) {
			this.componentGetter = componentGetter;
			Id = id;
			RemovalUpdateRegionSets = removalUpdateRegionSets;
		}

		internal Tuple<ComponentListItem, FlowComponentOrNode> GetItemAndComponent( ElementClassSet classes, CssLength width, bool includeContentContainer = false ) {
			return Tuple.Create( this, componentGetter( includeContentContainer, classes, width ) );
		}
	}

	public static class ComponentListItemExtensionCreators {
		/// <summary>
		/// Creates a list item containing these components.
		/// </summary>
		/// <param name="children"></param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the item.</param>
		/// <param name="visualOrderRank"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this item will be a part of.</param>
		/// <param name="etherealChildren"></param>
		public static ComponentListItem ToComponentListItem(
			this IEnumerable<FlowComponent> children, DisplaySetup displaySetup = null, ElementClassSet classes = null, int? visualOrderRank = null,
			IEnumerable<UpdateRegionSet> updateRegionSets = null, IEnumerable<EtherealComponent> etherealChildren = null ) {
			return children.ToComponentListItem(
				"",
				displaySetup: displaySetup,
				classes: classes,
				visualOrderRank: visualOrderRank,
				updateRegionSets: updateRegionSets,
				etherealChildren: etherealChildren );
		}

		/// <summary>
		/// Creates a list item containing these components.
		/// </summary>
		/// <param name="children"></param>
		/// <param name="id">The ID of the item. This is required if you're adding the item on an intermediate post-back or want to remove the item on an
		/// intermediate post-back. Do not pass null or the empty string.</param>
		/// <param name="displaySetup"></param>
		/// <param name="classes">The classes on the item.</param>
		/// <param name="visualOrderRank"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this item will be a part of.</param>
		/// <param name="removalUpdateRegionSets">The intermediate-post-back update-region sets that this item's removal will be a part of.</param>
		/// <param name="etherealChildren"></param>
		public static ComponentListItem ToComponentListItem(
			this IEnumerable<FlowComponent> children, string id, DisplaySetup displaySetup = null, ElementClassSet classes = null, int? visualOrderRank = null,
			IEnumerable<UpdateRegionSet> updateRegionSets = null, IEnumerable<UpdateRegionSet> removalUpdateRegionSets = null,
			IEnumerable<EtherealComponent> etherealChildren = null ) {
			return new ComponentListItem(
				( includeContentContainer, itemTypeClasses, width ) => {
					FlowComponentOrNode component = null;
					component = new IdentifiedFlowComponent(
						() => new IdentifiedComponentData<FlowComponentOrNode>(
							id,
							new UpdateRegionLinker(
								"",
								new PreModificationUpdateRegion( updateRegionSets, component.ToCollection, () => "" ).ToCollection(),
								arg => component.ToCollection() ).ToCollection(),
							ImmutableArray<EwfValidation>.Empty,
							errorsByValidation => new DisplayableElement(
								context => {
									var attributes = new List<Tuple<string, string>>();
									if( visualOrderRank.HasValue || width != null )
										attributes.Add(
											Tuple.Create(
												"style",
												StringTools.ConcatenateWithDelimiter(
													", ",
													visualOrderRank.HasValue ? "order: {0}".FormatWith( visualOrderRank.Value ) : "",
													width != null ? "width: {0}".FormatWith( width.Value ) : "" ) ) );

									return new DisplayableElementData(
										displaySetup,
										() => new DisplayableElementLocalData( "li", focusDependentData: new DisplayableElementFocusDependentData( attributes: attributes ) ),
										classes: CssElementCreator.ItemClass.Add( itemTypeClasses ).Add( classes ?? ElementClassSet.Empty ),
										children: includeContentContainer
											          ? new DisplayableElement(
												          innerContext => new DisplayableElementData(
													          null,
													          () => new DisplayableElementLocalData( "div" ),
													          classes: CssElementCreator.ItemClass,
													          children: children,
													          etherealChildren: etherealChildren ) ).ToCollection()
											          : children,
										etherealChildren: includeContentContainer ? null : etherealChildren );
								} ).ToCollection() ) );
					return component;
				},
				id,
				removalUpdateRegionSets );
		}
	}
}