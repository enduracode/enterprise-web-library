using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Displays a clickable link to the user that will display a ModalWindow.
	/// NOTE: Think about renaming this. At the least, the term Link should be replaced with Button to be consistent with other action controls.
	/// </summary>
	public class LaunchWindowLink: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ActionControl {
		private readonly ModalWindow windowToLaunch;

		/// <summary>
		/// Sets the display style of this button. Do not set this to null.
		/// Choices are: BoxActionControlStyle, ImageActionControlStyle, CustomActionControlStyle, ButtonActionControlStyle, and TextActionControlStyle (default).
		/// </summary>
		public ActionControlStyle ActionControlStyle { private get; set; }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		// GMS NOTE: Why would we have a constructor that doesn't take a control style if it seems to be absolutely required?
		/// <summary>
		/// Creates a new LaunchWindowLink with the given ModalWindow to launch when clicked.
		/// You must set the ActionControlStyle after creating this object.
		/// </summary>
		public LaunchWindowLink( ModalWindow windowToLaunch ) {
			ActionControlStyle = new TextActionControlStyle( "" );

			if( windowToLaunch == null )
				throw new ApplicationException( "WindowToLaunch must be set on LaunchWindowLink" );
			this.windowToLaunch = windowToLaunch;
		}

		/// <summary>
		/// Checks that WindowToLaunch has been set and applies the attributes for this LaunchWindowLink.
		/// </summary>
		void ControlTreeDataLoader.LoadData() {
			if( TagKey == HtmlTextWriterTag.Button )
				PostBackButton.AddButtonAttributes( this );
			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			ActionControlStyle.SetUpControl( this, "", Unit.Empty, Unit.Empty, width => { } );

			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			this.AddJavaScriptEventScript( JsWritingMethods.onclick, windowToLaunch.GetJsOpenStatement() + " return false" );
			return ActionControlStyle.GetJsInitStatements( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return PostBackButton.GetTagKey( ActionControlStyle ); } }
	}
}