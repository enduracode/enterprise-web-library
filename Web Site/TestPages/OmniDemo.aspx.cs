using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class OmniDemo: EwfPage {
		protected override PageContent getContent() {
			var content = new UiPageContent( isAutoDataUpdater: true );

			var omni = FormItemList.CreateGrid( numberOfColumns: 7 );

			var boxId = new ModalBoxId();
			omni.AddFormItems(
				new TextControl( "", true ).ToFormItem(
					setup: new FormItemSetup( columnSpan: 2 ),
					label: "Model number ".ToComponents()
						.Append(
							new EwfButton(
								new StandardButtonStyle( "(popup)", buttonSize: ButtonSize.ShrinkWrap ),
								behavior: new OpenModalBehavior( boxId, etherealChildren: new ModalBox( boxId, true, "More information...".ToComponents() ).ToCollection() ) ) )
						.Materialize() ),
				"".ToComponents().ToFormItem( label: "Normal price".ToComponents() ),
				new TextControl( "", true ).ToFormItem( label: "Actual price".ToComponents() ),
				new TextControl( "", true ).ToFormItem( label: "Quantity".ToComponents() ),
				"".ToComponents().ToFormItem( label: "Inventory".ToComponents() ),
				"".ToComponents().ToFormItem( label: "Bill Number".ToComponents() ) );
			content.Add( omni );

			return content;
		}
	}
}