using System.IO;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// A collection of Stream-related static methods.
	/// </summary>
	public static class StreamTools {
		/// <summary>
		/// Returns the position to the beginning.
		/// </summary>
		public static void Reset( this MemoryStream ms ) {
			resetSeekableStream( ms );
		}

		/// <summary>
		/// Returns the position to the beginning.
		/// </summary>
		public static void Reset( this FileStream ms ) {
			resetSeekableStream( ms );
		}

		/// <summary>
		/// Returns the stream position to the beginning.
		/// This will throw an exception if the stream does not support seeking.
		/// </summary>
		private static void resetSeekableStream( Stream s ) {
			s.Seek( 0, SeekOrigin.Begin );
		}
	}
}