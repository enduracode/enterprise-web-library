using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An element that hovers over the page in the center of the browser window and prevents interaction with the page. There can only be one modal window
	/// visible at a time. This control is intended to be used with LaunchWindowLink.
	/// </summary>
	// NOTE: Prohibit form controls from existing in a modal window.
	// NOTE: Prevent a modal window from opening another modal window, probably be prohibiting LaunchWindowLink controls from existing in a modal window.
	// NOTE: Answer these questions and make the necessary implementation changes:
	//       1. Should modal windows have a gigantic close button?
	//       2. Should clicking outside of a modal window close it? What if it's a modal confirmation?
	//       3. Should modal windows be locked in the center of the browser window?
	//       4. Should modal windows gray out the rest of the page?
	//       5. Should modal windows be able to contain post back buttons that require [modal] confirmation?
	//       6. Should modal windows be able to contain action controls that don't open another modal, such as EwfLink?
	//       7. Can we reimplement info/warning status message boxes to use modal windows? For info messages, we'd need a way to make a modal window close
	//          automatically on a timer.
	public class ModalWindow: EtherealControl {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfModalConfirmation";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				// NOTE: This element will select all modal windows, not just confirmations. This is not a big deal since we probably only want to use modal windows for
				// confirmations anyway.
				// NOTE: This element only selects the content area of the confirmation block right now.
				return new[] { new CssElement( "ModalConfirmationBlock", "div." + CssClass ) };
			}
		}

		private readonly WebControl control;
		private readonly string title;
		private readonly PostBack postBack;
		private readonly bool open;

		/// <summary>
		/// Creates a modal window.
		/// </summary>
		public ModalWindow( Control content, string title = "", bool open = false ): this( content, title: title, postBack: null, open: open ) {}

		internal ModalWindow( Control content, string title = "", PostBack postBack = null, bool open = false ) {
			control = new Block( content ) { CssClass = CssElementCreator.CssClass };
			this.title = title;
			this.postBack = postBack;
			this.open = open;

			EwfPage.Instance.AddEtherealControl( this );
		}

		WebControl EtherealControl.Control { get { return control; } }

		string EtherealControl.GetJsInitStatements() {
			return "$( '#" + control.ClientID + "' ).dialog( { autoOpen: " + open.ToString().ToLower() + ", width: 600, hide: 'fade', show: 'fade', " +
			       ( postBack != null
				         ? "buttons: { 'Cancel': function() { $( this ).dialog( 'close' ) }, 'Continue': function() { " + PostBackButton.GetPostBackScript( postBack ) +
				           "; } }, "
				         : "" ) + "draggable: false, modal: true, resizable: false" + ( title.Any() ? ", title: '" + title + "'" : "" ) + " } );";
		}
	}
}