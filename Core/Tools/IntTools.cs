using System;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// Extension methods for integers.
	/// </summary>
	public static class IntTools {
		/// <summary>
		/// Executes action n times.
		/// </summary>
		public static void Times( this int n, Action action ) {
			for( var i = 0; i < n; i++ )
				action();
		}

		/// <summary>
		/// Executes action n times. The zero-based index of the iteration is passed to action.
		/// </summary>
		public static void Times( this int n, Action<int> action ) {
			for( var i = 0; i < n; i++ )
				action( i );
		}

		/// <summary>
		/// Formats this integer using the M suffix if it's at least one million and the k suffix if it's at least one thousand. Doesn't work with negative
		/// integers.
		/// </summary>
		public static string ToKiloOrMegaString( this int n, bool formatAsCurrency ) {
			var formatString = formatAsCurrency ? "c0" : "n0";
			if( n >= 1000000 )
				return ( n / 1000000m ).ToString( formatString ) + "M";
			if( n >= 1000 )
				return ( n / 1000m ).ToString( formatString ) + "k";
			return n.ToString( formatString );
		}
	}
}