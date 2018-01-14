using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A style that displays a hyperlink in a custom way.
	/// </summary>
	public class CustomHyperlinkStyle: HyperlinkStyle {
		private readonly ElementClassSet classes;
		private readonly IEnumerable<FlowComponent> children;

		/// <summary>
		/// Creates a custom style object.
		/// </summary>
		/// <param name="classes">The classes on the hyperlink.</param>
		/// <param name="children"></param>
		public CustomHyperlinkStyle( ElementClassSet classes = null, IEnumerable<FlowComponent> children = null ) {
			this.classes = classes;
			this.children = children;
		}

		ElementClassSet HyperlinkStyle.GetClasses() {
			return ActionComponentCssElementCreator.AllStylesClass.Add( classes ?? ElementClassSet.Empty );
		}

		IEnumerable<FlowComponent> HyperlinkStyle.GetChildren( string destinationUrl ) {
			return children;
		}

		string HyperlinkStyle.GetJsInitStatements( string id ) {
			return "";
		}
	}
}