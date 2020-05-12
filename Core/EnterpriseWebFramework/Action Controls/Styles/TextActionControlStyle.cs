using System.Linq;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls as text.
	/// </summary>
	public class TextActionControlStyle: ActionControlStyle {
		private readonly ActionComponentIcon icon;

		/// <summary>
		/// Gets or sets the text. Do not set this to null.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Creates a text action control style.
		/// </summary>
		public TextActionControlStyle() {
			Text = "";
		}

		/// <summary>
		/// Creates a text action control style.
		/// </summary>
		/// <param name="text">Do not pass null.</param>
		/// <param name="icon">The icon.</param>
		public TextActionControlStyle( string text, ActionComponentIcon icon = null ) {
			this.icon = icon;
			Text = text;
		}

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText ) {
			control.CssClass = control.CssClass.ConcatenateWithSpace(
				ActionComponentCssElementCreator.AllStylesClass.ClassName + " " + ActionComponentCssElementCreator.HyperlinkStandardStyleClass.ClassName );
			return control.AddControlsReturnThis( ActionComponentIcon.GetIconAndTextComponents( icon, Text.Any() ? Text : defaultText ).GetControls() );
		}

		string ActionControlStyle.GetJsInitStatements() {
			return "";
		}
	}
}