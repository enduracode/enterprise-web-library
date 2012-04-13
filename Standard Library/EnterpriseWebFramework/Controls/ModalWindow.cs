using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Displays a closeable modal window that cannot be ignored by the user.
	/// This can only be displayed to the user via a LaunchWindowLink.
	/// Never add a ModalWindow to the control tree yourself.
	/// </summary>
	// Sealed because of the virtual method call in the constructor
	public sealed class ModalWindow: EtherealControl {
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
		private readonly PostBackButton postBackButton;

		/// <summary>
		/// Creates a modal window.
		/// </summary>
		public ModalWindow( Control content, string title = "" ): this( content, title: title, postBackButton: null ) {}

		internal ModalWindow( Control content, string title = "", PostBackButton postBackButton = null ) {
			control = new Block( content ) { CssClass = CssElementCreator.CssClass };
			this.title = title;
			this.postBackButton = postBackButton;

			EwfPage.Instance.AddEtherealControl( this );
		}

		WebControl EtherealControl.Control { get { return control; } }

		string EtherealControl.GetJsInitStatements() {
			return "$( '#" + control.ClientID + "' ).dialog( { autoOpen: false, width: 600, hide: 'fade', show: 'fade'," +
			       ( postBackButton != null
			         	? "buttons: { 'Cancel': function() { $( this ).dialog( 'close' ) }, 'Continue': function() { " +
			         	  PostBackButton.GetPostBackScript( postBackButton, true ) + "; } }, "
			         	: "" ) + "draggable: false, modal: true, resizable: false" + ( title.Any() ? ", title: '" + title + "'" : "" ) + " } );";
		}
	}
}