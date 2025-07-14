using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Phrasing;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A line break.
/// </summary>
[ PublicAPI ]
public sealed class LineBreak: PhrasingComponent {
	private readonly IReadOnlyCollection<FlowComponentOrNode> children;

	/// <summary>
	/// Creates a line break.
	/// </summary>
	public LineBreak() {
		children = new ElementComponent( _ => new ElementData( () => new ElementLocalData( "br" ) ) ).ToCollection();
	}

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
}