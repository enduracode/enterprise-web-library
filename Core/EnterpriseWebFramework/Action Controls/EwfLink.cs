using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.JavaScriptWriting;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A link that intelligently behaves like either a HyperLink or a LinkButton depending on whether its page needs to be saved.
	/// </summary>
	public class EwfLink: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ActionControl {
		internal static PostBack GetLinkPostBack( ResourceInfo destination ) {
			var id = PostBack.GetCompositeId( "ewfLink", destination.GetUrl() );
			return EwfPage.Instance.GetPostBack( id ) ?? PostBack.CreateFull( id: id, actionGetter: () => new PostBackAction( destination ) );
		}

		/// <summary>
		/// Creates a link.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		public static EwfLink Create( ResourceInfo navigatePageInfo, ActionControlStyle actionControlStyle, string toolTipText = null, Control toolTipControl = null ) {
			return new EwfLink( navigatePageInfo ) { ActionControlStyle = actionControlStyle, toolTip = toolTipText, toolTipControl = toolTipControl };
		}

		/// <summary>
		/// Creates a link that will open a new window (or tab) when clicked.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		/// <returns></returns>
		public static EwfLink CreateForNavigationInNewWindow(
			ResourceInfo navigatePageInfo, ActionControlStyle actionControlStyle, string toolTipText = null, Control toolTipControl = null ) {
			return new EwfLink( navigatePageInfo )
				{
					ActionControlStyle = actionControlStyle,
					navigatesInNewWindow = true,
					toolTip = toolTipText,
					toolTipControl = toolTipControl
				};
		}

		/// <summary>
		/// Creates a link that will open a new pop up window when clicked.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="popUpWindowSettings"></param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		public static EwfLink CreateForNavigationInPopUpWindow(
			ResourceInfo navigatePageInfo, ActionControlStyle actionControlStyle, PopUpWindowSettings popUpWindowSettings, string toolTipText = null,
			Control toolTipControl = null ) {
			return new EwfLink( navigatePageInfo )
				{
					ActionControlStyle = actionControlStyle,
					popUpWindowSettings = popUpWindowSettings,
					toolTip = toolTipText,
					toolTipControl = toolTipControl
				};
		}

		/// <summary>
		/// Creates a link that will close the current (pop up) window and navigate in the opening window.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		public static EwfLink CreateForNavigationInOpeningWindow(
			ResourceInfo navigatePageInfo, ActionControlStyle actionControlStyle, string toolTipText = null, Control toolTipControl = null ) {
			return new EwfLink( navigatePageInfo )
				{
					ActionControlStyle = actionControlStyle,
					navigatesInOpeningWindow = true,
					toolTip = toolTipText,
					toolTipControl = toolTipControl
				};
		}

		private readonly ResourceInfo destinationResourceInfo;

		private bool navigatesInNewWindow;
		private PopUpWindowSettings popUpWindowSettings;
		private bool navigatesInOpeningWindow;

		private Unit width = Unit.Empty;
		private Unit height = Unit.Empty;

		private string toolTip;
		private Control toolTipControl;

		/// <summary>
		/// Gets or sets the display style of this button. Do not set this to null.
		/// Choices are: TextActionControlStyle (default), ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.
		/// </summary>
		public ActionControlStyle ActionControlStyle { get; set; }

		/// <summary>
		/// Guaranteed to stay public through 28 February 2013.
		/// </summary>
		public EwfLink( ResourceInfo destinationResourceInfo ) {
			this.destinationResourceInfo = destinationResourceInfo;
			ActionControlStyle = new TextActionControlStyle( "" );
		}

		public ResourceInfo DestinationResourceInfo { get { return destinationResourceInfo; } }

		/// <summary>
		/// Gets or sets the width of this button. Doesn't work with the text action control style.
		/// </summary>
		public override Unit Width { get { return width; } set { width = value; } }

		/// <summary>
		/// Gets or sets the height of this button. Only works with the image action control style.
		/// </summary>
		public override Unit Height { get { return height; } set { height = value; } }

		/// <summary>
		/// Standard library use only.
		/// </summary>
		public bool UserCanNavigateToDestination() {
			return destinationResourceInfo == null || destinationResourceInfo.UserCanAccessResource;
		}

		void ControlTreeDataLoader.LoadData() {
			var url = "";
			if( destinationResourceInfo != null && !( destinationResourceInfo.AlternativeMode is DisabledResourceMode ) ) {
				url = destinationResourceInfo.GetUrl();
				Attributes.Add( "href", this.GetClientUrl( url ) );
			}

			if( isPostBackButton && url.Any() ) {
				var postBack = GetLinkPostBack( destinationResourceInfo );
				EwfPage.Instance.AddPostBack( postBack );
				PreRender += delegate { this.AddJavaScriptEventScript( JsWritingMethods.onclick, PostBackButton.GetPostBackScript( postBack ) ); };
			}
			if( navigatesInNewWindow )
				Attributes.Add( "target", "_blank" );
			if( popUpWindowSettings != null && url.Any() )
				this.AddJavaScriptEventScript( JsWritingMethods.onclick, JsWritingMethods.GetPopUpWindowScript( url, this, popUpWindowSettings ) + " return false" );
			if( navigatesInOpeningWindow && ( destinationResourceInfo == null || url.Any() ) ) {
				var openingWindowNavigationScript = destinationResourceInfo != null ? "opener.document.location = '" + this.GetClientUrl( url ) + "'; " : "";
				this.AddJavaScriptEventScript( JsWritingMethods.onclick, openingWindowNavigationScript + "window.close(); return false" );
			}

			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			if( destinationResourceInfo != null && destinationResourceInfo.AlternativeMode is NewContentResourceMode )
				CssClass = CssClass.ConcatenateWithSpace( CssElementCreator.NewContentClass );
			ActionControlStyle.SetUpControl( this, url, width, height, setWidth );

			if( destinationResourceInfo != null && destinationResourceInfo.AlternativeMode is DisabledResourceMode ) {
				var message = ( destinationResourceInfo.AlternativeMode as DisabledResourceMode ).Message;
				new ToolTip( EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( message.Any() ? message : Translation.ThePageYouRequestedIsDisabled ), this );
			}
			else if( toolTip != null || toolTipControl != null )
				new ToolTip( toolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( toolTip ), this );
		}

		private void setWidth( Unit w ) {
			base.Width = w;
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return ActionControlStyle.GetJsInitStatements( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.A; } }

		private bool isPostBackButton {
			get { return EwfPage.Instance.IsAutoDataUpdater && !navigatesInNewWindow && popUpWindowSettings == null && !navigatesInOpeningWindow; }
		}
	}
}