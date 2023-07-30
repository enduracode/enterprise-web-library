#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A style that displays a button in a custom way.
/// </summary>
public class CustomButtonStyle: ButtonStyle {
	private readonly ElementClassSet classes;
	private readonly IEnumerable<ElementAttribute> attributes;
	private readonly IReadOnlyCollection<FlowComponent> children;

	/// <summary>
	/// Creates a custom style object.
	/// </summary>
	/// <param name="classes">The classes on the button.</param>
	/// <param name="attributes"></param>
	/// <param name="children"></param>
	public CustomButtonStyle(
		ElementClassSet classes = null, IEnumerable<ElementAttribute> attributes = null, IReadOnlyCollection<PhrasingComponent> children = null ) {
		this.classes = classes;
		this.attributes = attributes ?? Enumerable.Empty<ElementAttribute>();
		this.children = children;
	}

	ElementClassSet ButtonStyle.GetClasses() => ActionComponentCssElementCreator.AllStylesClass.Add( classes ?? ElementClassSet.Empty );

	IEnumerable<ElementAttribute> ButtonStyle.GetAttributes() => attributes;

	IReadOnlyCollection<FlowComponent> ButtonStyle.GetChildren() => children;

	string ButtonStyle.GetJsInitStatements( string id ) => "";
}