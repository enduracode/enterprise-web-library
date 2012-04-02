using System;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls as text.
	/// </summary>
	public class TextActionControlStyle: ActionControlStyle {
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
		/// Creates a text action control style. Do not pass null for text.
		/// </summary>
		public TextActionControlStyle( string text ) {
			Text = text;
		}

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter ) {
			control.CssClass = control.CssClass.ConcatenateWithSpace( CssElementCreator.AllStylesClass + " " + CssElementCreator.TextStyleClass );
			return control.AddControlsReturnThis( ( Text.Length > 0 ? Text : defaultText ).GetLiteralControl() );
		}

		string ActionControlStyle.GetJsInitStatements( WebControl controlForGetClientUrl ) {
			return "";
		}
	}
}