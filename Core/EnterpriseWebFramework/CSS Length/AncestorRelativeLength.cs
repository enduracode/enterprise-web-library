using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A length that is relative to the parent or viewport.
	/// </summary>
	public class AncestorRelativeLength: CssLength {
		private readonly string value;

		internal AncestorRelativeLength( string value ) {
			this.value = value;
		}

		string CssLength.Value => value;
	}

	public static class AncestorRelativeLengthExtensionCreators {
		/// <summary>
		/// Creates a CSS percentage length from this number.
		/// </summary>
		public static AncestorRelativeLength ToPercentage( this decimal number ) {
			return new AncestorRelativeLength( "{0}%".FormatWith( number ) );
		}

		/// <summary>
		/// Creates a CSS percentage length from this number.
		/// </summary>
		public static AncestorRelativeLength ToPercentage( this int number ) {
			return ( (decimal)number ).ToPercentage();
		}
	}
}