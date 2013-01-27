using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	[ Obsolete( "Guaranteed through 28 February 2013." ) ]
	public class ColumnPrimaryDynamicTable: WebControl, ControlTreeDataLoader {
		private List<RowSetup> rowSetups;
		private readonly List<ColumnSetup> columnSetups = new List<ColumnSetup>();
		private readonly List<List<CellPlaceholder>> columnPlaceholders = new List<List<CellPlaceholder>>();

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public ColumnPrimaryDynamicTable() {
			createChildControlsAndWireUpEvents();
		}

		private void createChildControlsAndWireUpEvents() {
			UnderlyingTable = TableOps.CreateUnderlyingTable();
			Controls.Add( UnderlyingTable );
		}

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public void SetUpRows( params RowSetup[] rowSetups ) {
			if( this.rowSetups != null )
				throw new ApplicationException( "SetUpRows cannot be called multiple times." );
			this.rowSetups = new List<RowSetup>( rowSetups );
		}

		/// <summary>
		/// Gets the ASP.NET table control that is used to implement this table.
		/// </summary>
		public System.Web.UI.WebControls.Table UnderlyingTable { get; private set; }

		/// <summary>
		/// Set to true if you want the row colors to alternate like a normal dynamic table. The default is false.
		/// </summary>
		public bool AlternateRowColors { get; set; }

		/// <summary>
		/// Returns the number of rows (according to the row setups passed in SetUpRows or the rows implicitly created by the first call to AddColumn)
		/// in this table.
		/// </summary>
		public int NumberOfRows { get { return rowSetups.Count; } }

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public void AddTextColumn( params string[] cellText ) {
			AddTextColumn( null, cellText );
		}

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public void AddColumn( params EwfTableCell[] cells ) {
			AddColumn( null, cells );
		}

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public void AddTextColumn( ColumnSetup columnSetup, params string[] cellText ) {
			var cells = new EwfTableCell[ cellText.Length ];
			for( var i = 0; i < cellText.Length; i += 1 )
				cells[ i ] = new EwfTableCell( cellText[ i ] );
			AddColumn( columnSetup, cells );
		}

		[ Obsolete( "Guaranteed through 28 February 2013." ) ]
		public void AddColumn( ColumnSetup columnSetup, params EwfTableCell[] cells ) {
			// If SetUpRows was never called, implicitly set up the rows based on this first column.
			if( rowSetups == null ) {
				rowSetups = new List<RowSetup>();
				foreach( var cell in cells ) {
					for( var i = 0; i < cell.FieldSpan; i++ )
						rowSetups.Add( new RowSetup() );
				}
			}

			columnSetups.Add( columnSetup ?? new ColumnSetup() );
			var columnIndex = columnSetups.Count - 1;

			// Add a placeholder for this column if necessary.
			if( columnSetups.Count > columnPlaceholders.Count )
				addColumnPlaceholder();
			var columnPlaceholder = columnPlaceholders[ columnIndex ];

			// Sum the cells taken up by previously-added columns (via their ColSpan property).
			var potentialCellPlaceholders = columnPlaceholder.Count( cellPlaceholder => cellPlaceholder != null );

			// Add to that the number of cells this new column will take up.
			potentialCellPlaceholders += cells.Sum( cell => cell.FieldSpan );

			if( potentialCellPlaceholders != rowSetups.Count )
				throw new ApplicationException( "Column to be added has " + potentialCellPlaceholders + " cells, but should have " + rowSetups.Count + " cells." );

			// Add this column's cells and any necessary spaces to columnPlaceholders.
			var placeholderRow = 0;
			foreach( var cell in cells ) {
				while( columnPlaceholder[ placeholderRow ] != null )
					placeholderRow++;
				for( var cellRow = 0; cellRow < cell.FieldSpan; cellRow++ ) {
					for( var cellCol = 0; cellCol < cell.ItemSpan; cellCol++ ) {
						if( columnSetups.Count + cellCol > columnPlaceholders.Count )
							addColumnPlaceholder();
						if( columnPlaceholders[ columnIndex + cellCol ][ placeholderRow ] != null )
							throw new ApplicationException( "Two cells spanning multiple rows and/or columns have collided." );
						columnPlaceholders[ columnIndex + cellCol ][ placeholderRow ] = cellCol == 0 && cellRow == 0 ? cell as CellPlaceholder : new SpaceForMultiColOrRowCell();
					}
					placeholderRow++;
				}
			}
		}

		private void addColumnPlaceholder() {
			var placeholders = new List<CellPlaceholder>();
			for( var i = 0; i < rowSetups.Count; i++ )
				placeholders.Add( null );
			columnPlaceholders.Add( placeholders );
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			if( columnSetups.Count == 0 )
				return;

			if( columnSetups.Count != columnPlaceholders.Count )
				throw new ApplicationException( "There are incomplete columns in this table. It cannot be drawn." );
			for( var row = 0; row < rowSetups.Count; row++ ) {
				var rowPlaceholder = new List<CellPlaceholder>();
				for( var col = 0; col < columnSetups.Count; col++ )
					rowPlaceholder.Add( columnPlaceholders[ col ][ row ] );
				TableOps.DrawRow( UnderlyingTable, rowSetups[ row ], rowPlaceholder, columnSetups, true );
			}

			if( AlternateRowColors )
				TableOps.AlternateRowColors( UnderlyingTable, rowSetups );
		}

		/// <summary>
		/// Returns the div tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }

		/// <summary>
		/// Renders this control after applying the appropriate CSS classes.
		/// </summary>
		protected override void Render( HtmlTextWriter writer ) {
			CssClass = CssClass.ConcatenateWithSpace( "ewfStandardDynamicTable" );
			UnderlyingTable.CssClass = UnderlyingTable.CssClass.ConcatenateWithSpace( "ewfStandard" );
			base.Render( writer );
		}
	}
}