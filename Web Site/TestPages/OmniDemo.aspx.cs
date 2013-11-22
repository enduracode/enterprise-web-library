using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class OmniDemo: EwfPage {
		protected override void loadData() {
			var omni = FormItemBlock.CreateFormItemList( numberOfColumns: 7 );
			var complexLabel = new Panel();
			complexLabel.AddControlsReturnThis( "Model number".GetLiteralControl() );
			complexLabel.AddControlsReturnThis( new LaunchWindowLink( new ModalWindow( "More information...".GetLiteralControl() ) ) { ActionControlStyle = new TextActionControlStyle( " (popup)" ) } );
			omni.AddFormItems( FormItem.Create( complexLabel, new EwfTextBox( "" ), cellSpan : 2 ),
												 FormItem.Create( "Normal price".GetLiteralControl(), new EwfLabel() ),
												 FormItem.Create( "Actual price".GetLiteralControl(), new EwfTextBox( "" ) ),
												 FormItem.Create( "Quantity".GetLiteralControl(), new EwfTextBox( "" ) ),
												 FormItem.Create( "Inventory".GetLiteralControl(), new EwfLabel() ),
												 FormItem.Create( "Bill Number".GetLiteralControl(), new EwfLabel() ) );
			ph.AddControlsReturnThis( omni );
		}
	}
}