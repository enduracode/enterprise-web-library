using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An element that hovers over the page in the center of the browser window and prevents interaction with the page. There can only be one modal window
	/// visible at a time. This control is intended to be used with LaunchWindowLink.
	/// </summary>
	// NOTE: Prohibit form controls from existing in a modal window.
	// NOTE: Prevent a modal window from opening another modal window, probably by prohibiting LaunchWindowLink controls from existing in a modal window.
	// NOTE: Answer these questions and make the necessary implementation changes:
	//       1. Should modal windows have a gigantic close button?
	//       2. Should clicking outside of a modal window close it? What if it's a modal confirmation?
	//       3. Should modal windows be able to contain post back buttons that require [modal] confirmation?
	//       4. Should modal windows be able to contain action controls that don't open another modal, such as EwfLink?
	public class ModalWindow: EtherealControl {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfModal";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "ModalSectionContainer", "div." + CssClass ) };
			}
		}

		private readonly WebControl control;
		private readonly bool open;

		/// <summary>
		/// Creates a modal window.
		/// </summary>
		public ModalWindow( Control content, string title = "", bool open = false ): this( content, title: title, postBack: null, open: open ) {}

		internal ModalWindow( Control content, string title = "", PostBack postBack = null, bool open = false ) {
			control = new Block( new Section( title, content.ToSingleElementArray().Concat( getButtonTable( postBack ) ) ) ) { CssClass = CssElementCreator.CssClass };
			this.open = open;

			EwfPage.Instance.AddEtherealControl( this );
		}

		private IEnumerable<Control> getButtonTable( PostBack postBack ) {
			if( postBack == null )
				return new Control[ 0 ];

			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly );
			table.AddItem(
				new EwfTableItem(
					new ControlLine(
						new CustomButton( () => "$.modal.close()" ) { ActionControlStyle = new ButtonActionControlStyle( "Cancel" ) },
						new PostBackButton( postBack, new ButtonActionControlStyle( "Continue" ), usesSubmitBehavior: false ) ).ToCell(
							new TableCellSetup( textAlignment: TextAlignment.Right ) ) ) );
			return table.ToSingleElementArray();
		}

		WebControl EtherealControl.Control { get { return control; } }

		string EtherealControl.GetJsInitStatements() {
			return open ? GetJsOpenStatement() : "";
		}

		internal string GetJsOpenStatement() {
			return "$( '#" + control.ClientID + "' ).modal( {} );";
		}
	}
}