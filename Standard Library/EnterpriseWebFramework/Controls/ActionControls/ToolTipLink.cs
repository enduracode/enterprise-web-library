using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.JavaScriptWriting;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Provides a clickable link that displays a special tool tip.
	/// </summary>
	public class ToolTipLink: WebControl, ControlTreeDataLoader, ControlWithJsInitLogic, ActionControl {
		private Control toolTipControl;

		/// <summary>
		/// Sets the display style of this button. Do not set this to null.
		/// Choices are: BoxActionControlStyle, ImageActionControlStyle, CustomActionControlStyle, ButtonActionControlStyle, and TextActionControlStyle (default).
		/// </summary>
		public ActionControlStyle ActionControlStyle { private get; set; }

		/// <summary>
		/// Optional title to be displayed in the the tool tip.
		/// </summary>
		public String ToolTipTitle { get; set; }

		/// <summary>
		/// Creates a tool tip link. Do not pass null for the tool tip control.
		/// NOTE: In IE9, action controls may not display inside the tool tip unless they are inside a table.
		/// NOTE: In IE7-9, EWF links in the tool tip will not work, even if they are displayed.
		/// NOTE: In Firefox, EWF links inside a table in the tool tip will display but will not work.
		/// </summary>
		public ToolTipLink( Control toolTipControl ) {
			ActionControlStyle = new TextActionControlStyle( "" );
			this.toolTipControl = toolTipControl;
		}

		/// <summary>
		/// Do not call this, and do not place tool tip links in markup.
		/// </summary>
		// NOTE: Remove this when we have eliminated tool tip links from markup.
		public ToolTipLink(): this( null ) {}

		/// <summary>
		/// Do not call this. It only exists to support tool tip links that were placed in markup.
		/// </summary>
		// NOTE: When we remove this property, make the toolTipControl field readonly.
		public Control ToolTipControl { set { toolTipControl = value; } }

		/// <summary>
		/// Do not use this property; it will be deleted.
		/// </summary>
		// NOTE: Remove this when we have eliminated tool tip links from markup.
		public string Text { private get; set; }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			if( toolTipControl == null )
				throw new ApplicationException( "ToolTipControl must be set on ToolTipLink" );

			if( TagKey == HtmlTextWriterTag.Button )
				PostBackButton.AddButtonAttributes( this );

			// NOTE: When this control is rendered as an anchor, the presence of an onclick attribute is necessary for it to be selected properly by our action
			// control CSS elements. This hack would not be necessary if Telerik used the onclick attribute to open the tool tip.
			this.AddJavaScriptEventScript( JsWritingMethods.onclick, "" );

			CssClass = CssClass.ConcatenateWithSpace( "ewfClickable" );
			ActionControlStyle.SetUpControl( this, Text, Unit.Empty, Unit.Empty, width => { } );

			new ToolTip( toolTipControl, this, title: ToolTipTitle ?? "", sticky: true );
		}

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return ActionControlStyle.GetJsInitStatements( this );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return PostBackButton.GetTagKey( ActionControlStyle ); } }
	}
}