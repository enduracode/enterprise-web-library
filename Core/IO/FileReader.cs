using System.IO;
using System.Text;

namespace EnterpriseWebLibrary.IO {
	/// <summary>
	/// Used to transparently read from a file given its path, stream, etc.
	/// </summary>
	public class FileReader {
		/// <summary>
		/// Method that requires the use of an open StreamReader object.
		/// </summary>
		public delegate void StreamReaderMethod( StreamReader streamReader );

		private readonly string filePath;
		private readonly Stream stream;

		/// <summary>
		/// Creates a FileReader to read the file at the given path.
		/// </summary>
		public FileReader( string filePath ) {
			this.filePath = filePath;
		}

		/// <summary>
		/// Creates a FileReader to read from the given stream.
		/// </summary>
		public FileReader( Stream stream ) {
			this.stream = stream;
		}

		/// <summary>
		/// Executes the given method inside an open StreamReader.  The caller is not responsible for opening, closing, or
		/// cleaning up after the StreamReader.
		/// </summary>
		public void ExecuteInStreamReader( StreamReaderMethod method ) {
			using(
				var reader = filePath == null ? new StreamReader( stream, Encoding.Default, true ) : new StreamReader( File.OpenRead( filePath ), Encoding.Default, true ) )
				method( reader );
		}
	}
}