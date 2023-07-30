#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A vertical alignment for an item in a grid cell.
	/// </summary>
	public enum GridVerticalAlignment {
		/// <summary>
		/// No alignment is specified.
		/// </summary>
		NotSpecified,

		/// <summary>
		/// Aligns the item to be flush with the cell’s top edge. See https://www.w3.org/TR/css-grid-1/#column-align.
		/// </summary>
		Top,

		/// <summary>
		/// Aligns the item to be flush with the cell’s bottom edge. See https://www.w3.org/TR/css-grid-1/#column-align.
		/// </summary>
		Bottom,

		/// <summary>
		/// Centers the item within its cell. See https://www.w3.org/TR/css-grid-1/#column-align.
		/// </summary>
		Center,

		/// <summary>
		/// Stretches the item to fill the cell. See https://www.w3.org/TR/css-grid-1/#column-align.
		/// </summary>
		Stretch
	}

	internal static class GridVerticalAlignmentStatics {
		// These are used by EWF CSS files for alignment rules.
		private static readonly ElementClass topClass = new ElementClass( "ewfGvaT" );
		private static readonly ElementClass bottomClass = new ElementClass( "ewfGvaB" );
		private static readonly ElementClass centerClass = new ElementClass( "ewfGvaC" );
		private static readonly ElementClass stretchClass = new ElementClass( "ewfGvaS" );

		internal static ElementClassSet Class( GridVerticalAlignment alignment ) {
			switch( alignment ) {
				case GridVerticalAlignment.Top:
					return topClass;
				case GridVerticalAlignment.Bottom:
					return bottomClass;
				case GridVerticalAlignment.Center:
					return centerClass;
				case GridVerticalAlignment.Stretch:
					return stretchClass;
				default:
					return ElementClassSet.Empty;
			}
		}
	}
}