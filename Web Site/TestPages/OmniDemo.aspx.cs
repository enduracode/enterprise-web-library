using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class OmniDemo: EwfPage {
		protected override void loadData() {
			var omni = FormItemBlock.CreateFormItemList( numberOfColumns: 7 );
			var complexLabel = new Panel();
			complexLabel.AddControlsReturnThis( "Model number".GetLiteralControl() );
			complexLabel.AddControlsReturnThis(
				new LaunchWindowLink( new ModalWindow( "More information...".GetLiteralControl() ) ) { ActionControlStyle = new TextActionControlStyle( " (popup)" ) } );
			omni.AddFormItems(
				FormItem.Create( complexLabel, new EwfTextBox( "" ), cellSpan: 2 ),
				FormItem.Create( "Normal price", new EwfLabel() ),
				FormItem.Create( "Actual price", new EwfTextBox( "" ) ),
				FormItem.Create( "Quantity", new EwfTextBox( "" ) ),
				FormItem.Create( "Inventory", new EwfLabel() ),
				FormItem.Create( "Bill Number", new EwfLabel() ) );
			ph.AddControlsReturnThis( omni );
		}

		public override bool IsAutoDataUpdater { get { return true; } }
	}
}