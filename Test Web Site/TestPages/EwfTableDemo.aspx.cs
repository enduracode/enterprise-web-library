using System;
using System.Linq;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.TestWebSite.TestPages {
	public partial class EwfTableDemo: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return "EWF Table"; } }
		}

		protected override void LoadData( DBConnection cn ) {
			var dataRows = Enumerable.Range( 0, 550 );

			var table = EwfTable.Create( defaultItemLimit: DataRowLimit.Fifty,
			                             caption: "Caption",
			                             subCaption: "Sub caption",
			                             fields:
			                             	new[]
			                             		{
			                             			new EwfTableField( size: Unit.Percentage( 1 ), toolTip: "First column!" ),
			                             			new EwfTableField( size: Unit.Percentage( 2 ), toolTip: "Second column!" )
			                             		},
			                             headItems: new EwfTableItem( "First Column".ToCell(), "Second Column".ToCell() ).ToSingleElementArray() );
			table.AddData( dataRows,
			               i =>
			               new EwfTableItem( new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( ActionControls.GetInfo() ) ),
			                                 new EwfTableCell( i.ToString() ),
			                                 new EwfTableCell( ( i * 2 ) + Environment.NewLine + "extra stuff" ) ) );
			place.Controls.Add( table );
		}
	}
}