using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class OmniDemo: EwfPage {
		protected override void loadData() {
			var omni = FormItemBlock.CreateFormItemList( numberOfColumns: 7 );
			omni.AddFormItems( FormItem.Create( "Model number", new EwfTextBox( "" ), cellSpan: 2 ),
			                   FormItem.Create( "Normal price", new EwfLabel() ),
			                   FormItem.Create( "Actual price", new EwfTextBox( "" ) ),
			                   FormItem.Create( "Quantity", new EwfTextBox( "" ) ),
			                   FormItem.Create( "Inventory", new EwfLabel() ),
			                   FormItem.Create( "Bill Number", new EwfLabel() ) );
			ph.AddControlsReturnThis( omni );
		}
	}
}