using System;
using System.Linq;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class EwfTableDemo: EwfPage {
		public partial class Info {
			public override string PageName { get { return "EWF Table"; } }
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