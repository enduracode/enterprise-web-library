using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An element that hovers over the page near the target control. There can only be one target control for a tool tip since having more than one target would
	/// require the tool tip to support being visible in multiple places at the same time. The WebControl content of the tool tip doesn't support this.
	/// </summary>
	internal class ToolTip: EtherealControl {
		internal static Control GetToolTipTextControl( string toolTip ) {
			return toolTip.GetLiteralControl();
		}

		private readonly WebControl control;
		private readonly Control targetControl;
		private readonly string title;
		private readonly bool sticky;

		/// <summary>
		/// Creates a tool tip.
		/// </summary>
		internal ToolTip( Control content, Control targetControl, string title = "", bool sticky = false ) {
			control = new Block( content );
			this.targetControl = targetControl;
			this.title = title;
			this.sticky = sticky;

			EwfPage.Instance.AddEtherealControl( targetControl, this );
		}

		WebControl EtherealControl.Control { get { return control; } }

		string EtherealControl.GetJsInitStatements() {
			return "$( '#" + targetControl.ClientID + "' ).qtip( { content: { text: $( '#" + control.ClientID + "' )" +
			       ( title.Any() ? ", title: { text: '" + title + "' }" : "" ) + " }, position: { container: $( '#" + EwfPage.Instance.Form.ClientID +
			       "' ), viewport: $( window ) }" + ( sticky ? ", show: { event: 'click', delay: 0 }, hide: { event: 'unfocus' }" : "" ) +
			       ", style: { classes: 'qtip-bootstrap' } } );";
		}
	}
}