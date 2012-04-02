using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A link that intelligently behaves like either a HyperLink or a LinkButton depending on whether its page needs to be saved.
	/// </summary>
	public class EwfLink: WebControl, ControlTreeDataLoader, IPostBackEventHandler, ControlWithJsInitLogic, ActionControl {
		private PageInfo navigatePageInfo;
		private string text = "";

		/// <summary>
		/// Gets or sets the display style of this button. Do not set this to null.
		/// Choices are: TextActionControlStyle (default), ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.
		/// </summary>
		public ActionControlStyle ActionControlStyle { get; set; }

		private bool navigatesInNewWindow;
		private PopUpWindowSettings popUpWindowSettings;
		private bool navigatesInOpeningWindow;

		private Unit width = Unit.Empty;
		private Unit height = Unit.Empty;

		/// <summary>
		/// NOTE: Only exists to support pages that have not yet converted to the immutable static method constructors.
		/// </summary>
		public EwfLink( PageInfo navigatePageInfo ) {
			NavigatePageInfo = navigatePageInfo;
			ActionControlStyle = new TextActionControlStyle( "" );
		}

		/// <summary>
		/// Creates a link.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		public static EwfLink Create( PageInfo navigatePageInfo, ActionControlStyle actionControlStyle, string toolTipText = null, Control toolTipControl = null ) {
			return new EwfLink( navigatePageInfo ) { ActionControlStyle = actionControlStyle, ToolTip = toolTipText, ToolTipControl = toolTipControl };
		}

		/// <summary>
		/// Creates a link that will open a new window (or tab) when clicked.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		/// <returns></returns>
		public static EwfLink CreateForNavigationInNewWindow( PageInfo navigatePageInfo, ActionControlStyle actionControlStyle, string toolTipText = null,
		                                                      Control toolTipControl = null ) {
			return new EwfLink( navigatePageInfo )
			       	{ ActionControlStyle = actionControlStyle, NavigatesInNewWindow = true, ToolTip = toolTipText, ToolTipControl = toolTipControl };
		}

		/// <summary>
		/// Creates a link that will open a new pop up window when clicked.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="popUpWindowSettings"></param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		public static EwfLink CreateForNavigationInPopUpWindow( PageInfo navigatePageInfo, ActionControlStyle actionControlStyle,
		                                                        PopUpWindowSettings popUpWindowSettings, string toolTipText = null, Control toolTipControl = null ) {
			var link = new EwfLink( navigatePageInfo ) { ActionControlStyle = actionControlStyle, ToolTip = toolTipText, ToolTipControl = toolTipControl };
			link.NavigateInPopUpWindow( popUpWindowSettings );
			return link;
		}

		/// <summary>
		/// Creates a link that will close the current (pop up) window and navigate in the opening window.
		/// </summary>
		/// <param name="navigatePageInfo">Where to navigate. Specify null if you don't want the link to do anything.</param>
		/// <param name="actionControlStyle">Choices are: TextActionControlStyle, ImageActionControlStyle, ButtonActionControlStyle, CustomActionControlStyle, and BoxActionControlStyle.</param>
		/// <param name="toolTipText">EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.</param>
		/// <param name="toolTipControl">Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.</param>
		public static EwfLink CreateForNavigationInOpeningWindow( PageInfo navigatePageInfo, ActionControlStyle actionControlStyle, string toolTipText = null,
		                                                          Control toolTipControl = null ) {
			return new EwfLink( navigatePageInfo )
			       	{ ActionControlStyle = actionControlStyle, NavigatesInOpeningWindow = true, ToolTip = toolTipText, ToolTipControl = toolTipControl };
		}

		// NOTE: All action control Text properties should be axed since they're incompatible with some action control styles. Use properties on the styles instead.
		/// <summary>
		/// Gets or sets the text caption for this control. The text will be HTML-encoded before being rendered on the page. Do not pass null; if you do, it will be
		/// converted to the empty string.
		/// </summary>
		public string Text { get { return text; } set { text = value ?? ""; } }

		/// <summary>
		/// Gets or sets the page to link to when this control is clicked. Specify null if you don't want the link to do anything.
		/// NOTE: Do not use the setter; it will be deleted.
		/// </summary>
		public PageInfo NavigatePageInfo { get { return navigatePageInfo; } set { navigatePageInfo = value; } }


		/// <summary>
		/// NOTE: Do not use. Will be deleted.
		/// </summary>
		public bool NavigatesInNewWindow {
			set {
				navigatesInNewWindow = value;
				if( value ) {
					popUpWindowSettings = null;
					navigatesInOpeningWindow = false;
				}
			}
		}

		/// <summary>
		/// NOTE: Do not use. Will be deleted.
		/// </summary>
		public void NavigateInPopUpWindow( PopUpWindowSettings popUpWindowSettings ) {
			this.popUpWindowSettings = popUpWindowSettings;
			if( popUpWindowSettings != null ) {
				navigatesInNewWindow = false;
				navigatesInOpeningWindow = false;
			}
		}

		/// <summary>
		/// NOTE: Do not use. Will be deleted.
		/// </summary>
		public bool NavigatesInOpeningWindow {
			set {
				navigatesInOpeningWindow = value;
				if( value ) {
					navigatesInNewWindow = false;
					popUpWindowSettings = null;
				}
			}
		}

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// NOTE: Do not use the setter; it will be deleted.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// NOTE: Do not use the setter; it will be deleted.
		/// </summary>
		public Control ToolTipControl { get; set; }

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
			return navigatePageInfo == null || navigatePageInfo.UserCanAccessPageAndAllControls;
		}

		/// <summary>
		/// If the page is an AutoDataModifier we make a LinkButton, which forces a postback and allows data to be modified and also gives us the opportunity to
		/// not navigate if the data modification failed. The reason we only use linkbuttons on AutoDataModifiers is that normal DataModifiers will only be
		/// modifying data if another button (therefore, not this one) is pressed. The only case where this could possibly end up mattering is if there is a control
		/// with no autopostback that has a change event that calls ValidateFormsValuesAndModifyData. We cannot think of a reasonable case where this would happen.
		/// </summary>
		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			var finalNavigateUrl = "";
			if( navigatePageInfo != null )
				finalNavigateUrl = navigatePageInfo.GetUrl();
			if( finalNavigateUrl.Length > 0 )
				Attributes.Add( "href", this.GetClientUrl( finalNavigateUrl ) );

			if( isPostBackButton && finalNavigateUrl.Length > 0 )
				this.AddJavaScriptEventScript( JsWritingMethods.onclick, PostBackButton.GetPostBackScript( this, true ) );
			if( navigatesInNewWindow )
				Attributes.Add( "target", "_blank" );
			if( popUpWindowSettings != null && finalNavigateUrl.Length > 0 )
				this.AddJavaScriptEventScript( JsWritingMethods.onclick,
				                               JsWritingMethods.GetPopUpWindowScript( finalNavigateUrl, this, popUpWindowSettings ) + " return false" );
			if( navigatesInOpeningWindow ) {
				var openingWindowNavigationScript = finalNavigateUrl.Length > 0 ? "opener.document.location = '" + this.GetClientUrl( finalNavigateUrl ) + "'; " : "";
				this.AddJavaScriptEventScript( JsWritingMethods.onclick, openingWindowNavigationScript + "window.close(); return false" );
			}

			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			ActionControlStyle.SetUpControl( this, text.Length > 0 ? text : finalNavigateUrl, width, height, setWidth );

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		private void setWidth( Unit w ) {
			base.Width = w;
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return ActionControlStyle.GetJsInitStatements( this );
		}

		void IPostBackEventHandler.RaisePostBackEvent( string eventArgument ) {
			EwfPage.Instance.EhRedirect( navigatePageInfo );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.A; } }

		private bool isPostBackButton { get { return EwfPage.Instance is AutoDataModifier && !navigatesInNewWindow && popUpWindowSettings == null && !navigatesInOpeningWindow; } }
	}
}