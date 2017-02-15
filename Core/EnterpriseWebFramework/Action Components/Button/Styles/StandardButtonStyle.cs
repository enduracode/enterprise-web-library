using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
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

		ElementClassSet ButtonStyle.GetClasses() {
			var styleClass = ActionComponentCssElementCreator.NormalButtonStyleClass;
			if( buttonSize == ButtonSize.ShrinkWrap )
				styleClass = ActionComponentCssElementCreator.ShrinkWrapButtonStyleClass;
			else if( buttonSize == ButtonSize.Large )
				styleClass = ActionComponentCssElementCreator.LargeButtonStyleClass;
			return ActionComponentCssElementCreator.AllStylesClass.Add( styleClass );
		}

		IEnumerable<FlowComponentOrNode> ButtonStyle.GetChildren() {
			return ActionComponentIcon.GetIconAndTextComponents( icon, text );
		}

		string ButtonStyle.GetJsInitStatements( string id ) {
			return "";
		}
	}

	/// <summary>
	/// A size for a standard-style button.
	/// </summary>
	public enum ButtonSize {
		/// <summary>
		/// A button that is shrink-wrapped to be almost no larger than an anchor tag.
		/// </summary>
		ShrinkWrap,

		/// <summary>
		/// A typical-sized button.
		/// </summary>
		Normal,

		/// <summary>
		/// A very large button that dominates the screen.
		/// </summary>
		Large
	}
}