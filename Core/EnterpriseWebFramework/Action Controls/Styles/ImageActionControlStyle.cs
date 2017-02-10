using System.Web.UI.WebControls;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls as images.
	/// </summary>
	public class ImageActionControlStyle: ActionControlStyle {
		private readonly ResourceInfo imageInfo;
		private readonly ResourceInfo rolloverImageInfo;

		/// <summary>
		/// Creates an image action control style with the specified image. Do not pass null.
		/// </summary>
		public ImageActionControlStyle( ResourceInfo imageInfo, ResourceInfo rolloverImageInfo = null ) {
			this.imageInfo = imageInfo;
			this.rolloverImageInfo = rolloverImageInfo;
		}

		/// <summary>
		/// Alternate text to be placed in the alt tag of the image.
		/// </summary>
		public string AlternateText { get; set; }

		/// <summary>
		/// If true, sets the size to the width available.
		/// </summary>
		public bool SizesToAvailableWidth { get; set; }

		string ActionControlStyle.Text => "";

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText ) {
			control.CssClass =
				control.CssClass.ConcatenateWithSpace(
					ActionComponentCssElementCreator.AllStylesClass.ClassName + " " + ActionComponentCssElementCreator.ImageStyleClass.ClassName );

			if( rolloverImageInfo != null && rolloverImageInfo.GetUrl() != imageInfo.GetUrl() ) {
				control.AddJavaScriptEventScript(
					JavaScriptWriting.JsWritingMethods.onmouseover,
					"$( this ).children().attr( 'src', '{0}' )".FormatWith( rolloverImageInfo.GetUrl() ) );
				control.AddJavaScriptEventScript(
					JavaScriptWriting.JsWritingMethods.onmouseout,
					"$( this ).children().attr( 'src', '{0}' )".FormatWith( imageInfo.GetUrl() ) );
			}

			control.AddControlsReturnThis(
				new EwfImage(
					new ImageSetup(
						AlternateText,
						sizesToAvailableWidth: SizesToAvailableWidth,
						classes: SizesToAvailableWidth ? new ElementClass( "ewfBlockContainer" ) : ElementClassSet.Empty ),
					imageInfo ).ToCollection().GetControls() );

			return null;
		}

		string ActionControlStyle.GetJsInitStatements() {
			if( rolloverImageInfo == null || rolloverImageInfo.GetUrl() == imageInfo.GetUrl() )
				return "";
			return "new Image().src = '" + rolloverImageInfo.GetUrl() + "';";
		}
	}
}