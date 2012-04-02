namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A vertical alignment for a table cell.
	/// </summary>
	public enum TableCellVerticalAlignment {
		/// <summary>
		/// No alignment is specified.
		/// </summary>
		NotSpecified,

		/// <summary>
		/// Controls are flush with the top of the cell. From http://www.w3.org/TR/html4/struct/tables.html#h-11.3.2.
		/// </summary>
		Top,

		/// <summary>
		/// Controls are centered vertically within the cell. From http://www.w3.org/TR/html4/struct/tables.html#h-11.3.2.
		/// </summary>
		Middle,

		/// <summary>
		/// Controls are flush with the bottom of the cell. From http://www.w3.org/TR/html4/struct/tables.html#h-11.3.2.
		/// </summary>
		Bottom,

		/// <summary>
		/// All cells in the same row as a cell whose alignment has this value should have their text positioned so that the first text line occurs on a base line
		/// common to all cells in the row. This constraint does not apply to subsequent text lines in these cells. From
		/// http://www.w3.org/TR/html4/struct/tables.html#h-11.3.2.
		/// </summary>
		BaseLine
	}

	internal static class TableCellVerticalAlignmentOps {
		internal static string Class( TableCellVerticalAlignment verticalAlignment ) {
			switch( verticalAlignment ) {
				case TableCellVerticalAlignment.Top:
					return EwfTable.CssElementCreator.CellAlignmentTopCssClass;
				case TableCellVerticalAlignment.Middle:
					return EwfTable.CssElementCreator.CellAlignmentMiddleCssClass;
				case TableCellVerticalAlignment.Bottom:
					return EwfTable.CssElementCreator.CellAlignmentBottomCssClass;
				case TableCellVerticalAlignment.BaseLine:
					return EwfTable.CssElementCreator.CellAlignmentBaseLineCssClass;
				default:
					return "";
			}
		}
	}
}