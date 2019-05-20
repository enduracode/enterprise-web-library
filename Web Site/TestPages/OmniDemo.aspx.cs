using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class OmniDemo: EwfPage {
		protected override void loadData() {
			var omni = FormItemBlock.CreateFormItemList( numberOfColumns: 7 );

			var boxId = new ModalBoxId();
			new ModalBox( boxId, true, "More information...".ToComponents() ).ToCollection().AddEtherealControls( this );

			omni.AddFormItems(
				new TextControl( "", true ).ToFormItem(
					setup: new FormItemSetup( cellSpan: 2 ),
					label: "Model number ".ToComponents()
						.Append( new EwfButton( new StandardButtonStyle( "(popup)", buttonSize: ButtonSize.ShrinkWrap ), behavior: new OpenModalBehavior( boxId ) ) )
						.Materialize() ),
				FormItem.Create( "Normal price", new PlaceHolder().AddControlsReturnThis( "".ToComponents().GetControls() ) ),
				new TextControl( "", true ).ToFormItem( label: "Actual price".ToComponents() ),
				new TextControl( "", true ).ToFormItem( label: "Quantity".ToComponents() ),
				FormItem.Create( "Inventory", new PlaceHolder().AddControlsReturnThis( "".ToComponents().GetControls() ) ),
				FormItem.Create( "Bill Number", new PlaceHolder().AddControlsReturnThis( "".ToComponents().GetControls() ) ) );
			ph.AddControlsReturnThis( omni );
		}

		public override bool IsAutoDataUpdater => true;
	}
}