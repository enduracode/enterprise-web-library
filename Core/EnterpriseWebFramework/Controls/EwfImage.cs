using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An image.
	/// </summary>
	public class EwfImage: WebControl, ControlTreeDataLoader {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfImage";

			/// <summary>
			/// EWL use only.
			/// </summary>
			public static readonly string[] Selectors = { "img." + CssClass, "div." + CssClass };

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "Image", Selectors.First(), Selectors.Skip( 1 ).ToArray() ) };
			}
		}

		private readonly ResourceInfo imageInfo;

		/// <summary>
		/// Alternate text to be placed in the alt tag of the image.
		/// </summary>
		public string AlternateText { get; set; }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		/// <summary>
		/// Gets or sets whether the image sizes itself to fit all available width.
		/// </summary>
		public bool SizesToAvailableWidth { get; set; }

		internal bool IsAutoSizer { private get; set; }

		/// <summary>
		/// Creates an image.
		/// </summary>
		/// <param name="imageInfo">Do not pass null.</param>
		public EwfImage( ResourceInfo imageInfo ) {
			this.imageInfo = imageInfo;
		}

		void ControlTreeDataLoader.LoadData() {
			if( !SizesToAvailableWidth ) {
				Attributes.Add( "src", this.GetClientUrl( imageInfo.GetUrl( true, true, false ) ) );
				Attributes.Add( "alt", AlternateText ?? "" );
			}
			else
				Controls.Add( new EwfImage( imageInfo ) { IsAutoSizer = true, AlternateText = AlternateText } );
			CssClass = CssClass.ConcatenateWithSpace( IsAutoSizer ? "ewfAutoSizer" : CssElementCreator.CssClass );

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		/// <summary>
		/// Returns the img tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return SizesToAvailableWidth ? HtmlTextWriterTag.Div : HtmlTextWriterTag.Img; } }
	}
}