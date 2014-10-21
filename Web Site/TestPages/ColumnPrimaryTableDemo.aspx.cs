using System;
using System.Linq;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class ColumnPrimaryTableDemo: EwfPage {
		partial class Info {
			public override string ResourceName { get { return "Column Primary"; } }
		}

		protected override void loadData() {
			var items =
				Enumerable.Range( 0, 20 )
					.Select(
						i =>
						new EwfTableItem(
							new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( ActionControls.GetInfo() ) ),
							i.ToString(),
							( i * 2 ) + Environment.NewLine + "extra stuff" ) )
					.ToList();

			place.Controls.Add(
				new ColumnPrimaryTable(
					caption: "My table",
					subCaption: "A new table implementation",
					fields:
						new[]
							{ new EwfTableField( size: Unit.Percentage( 1 ), toolTip: "First column!" ), new EwfTableField( size: Unit.Percentage( 2 ), toolTip: "Second column!" ) },
					items: items ) );
		}
	}
}