using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.IO;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal static class TableStatics {
		// This class name is used by EWF CSS and JavaScript files.
		private static readonly ElementClass activatableElementContainerClass = new ElementClass( "ewfAec" );

		internal static void DrawRow(
			Table table, RowSetup rowSetup, List<CellPlaceholder> rowPlaceholder, List<ColumnSetup> columnSetups, bool tableIsColumnPrimary ) {
			var row = new TableRow();
			rowSetup.UnderlyingTableRow = row;
			table.Rows.Add( row );

			row.CssClass = rowSetup.CssClass;
			if( rowSetup.ActivationBehavior != null )
				rowSetup.ActivationBehavior.SetUpClickableControl( row );

			for( var i = 0; i < rowPlaceholder.Count; i++ ) {
				if( rowPlaceholder[ i ] is EwfTableCell )
					row.Cells.Add( buildCell( rowPlaceholder[ i ] as EwfTableCell, rowSetup, columnSetups[ i ], tableIsColumnPrimary ) );
			}
		}

		private static TableCell buildCell( EwfTableCell ewfCell, RowSetup rowSetup, ColumnSetup columnSetup, bool tableIsColumnPrimary ) {
			var colSpan = tableIsColumnPrimary ? ewfCell.Setup.ItemSpan : ewfCell.Setup.FieldSpan;
			var rowSpan = tableIsColumnPrimary ? ewfCell.Setup.FieldSpan : ewfCell.Setup.ItemSpan;

			var underlyingCell = rowSetup.IsHeader || columnSetup.IsHeader ? new TableHeaderCell() : new TableCell();
			underlyingCell.AddControlsReturnThis( ( ewfCell.Content ?? Enumerable.Empty<FlowComponent>() ).GetControls() );
			if( colSpan == 1 )
				underlyingCell.Width = columnSetup.Width;
			underlyingCell.CssClass = StringTools.ConcatenateWithDelimiter(
				" ",
				TableCssElementCreator.AllCellAlignmentsClass.ClassName,
				columnSetup.CssClassOnAllCells,
				StringTools.ConcatenateWithDelimiter( " ", ewfCell.Setup.Classes.ToArray() ) );
			if( ewfCell.Setup.ActivationBehavior != null )
				ewfCell.Setup.ActivationBehavior.SetUpClickableControl( underlyingCell );

			if( colSpan != 1 )
				underlyingCell.ColumnSpan = colSpan;
			if( rowSpan != 1 )
				underlyingCell.RowSpan = rowSpan;

			ewfCell.Setup.EtherealContent.AddEtherealControls( underlyingCell );

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

		internal static List<List<CellPlaceholder>> BuildCellPlaceholderListsForItems( IReadOnlyCollection<EwfTableItem> items, int fieldCount ) {
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
				potentialCellPlaceholderCountForItem += itemCells.Sum( cell => cell.Setup.FieldSpan );

				if( potentialCellPlaceholderCountForItem != fieldCount )
					throw new ApplicationException(
						"Item to be added has " + potentialCellPlaceholderCountForItem + " cells, but should have " + fieldCount + " cells." );

				// Add this item's cells and any necessary spaces to cellPlaceholderListsForItems.
				var fieldIndex = 0;
				foreach( var cell in itemCells ) {
					while( cellPlaceholdersForItem[ fieldIndex ] != null )
						fieldIndex += 1;
					for( var fieldSpanIndex = 0; fieldSpanIndex < cell.Setup.FieldSpan; fieldSpanIndex += 1 ) {
						for( var itemSpanIndex = 0; itemSpanIndex < cell.Setup.ItemSpan; itemSpanIndex += 1 ) {
							if( itemIndex + itemSpanIndex >= cellPlaceholderListsForItems.Count )
								addCellPlaceholderListForItem( cellPlaceholderListsForItems, fieldCount );
							if( cellPlaceholderListsForItems[ itemIndex + itemSpanIndex ][ fieldIndex ] != null )
								throw new ApplicationException( "Two cells spanning multiple fields and/or items have overlapped." );
							cellPlaceholderListsForItems[ itemIndex + itemSpanIndex ][ fieldIndex ] = itemSpanIndex == 0 && fieldSpanIndex == 0
								                                                                          ? (CellPlaceholder)cell
								                                                                          : new SpaceForMultiColOrRowCell();
						}
						fieldIndex += 1;
					}
				}

				itemIndex += 1;
			}

			if( cellPlaceholderListsForItems.Count != items.Count )
				// Since every item must have at least one cell, this message assumes that the first count above is never less than the second.
				throw new ApplicationException( "A cell has overflowed the table." );

			return cellPlaceholderListsForItems;
		}

		private static void addCellPlaceholderListForItem( List<List<CellPlaceholder>> cellPlaceholderListsForItems, int fieldCount ) {
			var list = new List<CellPlaceholder>();
			for( var i = 0; i < fieldCount; i++ )
				list.Add( null );
			cellPlaceholderListsForItems.Add( list );
		}

		internal static IEnumerable<FlowComponent> BuildRows(
			List<List<CellPlaceholder>> cellPlaceholderListsForRows, IReadOnlyList<EwfTableFieldOrItemSetup> rowSetups, bool? useContrastForFirstRow,
			IReadOnlyList<EwfTableFieldOrItemSetup> columns, int firstDataColumnIndex, bool tableIsColumnPrimary ) {
			return cellPlaceholderListsForRows.Select(
				( row, rowIndex ) => {
					var rowSetup = rowSetups[ rowIndex ];
					var rowActivationBehavior = rowSetup.ActivationBehavior;
					return new FlowIdContainer(
						ElementActivationBehavior.GetActivatableElement(
								"tr",
								( useContrastForFirstRow.HasValue && ( ( rowIndex % 2 == 1 ) ^ useContrastForFirstRow.Value )
									  ? TableCssElementCreator.ContrastClass
									  : ElementClassSet.Empty ).Add( rowSetup.Classes ),
								rowSetup.Size != null
									? Tuple.Create( "style", "height: {0}".FormatWith( rowSetup.Size.Value ) ).ToCollection()
									: Enumerable.Empty<Tuple<string, string>>().Materialize(),
								rowActivationBehavior,
								row.Select( ( cell, colIndex ) => new { Cell = cell as EwfTableCell, ColumnIndex = colIndex } )
									.Where( cellAndIndex => cellAndIndex.Cell != null )
									.Select(
										cellAndIndex => {
											var columnSetup = columns[ cellAndIndex.ColumnIndex ];
											var cellSetup = cellAndIndex.Cell.Setup;

											var attributes = new List<Tuple<string, string>>();
											var rowSpan = tableIsColumnPrimary ? cellSetup.FieldSpan : cellSetup.ItemSpan;
											if( rowSpan != 1 )
												attributes.Add( Tuple.Create( "rowspan", rowSpan.ToString() ) );
											var colSpan = tableIsColumnPrimary ? cellSetup.ItemSpan : cellSetup.FieldSpan;
											if( colSpan != 1 )
												attributes.Add( Tuple.Create( "colspan", colSpan.ToString() ) );

											var cellActivationBehavior = cellSetup.ActivationBehavior ??
											                             ( tableIsColumnPrimary || rowActivationBehavior == null ? columnSetup.ActivationBehavior : null );
											return ElementActivationBehavior.GetActivatableElement(
												cellAndIndex.ColumnIndex < firstDataColumnIndex ? "th" : "td",
												TableCssElementCreator.AllCellAlignmentsClass.Add( textAlignmentClass( cellAndIndex.Cell, rowSetup, columnSetup ) )
													.Add( verticalAlignmentClass( rowSetup, columnSetup ) )
													.Add( cellSetup.ContainsActivatableElements ? activatableElementContainerClass : ElementClassSet.Empty )
													.Add( columnSetup.Classes )
													.Add( cellSetup.Classes.Aggregate( ElementClassSet.Empty, ( set, i ) => set.Add( new ElementClass( i ) ) ) ),
												attributes,
												cellActivationBehavior,
												cellAndIndex.Cell.Content,
												cellSetup.EtherealContent );
										} )
									.Materialize(),
								Enumerable.Empty<EtherealComponent>().Materialize() )
							.ToCollection() );
				} );
		}

		private static ElementClassSet textAlignmentClass( EwfTableCell cell, EwfTableFieldOrItemSetup row, EwfTableFieldOrItemSetup column ) {
			// NOTE: Think about whether the row or the column should win if nothing is specified on the cell.
			var alignments = new TextAlignment?[] { cell.Setup.TextAlignment, row.TextAlignment, column.TextAlignment };
			return TextAlignmentStatics.Class( alignments.FirstOrDefault( i => i != TextAlignment.NotSpecified ) ?? TextAlignment.NotSpecified );
		}

		private static ElementClassSet verticalAlignmentClass( EwfTableFieldOrItemSetup row, EwfTableFieldOrItemSetup column ) {
			// NOTE: Think about whether the row or the column should win.
			var alignments = new TableCellVerticalAlignment?[] { row.VerticalAlignment, column.VerticalAlignment };
			return TableCellVerticalAlignmentOps.Class(
				alignments.FirstOrDefault( i => i != TableCellVerticalAlignment.NotSpecified ) ?? TableCellVerticalAlignment.NotSpecified );
		}

		internal static Action<ExcelWorksheet> GetExcelRowAdder( bool rowIsHeader, IReadOnlyCollection<EwfTableCell> cells ) =>
			worksheet => {
				if( cells.Any( i => i.Setup.FieldSpan != 1 || i.Setup.ItemSpan != 1 ) )
					throw new ApplicationException( "Export to Excel does not currently support cells that span multiple columns or rows." );

				if( rowIsHeader )
					worksheet.AddHeaderToWorksheet( cells.Select( i => ( (CellPlaceholder)i ).SimpleText ).ToArray() );
				else
					worksheet.AddRowToWorksheet( cells.Select( i => ( (CellPlaceholder)i ).SimpleText ).ToArray() );
			};

		internal static void AssertAtLeastOneCellPerField( IReadOnlyCollection<EwfTableField> fields, List<List<CellPlaceholder>> cellPlaceholderListsForItems ) {
			// If there is absolutely nothing in the table, we must bypass the assertion since it will always throw an exception.
			if( !cellPlaceholderListsForItems.Any() )
				return;

			// Enforce that there is at least one cell in each field by looking at array of all items.
			for( var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex += 1 ) {
				if( !cellPlaceholderListsForItems.Select( i => i[ fieldIndex ] ).OfType<EwfTableCell>().Any() )
					throw new ApplicationException( "The field with index " + fieldIndex + " does not have any cells." );
			}
		}
	}
}