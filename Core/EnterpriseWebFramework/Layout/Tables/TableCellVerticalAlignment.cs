namespace EnterpriseWebLibrary.EnterpriseWebFramework {
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
		// These are used by EWF CSS files for alignment rules.
		private static readonly ElementClass topCssClass = new ElementClass( "ewfTcTop" );
		private static readonly ElementClass middleCssClass = new ElementClass( "ewfTcMiddle" );
		private static readonly ElementClass bottomCssClass = new ElementClass( "ewfTcBottom" );
		private static readonly ElementClass baseLineCssClass = new ElementClass( "ewfTcBaseLine" );

		internal static ElementClassSet Class( TableCellVerticalAlignment verticalAlignment ) {
			switch( verticalAlignment ) {
				case TableCellVerticalAlignment.Top:
					return topCssClass;
				case TableCellVerticalAlignment.Middle:
					return middleCssClass;
				case TableCellVerticalAlignment.Bottom:
					return bottomCssClass;
				case TableCellVerticalAlignment.BaseLine:
					return baseLineCssClass;
				default:
					return ElementClassSet.Empty;
			}
		}
	}
}