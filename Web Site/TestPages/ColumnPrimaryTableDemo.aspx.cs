using System;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

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
						i.ToString(),
						( i * 2 ) + Environment.NewLine + "extra stuff" ) )
				.ToList();

			place.AddControlsReturnThis(
				new ColumnPrimaryTable(
						caption: "My table",
						subCaption: "A new table implementation",
						fields: new[] { new EwfTableField( size: 1.ToPercentage() ), new EwfTableField( size: 2.ToPercentage() ) },
						items: items ).ToCollection()
					.GetControls() );
		}
	}
}