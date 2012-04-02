using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls as images.
	/// </summary>
	public class ImageActionControlStyle: ActionControlStyle {
		private readonly string imageUrl;

		/// <summary>
		/// Creates an image action control style with the specified image. Do not pass null.
		/// </summary>
		public ImageActionControlStyle( string imageUrl ) {
			this.imageUrl = imageUrl;
			RolloverImageUrl = "";
		}

		/// <summary>
		/// Alternate text to be placed in the alt tag of the image.
		/// </summary>
		public string AlternateText { get; set; }

		/// <summary>
		/// URL to the optional rollover image to display.
		/// </summary>
		public string RolloverImageUrl { get; set; }

		/// <summary>
		/// If true, sets the size to the width available.
		/// </summary>
		public bool SizesToAvailableWidth { get; set; }

		string ActionControlStyle.Text { get { return ""; } }

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter ) {
			control.CssClass = control.CssClass.ConcatenateWithSpace( CssElementCreator.AllStylesClass + " " + CssElementCreator.ImageStyleClass );

			var image = new EwfImage( imageUrl ) { AlternateText = AlternateText };
			if( SizesToAvailableWidth ) {
				control.CssClass = control.CssClass.ConcatenateWithSpace( "ewfBlockContainer" );
				image.IsAutoSizer = true;
			}
			else {
				// If the control is a button element, this makes it the proper width in IE7.
				control.Style.Add( "overflow", "visible" );

				image.Width = width;
				image.Height = height;
			}

			if( RolloverImageUrl.Length > 0 && RolloverImageUrl != imageUrl ) {
				// NOTE: These events should be handled at the PostBackButton level instead of the image element level so rolling over the PostBackButton border works.
				image.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onmouseover, "src='" + control.GetClientUrl( RolloverImageUrl ) + "'" );
				image.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onmouseout, "src='" + control.GetClientUrl( imageUrl ) + "'" );
			}

			control.Controls.Add( image );
			return null;
		}

		string ActionControlStyle.GetJsInitStatements( WebControl controlForGetClientUrl ) {
			if( !RolloverImageUrl.Any() || RolloverImageUrl == imageUrl )
				return "";
			return "new Image().src = '" + controlForGetClientUrl.GetClientUrl( RolloverImageUrl ) + "';";
		}
	}
}