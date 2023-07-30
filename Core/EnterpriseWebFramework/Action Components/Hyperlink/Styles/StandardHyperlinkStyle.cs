#nullable disable
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays a hyperlink as text.
	/// </summary>
	public class StandardHyperlinkStyle: HyperlinkStyle {
		private readonly ActionComponentIcon icon;
		private readonly string text;

		/// <summary>
		/// Creates a standard style object.
		/// </summary>
		/// <param name="text">Do not pass null. Pass the empty string to use the destination URL.</param>
		/// <param name="icon">The icon.</param>
		public StandardHyperlinkStyle( string text, ActionComponentIcon icon = null ) {
			this.icon = icon;
			this.text = text;
		}

		ElementClassSet HyperlinkStyle.GetClasses() =>
			ActionComponentCssElementCreator.AllStylesClass.Add( ActionComponentCssElementCreator.HyperlinkStandardStyleClass );

		IReadOnlyCollection<FlowComponent> HyperlinkStyle.GetChildren( string destinationUrl ) =>
			ActionComponentIcon.GetIconAndTextComponents( icon, text.Any() ? text : destinationUrl );

		string HyperlinkStyle.GetJsInitStatements( string id ) => "";
	}
}