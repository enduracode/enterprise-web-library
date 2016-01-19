using System;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls as rectangular boxes.
	/// </summary>
	public class BoxActionControlStyle: ActionControlStyle {
		private readonly string leftImageUrl;
		private readonly int? leftImageWidth;
		private readonly string rightImageUrl;
		private readonly int? rightImageWidth;
		private readonly string backgroundImageUrl;
		private readonly int? imageHeight;

		/// <summary>
		/// Gets the text displayed in the box.
		/// </summary>
		public string Text { get; private set; }

		/// <summary>
		/// Creates a box action control style. Do not pass null for any string parameters.
		/// </summary>
		public BoxActionControlStyle(
			string text, string leftImageUrl, int leftImageWidth, string rightImageUrl, int rightImageWidth, string backgroundImageUrl, int imageHeight ) {
			Text = text;
			this.leftImageUrl = leftImageUrl;
			this.leftImageWidth = leftImageWidth;
			this.rightImageUrl = rightImageUrl;
			this.rightImageWidth = rightImageWidth;
			this.backgroundImageUrl = backgroundImageUrl;
			this.imageHeight = imageHeight;
		}

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter ) {
			widthSetter( width );
			control.CssClass = control.CssClass.ConcatenateWithSpace( "ewfBlockContainer " + CssElementCreator.AllStylesClass + " " + CssElementCreator.BoxStyleClass );

			var span = new HtmlGenericControl( "span" );
			span.Attributes.Add( "class", CssElementCreator.BoxStyleSideAndBackgroundImageBoxClass );
			span.Style.Add( "margin-left", leftImageWidth.Value + "px" );
			span.Style.Add( "margin-right", rightImageWidth.Value + "px" );
			span.Style.Add( "background-image", control.GetClientUrl( backgroundImageUrl ) );

			var leftImage = new EwfImage( new ExternalResourceInfo( leftImageUrl ) ) { CssClass = "left" };
			leftImage.Style.Add( "margin-left", -leftImageWidth + "px" );

			var rightImage = new EwfImage( new ExternalResourceInfo( rightImageUrl ) ) { CssClass = "right" };
			rightImage.Style.Add( "margin-right", -rightImageWidth + "px" );

			var label = new Label { Text = Text.Length > 0 ? Text : defaultText, CssClass = CssElementCreator.BoxStyleTextClass };
			label.Style.Add( "line-height", imageHeight + "px" );

			control.Controls.Add( span.AddControlsReturnThis( leftImage, rightImage, label ) );
			return label;
		}

		string ActionControlStyle.GetJsInitStatements( WebControl controlForGetClientUrl ) {
			return "";
		}
	}
}