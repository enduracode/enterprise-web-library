namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A horizontal alignment for an item in a grid cell.
	/// </summary>
	public enum GridAlignment {
		/// <summary>
		/// No alignment is specified.
		/// </summary>
		NotSpecified,

		/// <summary>
		/// Aligns the item to be flush with the cell’s left edge. See https://www.w3.org/TR/css-grid-1/#row-align.
		/// </summary>
		Left,

		/// <summary>
		/// Aligns the item to be flush with the cell’s right edge. See https://www.w3.org/TR/css-grid-1/#row-align.
		/// </summary>
		Right,

		/// <summary>
		/// Centers the item within its cell. See https://www.w3.org/TR/css-grid-1/#row-align.
		/// </summary>
		Center,

		/// <summary>
		/// Stretches the item to fill the cell. See https://www.w3.org/TR/css-grid-1/#row-align.
		/// </summary>
		Stretch
	}

	internal static class GridAlignmentStatics {
		// These are used by EWF CSS files for alignment rules.
		private static readonly ElementClass leftClass = new ElementClass( "ewfGaL" );
		private static readonly ElementClass rightClass = new ElementClass( "ewfGaR" );
		private static readonly ElementClass centerClass = new ElementClass( "ewfGaC" );
		private static readonly ElementClass stretchClass = new ElementClass( "ewfGaS" );

		internal static ElementClassSet Class( GridAlignment alignment ) {
			switch( alignment ) {
				case GridAlignment.Left:
					return leftClass;
				case GridAlignment.Right:
					return rightClass;
				case GridAlignment.Center:
					return centerClass;
				case GridAlignment.Stretch:
					return stretchClass;
				default:
					return ElementClassSet.Empty;
			}
		}
	}
}