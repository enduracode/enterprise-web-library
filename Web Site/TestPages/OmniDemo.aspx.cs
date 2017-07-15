using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class OmniDemo: EwfPage {
		protected override void loadData() {
			var omni = FormItemBlock.CreateFormItemList( numberOfColumns: 7 );
			var complexLabel = new Panel();
			complexLabel.AddControlsReturnThis( "Model number".ToComponents().GetControls() );
			complexLabel.AddControlsReturnThis(
				new LaunchWindowLink( new ModalWindow( this, new PlaceHolder().AddControlsReturnThis( "More information...".ToComponents().GetControls() ) ) )
					{
						ActionControlStyle = new TextActionControlStyle( " (popup)" )
					} );
			omni.AddFormItems(
				FormItem.Create( complexLabel, new EwfTextBox( "" ), cellSpan: 2 ),
				FormItem.Create( "Normal price", new PlaceHolder().AddControlsReturnThis( "".ToComponents().GetControls() ) ),
				FormItem.Create( "Actual price", new EwfTextBox( "" ) ),
				FormItem.Create( "Quantity", new EwfTextBox( "" ) ),
				FormItem.Create( "Inventory", new PlaceHolder().AddControlsReturnThis( "".ToComponents().GetControls() ) ),
				FormItem.Create( "Bill Number", new PlaceHolder().AddControlsReturnThis( "".ToComponents().GetControls() ) ) );
			ph.AddControlsReturnThis( omni );
		}

		public override bool IsAutoDataUpdater => true;
	}
}