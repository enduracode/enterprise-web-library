using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class DynamicTableDemo: EwfPage {
		partial class Info {
			public override string PageName { get { return "Old Table"; } }

			protected override AlternativePageMode createAlternativeMode() {
				return new DisabledPageMode( "This demo is disabled." );
			}
		}

		protected override void loadData() {
			//Nothing Special
			for( var i = 0; i < 20; i += 3 )
				table.AddTextRow( i.ToString(), ( i + 1 ).ToString(), ( +2 ).ToString() );
			table.AllowExportToExcel = true;
			table.DefaultDataRowLimit = DataRowLimit.Fifty;

			//Selected Rows Action
			table2.SetUpColumns( new EwfTableColumn( "1" ), new EwfTableColumn( "2" ), new EwfTableColumn( "3" ), new EwfTableColumn( "4" ) );
			table2.AddTextRow( new RowSetup { ToolTip = "Row tool tip", UniqueIdentifier = 1 }, "One", "Two", "Three", "Four" );
			table2.AllowExportToExcel = true;
			for( var i = 0; i < 20; i += 3 ) {
				table2.AddRow( new RowSetup { UniqueIdentifier = 2 },
				               new EwfTableCell( "One" ),
				               new EwfTableCell( "Two" ),
				               new EwfTableCell( "Three with Tip" ) { ToolTip = "Cell tool tip" },
				               new EwfTableCell( "Four with TipControl" ) { ToolTipControl = new DatePicker( null ) } );
			}
			table2.AddSelectedRowsAction( "Selected Rows Action", delegate( DBConnection cn1, object id ) { AddStatusMessage( StatusMessageType.Info, id.ToString() ); } );
			//Clickable Rows
			for( var i = 0; i < 7; i++ ) {
				ClickScript clickScript;
				string[] cells;
				if( i % 3 == 0 ) {
					clickScript = ClickScript.CreatePostBackScript( delegate { AddStatusMessage( StatusMessageType.Warning, "Postback!" ); } );
					cells = new[] { "Post", "Back", "Row" };
				}
				else if( i % 2 == 0 ) {
					clickScript = ClickScript.CreateRedirectScript( ActionControls.GetInfo() );
					cells = new[] { "Re", "-Direct", "Script" };
				}
				else {
					clickScript = ClickScript.CreateCustomScript( "alert('custom script')" );
					cells = new[] { "Custom", "alert", "Script" };
				}
				table3.AddTextRow( new RowSetup { ClickScript = clickScript, UniqueIdentifier = 3, ToolTip = "Row Tool Tip" }, cells[ 0 ], cells[ 1 ], cells[ 2 ] );
			}

			//Mixed
			table4.AddSelectedRowsAction( "Selected Rows Action", delegate( DBConnection cn1, object id ) { AddStatusMessage( StatusMessageType.Info, id.ToString() ); } );
			for( var i = 0; i < 10; i += 3 )
				table4.AddTextRow( new RowSetup { UniqueIdentifier = 2 }, i.ToString(), ( i + 1 ).ToString(), ( +2 ).ToString() );

			for( var i = 0; i < 4; i++ ) {
				ClickScript clickScript;
				string[] cells;
				if( i % 3 == 0 ) {
					clickScript = ClickScript.CreatePostBackScript( delegate { AddStatusMessage( StatusMessageType.Warning, "Postback!" ); } );
					cells = new[] { "Post", "Back", "Row" };
				}
				else if( i % 2 == 0 ) {
					clickScript = ClickScript.CreateRedirectScript( ActionControls.GetInfo() );
					cells = new[] { "Re", "-Direct", "Script" };
				}
				else {
					clickScript = ClickScript.CreateCustomScript( "alert('custom script')" );
					cells = new[] { "Custom", "alert", "Script" };
				}

				table4.AddTextRow(
					new RowSetup
						{
							ClickScript = clickScript,
							UniqueIdentifier = 3,
							ToolTipControl = new EwfImage { ImageUrl = "http://redstapler.biz/images/logo_blkgradient.png" }
						},
					cells[ 0 ],
					cells[ 1 ],
					cells[ 2 ] );
			}

			//Reorderable
			for( var i = 0; i < 20; i += 3 )
				table5.AddTextRow( new RowSetup { UniqueIdentifier = "6", RankId = 0 }, i.ToString(), ( i + 1 ).ToString(), ( +2 ).ToString() );

			//Mixed - Single Cell
			table6.AddSelectedRowsAction( "Selected Rows Action", delegate( DBConnection cn1, object id ) { AddStatusMessage( StatusMessageType.Info, id.ToString() ); } );
			for( var i = 0; i < 10; i += 3 )
				table6.AddTextRow( new RowSetup { UniqueIdentifier = 2 }, i.ToString() );

			for( var i = 0; i < 10; i += 3 ) {
				table6.AddTextRow(
					new RowSetup
						{
							ClickScript = ClickScript.CreatePostBackScript( delegate { AddStatusMessage( StatusMessageType.Info, "Postback!" ); } ),
							UniqueIdentifier = 3
						},
					"Post" );
			}

			//Recorderable, Clickable
			for( var i = 0; i < 7; i++ ) {
				ClickScript clickScript;
				string[] cells;
				if( i % 3 == 0 ) {
					clickScript = ClickScript.CreatePostBackScript( delegate { AddStatusMessage( StatusMessageType.Warning, "Postback!" ); } );
					cells = new[] { "Post", "Back", "Row" };
				}
				else if( i % 2 == 0 ) {
					clickScript = ClickScript.CreateRedirectScript( ActionControls.GetInfo() );
					cells = new[] { "Re", "-Direct", "Script" };
				}
				else {
					clickScript = ClickScript.CreateCustomScript( "alert('custom script')" );
					cells = new[] { "Custom", "alert", "Script" };
				}
				table7.AddTextRow( new RowSetup { UniqueIdentifier = "6", RankId = 0, ClickScript = clickScript }, cells );
			}

			//Recorderable, Clickable, Selectable
			table8.AddSelectedRowsAction( "Selected Rows Action", delegate( DBConnection cn1, object id ) { AddStatusMessage( StatusMessageType.Info, id.ToString() ); } );
			for( var i = 0; i < 7; i++ ) {
				ClickScript clickScript;
				string[] cells;
				if( i % 3 == 0 ) {
					clickScript = ClickScript.CreatePostBackScript( delegate { AddStatusMessage( StatusMessageType.Warning, "Postback!" ); } );
					cells = new[] { "Post", "Back", "Row" };
				}
				else if( i % 2 == 0 ) {
					clickScript = ClickScript.CreateRedirectScript( ActionControls.GetInfo() );
					cells = new[] { "Re", "-Direct", "Script" };
				}
				else {
					clickScript = ClickScript.CreateCustomScript( "alert('custom script')" );
					cells = new[] { "Custom", "alert", "Script" };
				}
				table8.AddTextRow( new RowSetup { UniqueIdentifier = "6", RankId = 0, ClickScript = clickScript }, cells );
			}

			// Rowspan test
			table9.AddTextRow( "one", "two", "three" );
			table9.AddRow( new EwfTableCell( "four rowspan" ) { ItemSpan = 2 }, new EwfTableCell( "five" ), new EwfTableCell( "six rowspan" ) { ItemSpan = 2 } );
			table9.AddTextRow( "this is allowed because of the previous rowspans" );
			table9.AddTextRow( "seven", "eight", "nine" );
			table9.AddRow( new EwfTableCell( "ten columnspan" ) { FieldSpan = 2 }, new EwfTableCell( "eleven" ) );
			table9.AddRow( new EwfTableCell( "twelve whole row" ) { FieldSpan = 3 } );
			table9.AddRow( new EwfTableCell( "thirteen whole row, three row column span" ) { FieldSpan = 2, ItemSpan = 3 }, new EwfTableCell( "1/3 thirteen" ) );
			table9.AddTextRow( "fourteen" );
			table9.AddTextRow( "fifteen" );
			table9.AddTextRow( "sixteen", "seventeen", "eighteen" );
			table9.AddRow( new EwfTableCell( "Nineteen" ), new EwfTableCell( "Twenty -two rows two columns" ) { FieldSpan = 2, ItemSpan = 2 } );
			table9.AddTextRow( "21" );
			table9.AddTextRow( "Twenty-two", "Twenty-three", "Twenty-four" );
			table9.AddRow( new EwfTableCell( "Twenty-five" ), new EwfTableCell( "Twenty-six -Three rowspan" ) { ItemSpan = 3 }, new EwfTableCell( "Twenty-seven" ) );
			table9.AddTextRow( "Twenty-eight", "Twenty-nine" );
			table9.AddTextRow( "Thirty", "Thirty-one" );

			// Wrong number of cells
			//table9.AddTextRow( "","" );
		}
	}
}