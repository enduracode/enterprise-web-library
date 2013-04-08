using System;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders an action control as a button with rounded corners.
	/// </summary>
	public class ButtonActionControlStyle: ActionControlStyle {
		[ Obsolete( "Guaranteed through 30 June 2013." ) ]
		public string Text { get; set; }

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

		/// <summary>
		/// Creates a button action control style. Do not pass null for text.
		/// </summary>
		public ButtonActionControlStyle( string text, ButtonSize buttonSize = ButtonSize.Normal ) {
			Text = text;
			this.buttonSize = buttonSize;
		}

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter ) {
			widthSetter( width );

			var cssElement = CssElementCreator.NormalButtonStyleClass;
			if( buttonSize == ButtonSize.ShrinkWrap )
				cssElement = CssElementCreator.ShrinkWrapButtonStyleClass;
			else if( buttonSize == ButtonSize.Large )
				cssElement = CssElementCreator.LargeButtonStyleClass;
			control.CssClass = control.CssClass.ConcatenateWithSpace( CssElementCreator.AllStylesClass + " " + cssElement );

			return control.AddControlsReturnThis( ( Text.Length > 0 ? Text : defaultText ).GetLiteralControl() );
		}

		string ActionControlStyle.GetJsInitStatements( WebControl controlForGetClientUrl ) {
			return "";
		}
	}
}