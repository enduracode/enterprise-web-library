using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays a hyperlink as a standard button.
	/// </summary>
	public class ButtonHyperlinkStyle: HyperlinkStyle {
		private readonly ButtonSize buttonSize;
		private readonly ActionComponentIcon icon;
		private readonly string text;

		/// <summary>
		/// Creates a button style object.
		/// </summary>
		/// <param name="text">Do not pass null. Pass the empty string to use the destination URL.</param>
		/// <param name="buttonSize"></param>
		/// <param name="icon">The icon.</param>
		public ButtonHyperlinkStyle( string text, ButtonSize buttonSize = ButtonSize.Normal, ActionComponentIcon icon = null ) {
			this.buttonSize = buttonSize;
			this.icon = icon;
			this.text = text;
		}

		ElementClassSet HyperlinkStyle.GetClasses() => ActionComponentCssElementCreator.AllStylesClass.Add( ButtonSizeStatics.Class( buttonSize ) );

		IReadOnlyCollection<FlowComponent> HyperlinkStyle.GetChildren( string destinationUrl ) =>
			ActionComponentIcon.GetIconAndTextComponents( icon, text.Any() ? text : destinationUrl );

		string HyperlinkStyle.GetJsInitStatements( string id ) => "";
	}
}