#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A horizontal alignment for the items in a flex container.
	/// </summary>
	public enum FlexboxAlignment {
		/// <summary>
		/// No alignment is specified.
		/// </summary>
		NotSpecified,

		/// <summary>
		/// Items are packed toward the start of the line. See https://www.w3.org/TR/css-flexbox-1/#justify-content-property.
		/// </summary>
		Left,

		/// <summary>
		/// Items are packed toward the end of the line. See https://www.w3.org/TR/css-flexbox-1/#justify-content-property.
		/// </summary>
		Right,

		/// <summary>
		/// Items are packed toward the center of the line. See https://www.w3.org/TR/css-flexbox-1/#justify-content-property.
		/// </summary>
		Center,

		/// <summary>
		/// Items are evenly distributed in the line. See https://www.w3.org/TR/css-flexbox-1/#justify-content-property.
		/// </summary>
		Justify
	}

	internal static class FlexboxAlignmentStatics {
		// These are used by EWF CSS files for alignment rules.
		private static readonly ElementClass leftClass = new ElementClass( "ewfFaL" );
		private static readonly ElementClass rightClass = new ElementClass( "ewfFaR" );
		private static readonly ElementClass centerClass = new ElementClass( "ewfFaC" );
		private static readonly ElementClass justifyClass = new ElementClass( "ewfFaJ" );

		internal static ElementClassSet Class( FlexboxAlignment alignment ) {
			switch( alignment ) {
				case FlexboxAlignment.Left:
					return leftClass;
				case FlexboxAlignment.Right:
					return rightClass;
				case FlexboxAlignment.Center:
					return centerClass;
				case FlexboxAlignment.Justify:
					return justifyClass;
				default:
					return ElementClassSet.Empty;
			}
		}
	}
}