using System.Linq;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders an action control as a button with rounded corners.
	/// </summary>
	public class ButtonActionControlStyle: ActionControlStyle {
		/// <summary>
		/// The type of button to show.
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

		private readonly ButtonSize buttonSize;
		private readonly ActionControlIcon icon;
		private readonly string text;

		/// <summary>
		/// Creates a button action control style.
		/// </summary>
		/// <param name="text">Do not pass null.</param>
		/// <param name="buttonSize"></param>
		/// <param name="icon">The icon.</param>
		public ButtonActionControlStyle( string text, ButtonSize buttonSize = ButtonSize.Normal, ActionControlIcon icon = null ) {
			this.buttonSize = buttonSize;
			this.icon = icon;
			this.text = text;
		}

		string ActionControlStyle.Text => text;

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText ) {
			var cssElement = ActionComponentCssElementCreator.NormalButtonStyleClass.ClassName;
			if( buttonSize == ButtonSize.ShrinkWrap )
				cssElement = ActionComponentCssElementCreator.ShrinkWrapButtonStyleClass.ClassName;
			else if( buttonSize == ButtonSize.Large )
				cssElement = ActionComponentCssElementCreator.LargeButtonStyleClass.ClassName;
			control.CssClass = control.CssClass.ConcatenateWithSpace( ActionComponentCssElementCreator.AllStylesClass.ClassName + " " + cssElement );

			return control.AddControlsReturnThis( ActionControlIcon.GetIconAndTextComponents( icon, text.Any() ? text : defaultText ).GetControls() );
		}

		string ActionControlStyle.GetJsInitStatements() {
			return "";
		}
	}
}