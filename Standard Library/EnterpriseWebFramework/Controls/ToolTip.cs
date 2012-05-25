using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An element that hovers over the page near the target control. There can only be one target control for a tool tip since having more than one target would
	/// require the tool tip to support being visible in multiple places at the same time. The WebControl content of the tool tip doesn't support this.
	/// </summary>
	internal class ToolTip: EtherealControl {
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

			EwfPage.Instance.AddEtherealControl( this );
		}

		WebControl EtherealControl.Control { get { return control; } }

		string EtherealControl.GetJsInitStatements() {
			// NOTE: Should we be setting a max width on the tool tip? We had 480px before.
			return "$( '#" + targetControl.ClientID + "' ).qtip( { content: { text: $( '#" + control.ClientID + "' ).remove()" +
			       ( title.Any() ? ", title: { text: '" + title + "' }" : "" ) + " }" +
			       ( sticky ? ", show: { delay: 0, when: { event: 'click' }, effect: { length: 0 } }, hide: { when: { event: 'unfocus' } }" : "" ) + " } );";
		}

		internal static Control GetToolTipTextControl( string toolTip ) {
			return toolTip.GetLiteralControl();
		}
	}
}