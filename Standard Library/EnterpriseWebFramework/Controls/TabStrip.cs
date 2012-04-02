using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Provides a way to show content only for the selected tab while hiding the content for other tabs.
	/// </summary>
	public class TabStrip: WebControl {
		// NOTE: Make the tab choice survive data modifications.
		// NOTE: It would be nice if this controls could be added to this in markup.
		// NOTE: Consider whether to support client-side hiding, server-side hiding, or both.
		// NOTE: Consider using Telerik's tab strip under the hood
		// NOTE: Consider using this to achieve the existing vertical tab view in the EWF UI. But, think carefully about pages vs controls. This probably won't happen because page infos are powerful (newness) and we don't want to rebuild them at the control level.

		private readonly EwfListControl tabs = new EwfListControl { Type = EwfListControl.ListControlType.HorizontalRadioButton };

		/// <summary>
		/// Creates a new Tab Strip.
		/// </summary>
		public TabStrip() {
			base.Controls.Add( tabs );
		}

		/// <summary>
		/// Adds a tab with the given label and its associated content.
		/// The given control is added to this control, and thus should not be a control from markup and should not have been added to the page in
		/// any way previous to this.
		/// </summary>
		public void AddTab( string label, WebControl content ) {
			Controls.Add( content );
			tabs.AddItem( label, label );
			tabs.AddDisplayLink( label, true, content );
		}

		/// <summary>
		/// Adds a tab with the given label and its associated content.
		/// The given control is added to this control, and thus should not be a control from markup and should not have been added to the page in
		/// any way previous to this.
		/// </summary>
		public void AddTab( string label, HtmlControl content ) {
			Controls.Add( content );
			tabs.AddItem( label, label );
			tabs.AddDisplayLink( label, true, content );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}