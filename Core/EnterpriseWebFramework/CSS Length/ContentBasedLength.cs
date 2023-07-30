#nullable disable
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A font-relative or absolute length.
	/// </summary>
	public class ContentBasedLength: CssLength {
		private readonly string value;

		internal ContentBasedLength( string value ) {
			this.value = value;
		}

		string CssLength.Value => value;
	}

	public static class ContentBasedLengthExtensionCreators {
		/// <summary>
		/// Creates a CSS em length from this number.
		/// </summary>
		public static ContentBasedLength ToEm( this decimal number ) {
			return new ContentBasedLength( "{0}em".FormatWith( number ) );
		}

		/// <summary>
		/// Creates a CSS em length from this number.
		/// </summary>
		public static ContentBasedLength ToEm( this int number ) {
			return ( (decimal)number ).ToEm();
		}

		/// <summary>
		/// Creates a CSS pixel length from this number.
		/// </summary>
		public static ContentBasedLength ToPixels( this decimal number ) {
			return new ContentBasedLength( "{0}px".FormatWith( number ) );
		}

		/// <summary>
		/// Creates a CSS pixel length from this number.
		/// </summary>
		public static ContentBasedLength ToPixels( this int number ) {
			return ( (decimal)number ).ToPixels();
		}
	}
}