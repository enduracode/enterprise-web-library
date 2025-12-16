using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A stack list with a post-back that adds new items.
/// </summary>
[ PublicAPI ]
public class ExpandableStackList {
	public FlowComponent List { get; }
	public PostBack? AddItemPostBack { get; }

	/// <summary>
	/// Creates an expandable stack list.
	/// </summary>
	/// <param name="idBase"></param>
	/// <param name="itemGetter">A function that takes an item index and returns the list item.</param>
	/// <param name="itemLimit"></param>
	public ExpandableStackList( string idBase, Func<int, ComponentListItem> itemGetter, int itemLimit = 100 ) {
		var itemCount = ComponentStateItem.Create( $"{idBase}Count", 1, v => v >= 1 && v <= itemLimit, false );
		var updateRegion = new UpdateRegionSet();

		List = new StackList(
			Enumerable.Range( 0, itemCount.Value ).Select( itemGetter ).Materialize(),
			setup: new ComponentListSetup(
				lastItemAutofocusCondition: AutofocusCondition.PostBack( idBase ),
				tailUpdateRegions: new TailUpdateRegion( updateRegion, 0 ),
				etherealContent: itemCount.ToCollection() ) );

		if( itemCount.Value < itemLimit )
			AddItemPostBack = PostBack.CreateIntermediate(
				updateRegion,
				id: idBase,
				modificationMethod: () => itemCount.Value += 1,
				reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: idBase ) );
	}

	/// <summary>
	/// Returns the list followed by content that includes an add-item button.
	/// </summary>
	/// <param name="buttonLabel">The button label. If the entity being added were a “customer”, we recommend a phrase such as “Add another customer” or “Add
	/// another row”.</param>
	/// <param name="addItemContentGetter"></param>
	/// <param name="buttonIcon"></param>
	public IReadOnlyCollection<FlowComponent> GetListWithAddItemContent(
		string buttonLabel, Func<EwfButton, IReadOnlyCollection<FlowComponent>> addItemContentGetter, SpecifiedValue<ActionComponentIcon?>? buttonIcon = null ) {
		var components = new List<FlowComponent>();
		components.Add( List );

		if( AddItemPostBack is not null )
			components.AddRange(
				addItemContentGetter(
					new EwfButton(
						new StandardButtonStyle(
							buttonLabel,
							icon: buttonIcon is null ? new ActionComponentIcon( new FontAwesomeIcon( "fa-plus-circle" ) ) : buttonIcon.Value ),
						behavior: new PostBackBehavior( postBack: AddItemPostBack ) ) ) );

		return components;
	}
}