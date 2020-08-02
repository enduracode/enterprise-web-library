using System;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class ColumnPrimaryTableDemo: EwfPage {
		partial class Info {
			public override string ResourceName => "Column Primary";
		}

		protected override void loadData() {
			var itemGroups = Enumerable.Range( 1, 2 )
				.Select(
					group => new ColumnPrimaryItemGroup(
						"Group {0}".FormatWith( group ).ToComponents(),
						groupActions: new ButtonSetup(
								"Action 1",
								behavior: new PostBackBehavior(
									postBack: PostBack.CreateIntermediate(
										null,
										id: PostBack.GetCompositeId( group.ToString(), "action1" ),
										firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "Action 1" ) ) ) ).Append(
								new ButtonSetup(
									"Action 2",
									behavior: new PostBackBehavior(
										postBack: PostBack.CreateIntermediate(
											null,
											id: PostBack.GetCompositeId( group.ToString(), "action2" ),
											firstModificationMethod: () => AddStatusMessage( StatusMessageType.Info, "Action 2" ) ) ) ) )
							.Materialize(),
						items: Enumerable.Range( 1, 5 )
							.Select(
								i => new EwfTableItem(
									new EwfTableItemSetup( activationBehavior: ElementActivationBehavior.CreateRedirectScript( ActionControls.GetInfo() ) ),
									i.ToString().ToCell(),
									( ( i * 2 ) + Environment.NewLine + "extra stuff" ).ToCell() ) ) ) )
				.Materialize();

			place.AddControlsReturnThis(
				ColumnPrimaryTable.Create(
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
						fields: new[] { new EwfTableField( size: 1.ToPercentage() ), new EwfTableField( size: 2.ToPercentage() ) } )
					.AddItemGroups( itemGroups )
					.ToCollection()
					.GetControls() );
		}
	}
}