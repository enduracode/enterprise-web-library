using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls as text.
	/// </summary>
	public class TextActionControlStyle: ActionControlStyle {
		private readonly ActionControlIcon icon;

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
		public TextActionControlStyle( string text, ActionControlIcon icon = null ) {
			this.icon = icon;
			Text = text;
		}

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter ) {
			control.CssClass = control.CssClass.ConcatenateWithSpace( CssElementCreator.AllStylesClass + " " + CssElementCreator.TextStyleClass );
			return control.AddControlsReturnThis( ActionControlIcon.GetIconAndTextControls( icon, Text.Any() ? Text : defaultText ) );
		}

		string ActionControlStyle.GetJsInitStatements( WebControl controlForGetClientUrl ) {
			return "";
		}
	}
}