#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A style that displays a button as a rectangle with rounded corners.
/// </summary>
public class StandardButtonStyle: ButtonStyle {
	private readonly ButtonSize buttonSize;
	private readonly ActionComponentIcon icon;
	private readonly string text;

	/// <summary>
	/// Creates a standard style object.
	/// </summary>
	/// <param name="text">Do not pass null or the empty string.</param>
	/// <param name="buttonSize"></param>
	/// <param name="icon">The icon.</param>
	public StandardButtonStyle( string text, ButtonSize buttonSize = ButtonSize.Normal, ActionComponentIcon icon = null ) {
		this.buttonSize = buttonSize;
		this.icon = icon;
		this.text = text;
	}

	ElementClassSet ButtonStyle.GetClasses() => ActionComponentCssElementCreator.AllStylesClass.Add( ButtonSizeStatics.Class( buttonSize ) );

	IEnumerable<ElementAttribute> ButtonStyle.GetAttributes() => Enumerable.Empty<ElementAttribute>();

	IReadOnlyCollection<FlowComponent> ButtonStyle.GetChildren() => ActionComponentIcon.GetIconAndTextComponents( icon, text );

	string ButtonStyle.GetJsInitStatements( string id ) => "";
}