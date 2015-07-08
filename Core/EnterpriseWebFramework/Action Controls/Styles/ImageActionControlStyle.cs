using System;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls as images.
	/// </summary>
	public class ImageActionControlStyle: ActionControlStyle {
		private readonly ResourceInfo imageInfo;
		private ResourceInfo rolloverImageInfo;

		/// <summary>
		/// Creates an image action control style with the specified image. Do not pass null.
		/// </summary>
		public ImageActionControlStyle( ResourceInfo imageInfo, ResourceInfo rolloverImageInfo = null ) {
			this.imageInfo = imageInfo;
			this.rolloverImageInfo = rolloverImageInfo;
		}

		[ Obsolete( "Guaranteed through 30 April 2015. Please use the other constructor." ) ]
		public ImageActionControlStyle( string imageUrl ): this( new ExternalResourceInfo( imageUrl ) ) {}

		/// <summary>
		/// Alternate text to be placed in the alt tag of the image.
		/// </summary>
		public string AlternateText { get; set; }

		[ Obsolete( "Guaranteed through 30 April 2015. Please use the constructor." ) ]
		public string RolloverImageUrl {
			get { return rolloverImageInfo.GetUrl( false, false, false ); }
			set { rolloverImageInfo = new ExternalResourceInfo( value ); }
		}

		/// <summary>
		/// If true, sets the size to the width available.
		/// </summary>
		public bool SizesToAvailableWidth { get; set; }

		string ActionControlStyle.Text { get { return ""; } }

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter ) {
			control.CssClass = control.CssClass.ConcatenateWithSpace( CssElementCreator.AllStylesClass + " " + CssElementCreator.ImageStyleClass );

			var image = new EwfImage( imageInfo ) { AlternateText = AlternateText };
			if( SizesToAvailableWidth ) {
				control.CssClass = control.CssClass.ConcatenateWithSpace( "ewfBlockContainer" );
				image.IsAutoSizer = true;
			}
			else {
				image.Width = width;
				image.Height = height;
			}

			if( rolloverImageInfo != null && getClientUrl( control, rolloverImageInfo ) != getClientUrl( control, imageInfo ) ) {
				// NOTE: These events should be handled at the PostBackButton level instead of the image element level so rolling over the PostBackButton border works.
				image.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onmouseover, "src='" + getClientUrl( control, rolloverImageInfo ) + "'" );
				image.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onmouseout, "src='" + getClientUrl( control, imageInfo ) + "'" );
			}

			control.Controls.Add( image );
			return null;
		}

		string ActionControlStyle.GetJsInitStatements( WebControl controlForGetClientUrl ) {
			if( rolloverImageInfo == null || getClientUrl( controlForGetClientUrl, rolloverImageInfo ) == getClientUrl( controlForGetClientUrl, imageInfo ) )
				return "";
			return "new Image().src = '" + getClientUrl( controlForGetClientUrl, rolloverImageInfo ) + "';";
		}

		private string getClientUrl( WebControl control, ResourceInfo imageInfo ) {
			return control.GetClientUrl( imageInfo.GetUrl( true, true, false ) );
		}
	}
}