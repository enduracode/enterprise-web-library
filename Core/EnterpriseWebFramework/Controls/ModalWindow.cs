using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An element that hovers over the page in the center of the browser window and prevents interaction with the page. There can only be one modal window
	/// visible at a time. This control is intended to be used with LaunchWindowLink.
	/// </summary>
	public class ModalWindow: EtherealControl {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfModal";

			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "ModalSectionContainer", "div." + CssClass ) };
			}
		}

		private readonly WebControl control;
		private readonly bool open;

		/// <summary>
		/// Creates a modal window.
		/// </summary>
		public ModalWindow( Control parent, Control content, string title = "", bool open = false ): this(
			parent,
			content,
			title: title,
			postBack: null,
			open: open ) {}

		internal ModalWindow( Control parent, Control content, string title = "", PostBack postBack = null, bool open = false ) {
			control = new Block( new LegacySection( title, content.ToCollection().Concat( getButtonTable( postBack ) ) ) ) { CssClass = CssElementCreator.CssClass };
			this.open = open;

			EwfPage.Instance.AddEtherealControl( parent, this );
		}

		private IEnumerable<Control> getButtonTable( PostBack postBack ) {
			if( postBack == null )
				return new Control[ 0 ];

			var table = EwfTable.Create( style: EwfTableStyle.StandardLayoutOnly );
			table.AddItem(
				new EwfTableItem(
					new LineList(
							new EwfButton( new StandardButtonStyle( "Cancel" ), behavior: new CustomButtonBehavior( () => "$.modal.close();" ) ).ToCollection()
								.ToComponentListItem()
								.ToLineListItemCollection()
								.Append(
									new EwfButton( new StandardButtonStyle( "Continue" ), behavior: new PostBackBehavior( postBack: postBack ) ).ToCollection()
										.ToComponentListItem() ) ).ToCollection()
						.ToCell( new TableCellSetup( textAlignment: TextAlignment.Right ) ) ) );
			return table.ToCollection().GetControls();
		}

		WebControl EtherealControl.Control => control;

		string EtherealControl.GetJsInitStatements() {
			return open ? GetJsOpenStatement() : "";
		}

		internal string GetJsOpenStatement() {
			return "$( '#" + control.ClientID + "' ).modal( {} );";
		}
	}
}