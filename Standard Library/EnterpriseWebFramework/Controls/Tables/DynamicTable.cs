using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DataAccess.Ranking;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A wrapper built around System.Web.UI.WebControls.Table that makes it easier to manipulate from code.
	/// </summary>
	public class DynamicTable: WebControl, ControlTreeDataLoader {
		private const string pageStateKey = "rowLimit";

		private Table captionTable;
		private ControlStack captionStack;
		private ControlStack actionLinkStack;
		private Table table;

		private string caption = "";
		private string subCaption = "";
		private DataRowLimit defaultDataRowLimit = DataRowLimit.Unlimited;
		private readonly List<ActionButtonSetup> actionLinks = new List<ActionButtonSetup>();
		private readonly Dictionary<PostBackButton, RowMethod> selectedRowActionButtonsToMethods = new Dictionary<PostBackButton, RowMethod>();
		private readonly List<PostBackButton> selectedRowActionButtonsToAdd = new List<PostBackButton>();
		private bool allowExportToExcel;
		private bool isStandard = true;

		private List<ColumnSetup> columnSetups;
		private List<RowColumnSpanPair> previousRowColumnSpans = new List<RowColumnSpanPair>();
		private readonly List<RowSetup> rowSetups = new List<RowSetup>(); // This is parallel to table.Rows.
		private int dataRowCount;

		/// <summary>
		/// Returns the collection of strings representing the header for this table (if any). This is useful to share the column headers between the table and a separate
		/// Excel Export routine.
		/// </summary>
		public IEnumerable<string> ColumnHeaders { get; private set; }

		/// <summary>
		/// Set to true if you want this table to hide itself if it has no content rows.
		/// </summary>
		public bool HideIfEmpty { get; set; }

		/// <summary>
		/// Creates a table.
		/// </summary>
		public DynamicTable() {
			createChildControlsAndWireUpEvents();
		}

		/// <summary>
		/// Creates a table with the specified columns. Do not call SetUpColumns when using this constructor.
		/// </summary>
		public DynamicTable( params ColumnSetup[] columnSetups ) {
			createChildControlsAndWireUpEvents();
			SetUpColumns( columnSetups );
		}

		/// <summary>
		/// Creates a table with the specified columns and header cells. Do not call SetUpColumns when using this constructor.
		/// </summary>
		public DynamicTable( params EwfTableColumn[] columns ) {
			createChildControlsAndWireUpEvents();
			SetUpColumns( columns );
		}

		private void createChildControlsAndWireUpEvents() {
			captionTable = TableOps.CreateUnderlyingTable();
			captionTable.CssClass = "ewfStandardDynamicTableCaption";
			captionTable.Visible = false;
			var row = new TableRow();

			var captionCell = new TableCell();
			captionCell.Controls.Add( captionStack = ControlStack.Create( false ) );
			row.Cells.Add( captionCell );

			var actionLinksCell = new TableCell { CssClass = "ewfAddItemLink" };
			actionLinksCell.Controls.Add( actionLinkStack = ControlStack.Create( false ) );
			row.Cells.Add( actionLinksCell );

			captionTable.Rows.Add( row );
			Controls.Add( captionTable );

			table = TableOps.CreateUnderlyingTable();
			Controls.Add( table );

			PreRender += ewfTable_PreRender;
		}

		/// <summary>
		/// Configures this table with the specified columns and header cells. This is optional, but must be done before adding any rows if it is to be done.
		/// </summary>
		public void SetUpColumns( params EwfTableColumn[] columns ) {
			SetUpColumns( null, columns );
		}

		/// <summary>
		/// Configures this table with the specified csvLine for the column header line of an exported CSV file, and columns and header cells. This is optional, but must be done before adding any rows if it is to be done.
		/// </summary>
		public void SetUpColumns( List<string> csvLine, params EwfTableColumn[] columns ) {
			ColumnHeaders = columns.Select( c => ( (CellPlaceholder)c.HeaderCell ).SimpleText ).ToList();
			var columnSetupsLocal = new List<ColumnSetup>();
			var headerCells = new List<EwfTableCell>();
			foreach( var column in columns ) {
				columnSetupsLocal.Add( column.ColumnSetup );
				headerCells.Add( column.HeaderCell );
			}
			SetUpColumns( columnSetupsLocal.ToArray() );
			// NOTE: This AddRow call causes a problem. When reordering or selected actions are used, the header contains fewer cells than the body.
			// To fix this, we need to either make AddRow be deferred, or make this code here deferred (to ControlTreeDataLoader.LoadData).
			AddRow( new RowSetup { CsvLine = csvLine, IsHeader = true }, headerCells.ToArray() );
		}

		/// <summary>
		/// Configures this table with the specified columns. This is optional, but must be done before adding any rows if it is to be done.
		/// </summary>
		public void SetUpColumns( params ColumnSetup[] columnSetups ) {
			if( this.columnSetups != null )
				throw new ApplicationException( "SetUpColumns cannot be called multiple times." );
			this.columnSetups = new List<ColumnSetup>( columnSetups );
		}

		/// <summary>
		/// Sets the caption that appears above the table. Setting this to the empty string means the table will have no caption.
		/// </summary>
		public string Caption { set { caption = value; } }

		/// <summary>
		/// Sets the sub caption that appears directly under the caption. Do not pass null. Setting this to the empty string means there will be no sub caption.
		/// </summary>
		public string SubCaption { set { subCaption = value; } }

		/// <summary>
		/// The maximum number of result rows that will be shown. Default is DataRowLimit.Unlimited. A default row limit of anything other than Unlimited
		/// will cause the table to show a control allowing the user to select how many results they want to see, as well as an indicator of the total
		/// number of results that would be shown if there was no limit.
		/// </summary>
		public DataRowLimit DefaultDataRowLimit { set { defaultDataRowLimit = value; } }

		/// <summary>
		/// Returns the maximum number of rows that will be shown in this table.
		/// </summary>
		public int CurrentDataRowLimit { get { return EwfPage.Instance.PageState.GetValue( this, pageStateKey, (int)defaultDataRowLimit ); } }

		/// <summary>
		/// Adds a new action link to the top of the table.  This could be used to add a new customer or other entity to the table, for example.
		/// </summary>
		public void AddActionLink( ActionButtonSetup actionButtonSetup ) {
			actionLinks.Add( actionButtonSetup );
		}

		/// <summary>
		/// Adds a new column on the left of the table containing a checkbox for each row.
		/// Adds an action link to the top of the table that acts on all rows that are checked.
		/// Each row must specify the UniqueIdentifier for its row setup. This will be passed back to the given action method.
		/// Adding multiple actions results in just one column of checkboxes.
		/// The checkbox column consumes 5% of the table width.
		/// The action method automatically executes in an EhModifyData method, so your implementation does not need to do this.
		/// </summary>
		public void AddSelectedRowsAction( string label, RowMethod action ) {
			var button = new PostBackButton( new DataModification(), () => { }, new TextActionControlStyle( label ), false );
			selectedRowActionButtonsToMethods.Add( button, action );
			selectedRowActionButtonsToAdd.Add( button );
		}

		/// <summary>
		/// Adds a new column on the left of the table containing a checkbox for each row.
		/// Use this method if you have an existing button (that might affect multiple tables) that you want to fire the action.
		/// Each row must specify the UniqueIdentifier for its row setup. This will be passed back to the given action method.
		/// Adding multiple actions results in just one column of checkboxes.
		/// The checkbox column consumes 5% of the table width.
		/// The action method automatically executes in an EhModifyData method, so your implementation does not need to do this.
		/// </summary>
		public void AddSelectedRowsAction( PostBackButton existingButton, RowMethod action ) {
			selectedRowActionButtonsToMethods.Add( existingButton, action );
		}

		/// <summary>
		/// Sets whether or not this table will have standard styling.
		/// </summary>
		public bool IsStandard { set { isStandard = value; } }

		/// <summary>
		/// Set to true if you want an Export to Excel action link to appear.  This will only work if the table consists of simple text (no controls).
		/// </summary>
		public bool AllowExportToExcel { set { allowExportToExcel = value; } }

		/// <summary>
		/// Gets or sets the CSS class on the underlying table.
		/// </summary>
		public string TableCssClass { get { return table.CssClass; } set { table.CssClass = value; } }

		/// <summary>
		/// Add all data to the table at once, using a deferred rowAdder to prevent incurring performance penalties for creating Info objects
		/// for rows that will end up being hidden.
		/// If you are using this in conjunction with ExportToExcel or AllowExportToExcel, you will only export visible rows to Excel. The same table created without
		/// using this method will give you all rows in the Excel export regardless of what is visible.
		/// Items is accessed multiple times in this method. If items is a complex LINQ expression, this could be a performance issue. If this is the case, call ToList on it first.
		/// </summary>
		public void AddAllDataToTable<ItemType>( DataRowLimit defaultDataRowLimit, IEnumerable<ItemType> items, Action<ItemType> rowAdder ) {
			// NOTE: To defer the actions in this method to ControlTreeDataLoader.LoadData, create a generic class that contains the items and the rowAdder and then
			// create a non-generic interface with a non-generic AddRows( int rowLimit ) method.

			DefaultDataRowLimit = defaultDataRowLimit;

			foreach( var item in items.Take( CurrentDataRowLimit ) )
				rowAdder( item );

			// NOTE: We do this so we get the number of rows returned rather than the number shown. But, this is flawed because it doesn't take into account header rows.
			dataRowCount = items.Count();
		}

		/// <summary>
		/// Adds a text-only row to this table.
		/// </summary>
		public void AddTextRow( params string[] cellText ) {
			AddTextRow( new RowSetup(), cellText );
		}

		/// <summary>
		/// Adds a row of controls to this table.
		/// </summary>
		public void AddRow( params Control[] controls ) {
			AddRow( new RowSetup(), controls );
		}

		/// <summary>
		/// Adds a row to this table.
		/// </summary>
		public void AddRow( params EwfTableCell[] cells ) {
			AddRow( new RowSetup(), cells );
		}

		/// <summary>
		/// Adds a text-only row to this table.
		/// </summary>
		public void AddTextRow( RowSetup rowSetup, params string[] cellText ) {
			AddRow( rowSetup, cellText.Select( ct => new EwfTableCell( ct ) ).ToArray() );
		}

		/// <summary>
		/// Adds a row of controls to this table.
		/// </summary>
		public void AddRow( RowSetup rowSetup, params Control[] controls ) {
			AddRow( rowSetup, controls.Select( c => new EwfTableCell( c ) ).ToArray() );
		}

		/// <summary>
		/// Adds a row to this table.
		/// </summary>
		public void AddRow( RowSetup rowSetup, params EwfTableCell[] cells ) {
			// If SetUpColumns was never called, implicitly set up the columns based on this first row.
			if( columnSetups == null )
				columnSetups = cells.Select( c => new ColumnSetup() ).ToList();

			rowSetups.Add( rowSetup );
			if( !rowSetup.IsHeader )
				dataRowCount++;

			var defaultCsvLine = cells.Select( cell => ( cell as CellPlaceholder ).SimpleText ).ToList();

			if( rowSetup.CsvLine == null )
				rowSetup.CsvLine = defaultCsvLine;

			// Verify that this row has the right number of cells.
			try {
				if( cells.Sum( c => c.FieldSpan ) + previousRowColumnSpans.Sum( rcSpan => rcSpan.ColumnSpan ) != columnSetups.Count )
					throw new ApplicationException( "Row to be added has the wrong number of cells." );

				// Despite that it would make no sense to do this and all latest browsers will draw tables incorrectly when this happens, I cannot find official documentation
				// saying that it is wrong. NOTE: This check isn't as good as the logic we are going to add to EwfTableItemRemainingData (to ensure that the item has at
				// least one cell) because it doesn't catch a case like two cells that each have a row span greater than one and together span all columns.
				if( cells.Any( c => c.ItemSpan > 1 && c.FieldSpan == columnSetups.Count ) )
					throw new ApplicationException( "Cell may not take up all columns and span multiple rows." );
			}
			catch( ApplicationException e ) {
				if( !AppTools.IsDevelopmentInstallation )
					AppTools.EmailAndLogError( "", e );
				else
					throw;
			}
			foreach( var rowColumnSpanPair in previousRowColumnSpans )
				rowColumnSpanPair.RowSpan--;

			previousRowColumnSpans =
				( previousRowColumnSpans.Where( rowSpan => rowSpan.RowSpan > 0 )
				                        .Concat(
					                        cells.Where( c => c.ItemSpan != 1 )
					                             .Select( rowSpanCell => new RowColumnSpanPair { RowSpan = rowSpanCell.ItemSpan - 1, ColumnSpan = rowSpanCell.FieldSpan } ) ) )
					.ToList();

			var cellPlaceHolders = new List<CellPlaceholder>( cells );
			TableOps.DrawRow( table, rowSetup, cellPlaceHolders, columnSetups, false );
		}

		private class RowColumnSpanPair {
			public int RowSpan;
			public int ColumnSpan;
		}

		/// <summary>
		/// Returns true if this table has content rows (not including header rows, etc.).
		/// </summary>
		public bool HasContentRows { get { return dataRowCount > 0; } }

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			var modifiedCaption = caption;

			// Display the caption and the sub caption.
			if( defaultDataRowLimit != DataRowLimit.Unlimited ) {
				var formattedDataRowCount = dataRowCount.ToString( "N0" );
				if( caption.Length > 0 )
					modifiedCaption += " (" + formattedDataRowCount + ")";
				else
					modifiedCaption = formattedDataRowCount + " items";
			}
			if( modifiedCaption.Length > 0 ) {
				captionTable.Visible = true;
				captionStack.AddControls( new Label { Text = modifiedCaption, CssClass = "ewfCaption" } );
			}
			if( subCaption.Length > 0 ) {
				captionTable.Visible = true;
				captionStack.AddText( subCaption );
			}

			// Row limiting
			if( defaultDataRowLimit != DataRowLimit.Unlimited ) {
				captionStack.AddControls( new ControlLine( new LiteralControl( "Show:" ),
				                                           getDataRowLimitControl( DataRowLimit.Fifty ),
				                                           getDataRowLimitControl( DataRowLimit.FiveHundred ),
				                                           getDataRowLimitControl( DataRowLimit.Unlimited ) ) );
			}

			// Excel export
			if( allowExportToExcel )
				actionLinks.Add( new ActionButtonSetup( "Export to Excel", new PostBackButton( new DataModification(), ExportToExcel ) ) );

			// Action links
			foreach( var actionLink in actionLinks ) {
				captionTable.Visible = true;
				actionLinkStack.AddControls( actionLink.BuildButton( text => new TextActionControlStyle( text ), false ) );
			}

			// Selected row actions
			foreach( var button in selectedRowActionButtonsToAdd ) {
				captionTable.Visible = true;
				actionLinkStack.AddControls( button );
			}

			foreach( var buttonToMethod in selectedRowActionButtonsToMethods ) {
				var button = buttonToMethod.Key;
				var method = buttonToMethod.Value;
				button.ClickHandler = delegate {
					EwfPage.Instance.EhModifyData( cn1 => {
						foreach( var rowSetup in rowSetups ) {
							if( rowSetup.UniqueIdentifier != null &&
							    ( (EwfCheckBox)rowSetup.UnderlyingTableRow.Cells[ 0 ].Controls[ 0 ] ).IsCheckedInPostBack(
								    AppRequestState.Instance.EwfPageRequestState.PostBackValues ) )
								method( cn1, rowSetup.UniqueIdentifier );
						}
					} );
				};
			}

			if( selectedRowActionButtonsToMethods.Count > 0 ) {
				foreach( var rowSetup in rowSetups ) {
					var cell = new TableCell
						{
							Width = Unit.Percentage( 5 ),
							CssClass = EwfTable.CssElementCreator.AllCellAlignmentsClass.ConcatenateWithSpace( "ewfNotClickable" )
						};
					if( rowSetup.UniqueIdentifier != null )
						cell.Controls.Add( new EwfCheckBox( false ) );
					rowSetup.UnderlyingTableRow.Cells.AddAt( 0, cell );
				}
			}

			// Reordering
			var filteredRowSetups = rowSetups.Where( rs => rs.RankId.HasValue ).ToList();
			for( var i = 0; i < filteredRowSetups.Count; i++ ) {
				var previousRowSetup = ( i == 0 ? null : filteredRowSetups[ i - 1 ] );
				var rowSetup = filteredRowSetups[ i ];
				var nextRowSetup = ( ( i == filteredRowSetups.Count - 1 ) ? null : filteredRowSetups[ i + 1 ] );

				var upButton = new PostBackButton( new DataModification(),
				                                   () => { },
				                                   new ButtonActionControlStyle( @"/\", ButtonActionControlStyle.ButtonSize.ShrinkWrap ),
				                                   false );
				var downButton = new PostBackButton( new DataModification(),
				                                     () => { },
				                                     new ButtonActionControlStyle( @"\/", ButtonActionControlStyle.ButtonSize.ShrinkWrap ),
				                                     false );
				var controlLine = new ControlLine( new Control[ 0 ] );

				if( previousRowSetup != null ) {
					upButton.ClickHandler = () => EwfPage.Instance.EhModifyData( cn1 => RankingMethods.SwapRanks( cn1, previousRowSetup.RankId.Value, rowSetup.RankId.Value ) );
					controlLine.AddControls( upButton );
				}
				if( nextRowSetup != null ) {
					downButton.ClickHandler = () => EwfPage.Instance.EhModifyData( cn1 => RankingMethods.SwapRanks( cn1, rowSetup.RankId.Value, nextRowSetup.RankId.Value ) );
					controlLine.AddControls( downButton );
				}

				// NOTE: What about rows that don't have a RankId? They need to have an empty cell so all rows have the same cell count.
				var cell = new TableCell
					{
						Width = Unit.Percentage( 10 ),
						CssClass = EwfTable.CssElementCreator.AllCellAlignmentsClass.ConcatenateWithSpace( "ewfNotClickable" )
					};
				cell.Controls.Add( controlLine );
				rowSetup.UnderlyingTableRow.Cells.Add( cell );
			}

			if( HideIfEmpty && !HasContentRows )
				Visible = false;
		}

		/// <summary>
		/// Performs an EhModifyDataAndSendFile operation. This is convenient if you want to get the built-in export functionality, but from
		/// an external button rather than an action on this table.
		/// </summary>
		public void ExportToExcel() {
			var workbook = new ExcelFileWriter { UseLegacyExcelFormat = true };
			foreach( var rowSetup in rowSetups ) {
				if( rowSetup.IsHeader )
					workbook.DefaultWorksheet.AddHeaderToWorksheet( rowSetup.CsvLine.ToArray() );
				else
					workbook.DefaultWorksheet.AddRowToWorksheet( rowSetup.CsvLine.ToArray() );
			}
			workbook.SendExcelFile( caption.Length > 0 ? caption : "Excel export" );
		}

		private Control getDataRowLimitControl( DataRowLimit dataRowLimit ) {
			if( dataRowLimit == (DataRowLimit)CurrentDataRowLimit )
				return new Literal { Text = getDataRowLimitText( dataRowLimit ) };
			return new PostBackButton( new DataModification(),
			                           () => EwfPage.Instance.EhExecute( () => EwfPage.Instance.PageState.SetValue( this, pageStateKey, (int)dataRowLimit ) ),
			                           new TextActionControlStyle( getDataRowLimitText( dataRowLimit ) ),
			                           false );
		}

		private string getDataRowLimitText( DataRowLimit dataRowLimit ) {
			return dataRowLimit == DataRowLimit.Unlimited ? "All" : ( (int)dataRowLimit ).ToString();
		}

		private void ewfTable_PreRender( object sender, EventArgs e ) {
			// NOTE: This should all move to ControlTreeDataLoader.LoadData, but it can't right now because so many pages add table rows from PreRender.
			TableOps.AlternateRowColors( table, rowSetups );

			// NOTE: We should be able to get rid of this row hiding when we port everything over to use AddAllDataToTable.
			var dataRowIndex = 0;
			for( var rowIndex = 0; rowIndex < rowSetups.Count; rowIndex++ )
				table.Rows[ rowIndex ].Visible = dataRowIndex++ < CurrentDataRowLimit;
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }

		/// <summary>
		/// Renders this control after applying the appropriate CSS classes.
		/// </summary>
		protected override void Render( HtmlTextWriter writer ) {
			if( isStandard ) {
				CssClass = CssClass.ConcatenateWithSpace( "ewfStandardDynamicTable" );
				table.CssClass = table.CssClass.ConcatenateWithSpace( "ewfStandard" );
			}
			base.Render( writer );
		}
	}
}