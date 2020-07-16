using System;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class ColumnPrimaryTableDemo: EwfPage {
		partial class Info {
			public override string ResourceName => "Column Primary";
		}

		protected override void loadData() {
			var items = Enumerable.Range( 0, 20 )
				.Select(
					i => new EwfTableItem(
						new EwfTableItemSetup( activationBehavior: ElementActivationBehavior.CreateRedirectScript( ActionControls.GetInfo() ) ),
						i.ToString().ToCell(),
						( ( i * 2 ) + Environment.NewLine + "extra stuff" ).ToCell() ) )
				.ToList();

			place.AddControlsReturnThis(
				new ColumnPrimaryTable(
						caption: "My table",
						subCaption: "A new table implementation",
						allowExportToExcel: true,
						tableActions: new ButtonSetup(
							"Action",
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateIntermediate(
									null,
									id: "action",
									firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "You clicked action." ) ) ) ).ToCollection(),
						fields: new[] { new EwfTableField( size: 1.ToPercentage() ), new EwfTableField( size: 2.ToPercentage() ) },
						items: items ).ToCollection()
					.GetControls() );
		}
	}
}