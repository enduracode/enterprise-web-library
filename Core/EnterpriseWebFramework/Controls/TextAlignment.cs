namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A text alignment for a block.
	/// </summary>
	public enum TextAlignment {
		/// <summary>
		/// No alignment is specified.
		/// </summary>
		NotSpecified,

		/// <summary>
		/// Inline controls are left aligned. See http://www.w3.org/TR/CSS2/text.html#alignment-prop.
		/// </summary>
		Left,

		/// <summary>
		/// Inline controls are right aligned. See http://www.w3.org/TR/CSS2/text.html#alignment-prop.
		/// </summary>
		Right,

		/// <summary>
		/// Inline controls are center aligned. See http://www.w3.org/TR/CSS2/text.html#alignment-prop.
		/// </summary>
		Center,

		/// <summary>
		/// Inline controls are made flush with both sides. See http://www.w3.org/TR/CSS2/text.html#alignment-prop.
		/// </summary>
		Justify
	}

	internal static class TextAlignmentStatics {
		// These are used by EWF CSS files for alignment rules.
		private const string leftCssClass = "ewfTaL";
		private const string rightCssClass = "ewfTaR";
		private const string centerCssClass = "ewfTaC";
		private const string justifyCssClass = "ewfTaJ";

		internal static string Class( TextAlignment textAlignment ) {
			switch( textAlignment ) {
				case TextAlignment.Left:
					return leftCssClass;
				case TextAlignment.Right:
					return rightCssClass;
				case TextAlignment.Center:
					return centerCssClass;
				case TextAlignment.Justify:
					return justifyCssClass;
				default:
					return "";
			}
		}
	}
}