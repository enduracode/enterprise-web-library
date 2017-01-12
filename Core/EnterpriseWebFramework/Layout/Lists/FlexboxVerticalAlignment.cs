namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A vertical alignment for an item in a flex container line.
	/// </summary>
	public enum FlexboxVerticalAlignment {
		/// <summary>
		/// No alignment is specified.
		/// </summary>
		NotSpecified,

		/// <summary>
		/// The top margin edge of the item is placed flush with the top edge of the line. See https://www.w3.org/TR/css-flexbox-1/#align-items-property.
		/// </summary>
		Top,

		/// <summary>
		/// The bottom margin edge of the item is placed flush with the bottom edge of the line. See https://www.w3.org/TR/css-flexbox-1/#align-items-property.
		/// </summary>
		Bottom,

		/// <summary>
		/// The item's margin box is centered vertically within the line. See https://www.w3.org/TR/css-flexbox-1/#align-items-property.
		/// </summary>
		Center,

		/// <summary>
		/// The item participates in baseline alignment: all participating items on the line are aligned such that their baselines align. See
		/// https://www.w3.org/TR/css-flexbox-1/#align-items-property.
		/// </summary>
		Baseline
	}

	internal static class FlexboxVerticalAlignmentStatics {
		// These are used by EWF CSS files for alignment rules.
		private static readonly ElementClass topClass = new ElementClass( "ewfFvaT" );
		private static readonly ElementClass bottomClass = new ElementClass( "ewfFvaB" );
		private static readonly ElementClass centerClass = new ElementClass( "ewfFvaC" );
		private static readonly ElementClass baselineClass = new ElementClass( "ewfFvaBl" );

		internal static ElementClassSet Class( FlexboxVerticalAlignment alignment ) {
			switch( alignment ) {
				case FlexboxVerticalAlignment.Top:
					return topClass;
				case FlexboxVerticalAlignment.Bottom:
					return bottomClass;
				case FlexboxVerticalAlignment.Center:
					return centerClass;
				case FlexboxVerticalAlignment.Baseline:
					return baselineClass;
				default:
					return ElementClassSet.Empty;
			}
		}
	}
}