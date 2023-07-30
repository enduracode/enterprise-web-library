#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A first-level heading that displays the page’s name.
/// </summary>
public class PageName: FlowComponent {
	private readonly string pageName;
	private readonly IReadOnlyCollection<DisplayableElement> children;

	/// <summary>
	/// Creates a page-name heading.
	/// </summary>
	/// <param name="useEntitySetupNameIfEntitySetupIsParent">Pass true to use the entity-setup name if an entity setup exists and is also the parent.</param>
	public PageName( bool useEntitySetupNameIfEntitySetupIsParent = false ) {
		var page = PageBase.Current;
		pageName = useEntitySetupNameIfEntitySetupIsParent && page.EntitySetupIsParent ? page.EsAsBaseType.EntitySetupName : page.ResourceName;

		children = new DisplayableElement(
			_ => new DisplayableElementData( null, () => new DisplayableElementLocalData( "h1" ), children: pageName.ToComponents() ) ).ToCollection();
	}

	/// <summary>
	/// Returns true if this component will not display any content.
	/// </summary>
	public bool IsEmpty => !pageName.Any();

	IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() => children;
}