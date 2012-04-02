namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
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
		// These are used by Standard Library CSS files for alignment rules.
		internal const string LeftCssClass = "ewfTaL";
		internal const string RightCssClass = "ewfTaR";
		internal const string CenterCssClass = "ewfTaC";
		internal const string JustifyCssClass = "ewfTaJ";

		internal static string Class( TextAlignment textAlignment ) {
			switch( textAlignment ) {
				case TextAlignment.Left:
					return LeftCssClass;
				case TextAlignment.Right:
					return RightCssClass;
				case TextAlignment.Center:
					return CenterCssClass;
				case TextAlignment.Justify:
					return JustifyCssClass;
				default:
					return "";
			}
		}
	}
}