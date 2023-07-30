#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// Lazy loaded data used by an EWF table item group.
/// </summary>
public class EwfTableItemGroupRemainingData {
	internal readonly IReadOnlyCollection<FlowComponent> GroupName;
	internal readonly IReadOnlyCollection<ActionComponentSetup> GroupActions;
	internal readonly ElementActivationBehavior GroupHeadActivationBehavior;
	internal readonly bool? InitiallyCollapsed;
	internal readonly IReadOnlyCollection<TailUpdateRegion> TailUpdateRegions;

	/// <summary>
	/// Creates a remaining data object.
	/// </summary>
	/// <param name="groupName">The name of the group and any other information you want in the group head.</param>
	/// <param name="groupActions">Group action components. Any hyperlink with a destination to which the user cannot navigate (due to authorization logic) will
	/// be automatically hidden by the framework.</param>
	/// <param name="groupHeadActivationBehavior">The activation behavior for the group head</param>
	/// <param name="initiallyCollapsed">Whether the group is initially collapsed. Null means the group cannot be collapsed and is always visible.</param>
	/// <param name="tailUpdateRegions">The tail update regions for the group. If a table uses item limiting, these regions will include all subsequent item
	/// groups in the table. This is necessary because any number of items could be appended to this item group, potentially causing subsequent item groups to
	/// become invisible.</param>
	public EwfTableItemGroupRemainingData(
		IReadOnlyCollection<FlowComponent> groupName, IReadOnlyCollection<ActionComponentSetup> groupActions = null,
		ElementActivationBehavior groupHeadActivationBehavior = null, bool? initiallyCollapsed = null,
		IReadOnlyCollection<TailUpdateRegion> tailUpdateRegions = null ) {
		GroupName = groupName ?? Enumerable.Empty<FlowComponent>().Materialize();
		GroupActions = groupActions ?? Enumerable.Empty<ActionComponentSetup>().Materialize();
		GroupHeadActivationBehavior = groupHeadActivationBehavior;
		InitiallyCollapsed = initiallyCollapsed;
		TailUpdateRegions = tailUpdateRegions ?? Enumerable.Empty<TailUpdateRegion>().Materialize();
	}
}