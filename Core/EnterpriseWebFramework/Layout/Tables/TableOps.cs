using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	internal static class TableOps {
		internal static void DrawRow(
			Table table, RowSetup rowSetup, List<CellPlaceholder> rowPlaceholder, List<ColumnSetup> columnSetups, bool tableIsColumnPrimary ) {
			var row = new TableRow();
			rowSetup.UnderlyingTableRow = row;
			table.Rows.Add( row );

			row.CssClass = rowSetup.CssClass;
			if( rowSetup.ClickScript != null )
				rowSetup.ClickScript.SetUpClickableControl( row );

			for( var i = 0; i < rowPlaceholder.Count; i++ ) {
				if( rowPlaceholder[ i ] is EwfTableCell )
					row.Cells.Add( buildCell( rowPlaceholder[ i ] as EwfTableCell, rowSetup, columnSetups[ i ], tableIsColumnPrimary ) );
			}

			if( ( !rowSetup.ToolTip.IsNullOrWhiteSpace() || rowSetup.ToolTipControl != null ) && row.Cells.Count > 0 ) {
				// NOTE: This comment is no longer accurate.
				// It is very important that we add the tool tip to the cell so that the tool tip is hidden if the row is hidden.
				// We cannot add the tool tip to the row because rows can't have children of that type.
				new ToolTip( rowSetup.ToolTipControl ?? ToolTip.GetToolTipTextControl( rowSetup.ToolTip ), row );
			}
		}

		private static TableCell buildCell( EwfTableCell ewfCell, RowSetup rowSetup, ColumnSetup columnSetup, bool tableIsColumnPrimary ) {
			var colSpan = tableIsColumnPrimary ? ewfCell.ItemSpan : ewfCell.FieldSpan;
			var rowSpan = tableIsColumnPrimary ? ewfCell.FieldSpan : ewfCell.ItemSpan;

			var underlyingCell = ( rowSetup.IsHeader || columnSetup.IsHeader ) ? new TableHeaderCell() : new TableCell();
			underlyingCell.AddControlsReturnThis( ewfCell.Controls );
			if( colSpan == 1 )
				underlyingCell.Width = columnSetup.Width;
			underlyingCell.CssClass = StringTools.ConcatenateWithDelimiter(
				" ",
				EwfTable.CssElementCreator.AllCellAlignmentsClass,
				columnSetup.CssClassOnAllCells,
				ewfCell.CssClass );
			if( ewfCell.ClickScript != null )
				ewfCell.ClickScript.SetUpClickableControl( underlyingCell );

			if( !ewfCell.ToolTip.IsNullOrWhiteSpace() || ewfCell.ToolTipControl != null || !columnSetup.ToolTipOnCells.IsNullOrWhiteSpace() ) {
				var toolTipControl = ewfCell.ToolTipControl ??
				                     ToolTip.GetToolTipTextControl( !ewfCell.ToolTip.IsNullOrWhiteSpace() ? ewfCell.ToolTip : columnSetup.ToolTipOnCells );
				// NOTE: This comment is no longer accurate.
				// It is very important that we add the tool tip to the cell so that the tool tip is hidden if the row/cell is hidden.
				new ToolTip( toolTipControl, underlyingCell );
			}

			if( colSpan != 1 )
				underlyingCell.ColumnSpan = colSpan;
			if( rowSpan != 1 )
				underlyingCell.RowSpan = rowSpan;
			return underlyingCell;
		}

		internal static Table CreateUnderlyingTable() {
			var table = new Table();
			// using the CellSpacing property resulted in an automatic, unwanted insertion of the border-collapse CSS attribute.
			table.Attributes.Add( "cellspacing", "0" );
			return table;
		}

		internal static void AlternateRowColors( Table table, List<RowSetup> rowSetups ) {
			var contrast = false;
			for( var rowIndex = 0; rowIndex < rowSetups.Count; rowIndex++ ) {
				var row = table.Rows[ rowIndex ];
				if( contrast )
					row.CssClass = row.CssClass.ConcatenateWithSpace( "ewfContrast" );
				contrast = !contrast;
				if( rowIndex == rowSetups.Count - 1 )
					row.CssClass = row.CssClass.ConcatenateWithSpace( "ewfLast" );
			}
		}

		internal static List<List<CellPlaceholder>> BuildCellPlaceholderListsForItems( List<EwfTableItem> items, int fieldCount ) {
			var itemIndex = 0;
			var cellPlaceholderListsForItems = new List<List<CellPlaceholder>>();
			foreach( var itemCells in items.Select( i => i.Cells ) ) {
				// Add a list of cell placeholders for this item if necessary.
				if( itemIndex >= cellPlaceholderListsForItems.Count )
					addCellPlaceholderListForItem( cellPlaceholderListsForItems, fieldCount );

				var cellPlaceholdersForItem = cellPlaceholderListsForItems[ itemIndex ];

				// Sum the cells taken up by previous items, which can happen when cells have item span values greater than one.
				var potentialCellPlaceholderCountForItem = cellPlaceholdersForItem.Count( cellPlaceholder => cellPlaceholder != null );

				// Add to that the number of cells this item will take up.
				potentialCellPlaceholderCountForItem += itemCells.Sum( cell => cell.FieldSpan );

				if( potentialCellPlaceholderCountForItem != fieldCount )
					throw new ApplicationException( "Item to be added has " + potentialCellPlaceholderCountForItem + " cells, but should have " + fieldCount + " cells." );

				// Add this item's cells and any necessary spaces to cellPlaceholderListsForItems.
				var fieldIndex = 0;
				foreach( var cell in itemCells ) {
					while( cellPlaceholdersForItem[ fieldIndex ] != null )
						fieldIndex += 1;
					for( var fieldSpanIndex = 0; fieldSpanIndex < cell.FieldSpan; fieldSpanIndex += 1 ) {
						for( var itemSpanIndex = 0; itemSpanIndex < cell.ItemSpan; itemSpanIndex += 1 ) {
							if( itemIndex + itemSpanIndex >= cellPlaceholderListsForItems.Count )
								addCellPlaceholderListForItem( cellPlaceholderListsForItems, fieldCount );
							if( cellPlaceholderListsForItems[ itemIndex + itemSpanIndex ][ fieldIndex ] != null )
								throw new ApplicationException( "Two cells spanning multiple fields and/or items have overlapped." );
							cellPlaceholderListsForItems[ itemIndex + itemSpanIndex ][ fieldIndex ] = itemSpanIndex == 0 && fieldSpanIndex == 0
								                                                                          ? cell as CellPlaceholder
								                                                                          : new SpaceForMultiColOrRowCell();
						}
						fieldIndex += 1;
					}
				}

				itemIndex += 1;
			}

			if( cellPlaceholderListsForItems.Count != items.Count ) {
				// Since every item must have at least one cell, this message assumes that the first count above is never less than the second.
				throw new ApplicationException( "A cell has overflowed the table." );
			}

			return cellPlaceholderListsForItems;
		}

		private static void addCellPlaceholderListForItem( List<List<CellPlaceholder>> cellPlaceholderListsForItems, int fieldCount ) {
			var list = new List<CellPlaceholder>();
			for( var i = 0; i < fieldCount; i++ )
				list.Add( null );
			cellPlaceholderListsForItems.Add( list );
		}

		internal static IEnumerable<WebControl> BuildRows(
			List<List<CellPlaceholder>> cellPlaceholderListsForRows, ReadOnlyCollection<EwfTableFieldOrItemSetup> rowSetups, bool? useContrastForFirstRow,
			ReadOnlyCollection<EwfTableFieldOrItemSetup> columns, int firstDataColumnIndex, bool tableIsColumnPrimary ) {
			return cellPlaceholderListsForRows.Select(
				( row, rowIndex ) => {
					var rowControl = new WebControl( HtmlTextWriterTag.Tr );
					var rowClickScript = rowSetups[ rowIndex ].ClickScript;
					if( rowClickScript != null )
						rowClickScript.SetUpClickableControl( rowControl );
					rowControl.Height = rowSetups[ rowIndex ].Size;
					rowControl.CssClass =
						rowControl.CssClass.ConcatenateWithSpace(
							useContrastForFirstRow.HasValue && ( ( rowIndex % 2 == 1 ) ^ useContrastForFirstRow.Value ) ? EwfTable.CssElementCreator.ContrastClass : "" );
					rowControl.CssClass = rowControl.CssClass.ConcatenateWithSpace( StringTools.ConcatenateWithDelimiter( " ", rowSetups[ rowIndex ].Classes.ToArray() ) );
					return
						rowControl.AddControlsReturnThis(
							row.Select( ( cell, colIndex ) => new { Cell = cell as EwfTableCell, ColumnIndex = colIndex } )
								.Where( cellAndIndex => cellAndIndex.Cell != null )
								.Select(
									cellAndIndex => {
										var cellControl = new WebControl( cellAndIndex.ColumnIndex < firstDataColumnIndex ? HtmlTextWriterTag.Th : HtmlTextWriterTag.Td );

										var rowSpan = tableIsColumnPrimary ? cellAndIndex.Cell.FieldSpan : cellAndIndex.Cell.ItemSpan;
										if( rowSpan != 1 )
											cellControl.Attributes.Add( "rowspan", rowSpan.ToString() );

										var colSpan = tableIsColumnPrimary ? cellAndIndex.Cell.ItemSpan : cellAndIndex.Cell.FieldSpan;
										if( colSpan != 1 )
											cellControl.Attributes.Add( "colspan", colSpan.ToString() );

										var rowSetup = rowSetups[ rowIndex ];
										var columnSetup = columns[ cellAndIndex.ColumnIndex ];
										var clickScript = cellAndIndex.Cell.ClickScript ?? ( tableIsColumnPrimary || rowSetup.ClickScript == null ? columnSetup.ClickScript : null );
										if( clickScript != null )
											clickScript.SetUpClickableControl( cellControl );

										var columnClassString = StringTools.ConcatenateWithDelimiter( " ", columnSetup.Classes.ToArray() );
										var cellClassString = cellAndIndex.Cell.CssClass;
										cellControl.CssClass = StringTools.ConcatenateWithDelimiter(
											" ",
											cellControl.CssClass,
											EwfTable.CssElementCreator.AllCellAlignmentsClass,
											textAlignmentClass( cellAndIndex.Cell, rowSetup, columnSetup ),
											verticalAlignmentClass( rowSetup, columnSetup ),
											columnClassString,
											cellClassString );

										if( ( rowSetup.ToolTipControl != null || rowSetup.ToolTip.Length > 0 ) && cellAndIndex.ColumnIndex == 0 )
											new ToolTip( rowSetup.ToolTipControl ?? ToolTip.GetToolTipTextControl( rowSetup.ToolTip ), rowControl );
										if( columnSetup.ToolTipControl != null )
											throw new ApplicationException( "A column cannot have a tool tip control because there is no way to clone this control to put it on every cell." );
										if( columnSetup.ToolTip.Length > 0 )
											new ToolTip( ToolTip.GetToolTipTextControl( columnSetup.ToolTip ), cellControl );
										if( cellAndIndex.Cell.ToolTipControl != null || cellAndIndex.Cell.ToolTip.Length > 0 )
											new ToolTip( cellAndIndex.Cell.ToolTipControl ?? ToolTip.GetToolTipTextControl( cellAndIndex.Cell.ToolTip ), cellControl );

										return cellControl.AddControlsReturnThis( cellAndIndex.Cell.Controls ) as Control;
									} ) );
				} );
		}

		private static string textAlignmentClass( EwfTableCell cell, EwfTableFieldOrItemSetup row, EwfTableFieldOrItemSetup column ) {
			// NOTE: Think about whether the row or the column should win if nothing is specified on the cell.
			var alignments = new[] { cell.TextAlignment, row.TextAlignment, column.TextAlignment };
			return ( from i in alignments select TextAlignmentStatics.Class( i ) ).FirstOrDefault( i => i.Length > 0 ) ?? "";
		}

		private static string verticalAlignmentClass( EwfTableFieldOrItemSetup row, EwfTableFieldOrItemSetup column ) {
			// NOTE: Think about whether the row or the column should win.
			var alignments = new[] { row.VerticalAlignment, column.VerticalAlignment };
			return ( from i in alignments select TableCellVerticalAlignmentOps.Class( i ) ).FirstOrDefault( i => i.Length > 0 ) ?? "";
		}
	}
}