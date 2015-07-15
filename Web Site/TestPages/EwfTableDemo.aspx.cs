using System;
using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class EwfTableDemo: EwfPage {
		partial class Info {
			public override string ResourceName { get { return "EWF Table"; } }
		}

		protected override void loadData() {
			var dataRows = Enumerable.Range( 0, 550 );

			var table = EwfTable.Create(
				defaultItemLimit: DataRowLimit.Fifty,
				caption: "Caption",
				subCaption: "Sub caption",
				fields:
					new[]
						{ new EwfTableField( size: Unit.Percentage( 1 ), toolTip: "First column!" ), new EwfTableField( size: Unit.Percentage( 2 ), toolTip: "Second column!" ) },
				headItems: new EwfTableItem( "First Column", "Second Column" ).ToSingleElementArray() );
			table.AddData(
				dataRows,
				i =>
				new EwfTableItem(
					new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( ActionControls.GetInfo() ) ),
					i.ToString(),
					( i * 2 ) + Environment.NewLine + "extra stuff" ) );
			place.Controls.Add( table );
		}
	}
}