using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A line break opportunity.
/// </summary>
[ PublicAPI ]
public sealed class LineBreakOpportunity: PhrasingComponent {
	private readonly IReadOnlyCollection<FlowComponentOrNode> children;

	/// <summary>
	/// Creates a line break opportunity.
	/// </summary>
	public LineBreakOpportunity() {
		children = new ElementComponent( _ => new ElementData( () => new ElementLocalData( "wbr" ) ) ).ToCollection();
	}

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
}