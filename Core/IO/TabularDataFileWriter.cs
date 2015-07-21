using System.IO;

namespace EnterpriseWebLibrary.IO {
	/// <summary>
	/// Provides a way to write tabular data to files.
	/// </summary>
	public interface TabularDataFileWriter {
		/// <summary>
		/// Clears the current line.  This does not affect the file at all, it simply undoes any
		/// calls to AddValueToLine made since the last WriteCurrentLineToFile call.
		/// </summary>
		void ClearLine();

		/// <summary>
		/// Add several values to the current line.  Collection can be empty but not null.
		/// Values may be null.  If a value is not null, a call to ToString determines what text is added to the line.
		/// This method may be called repeatedly to add several sets of values to the same line. The line is only advanced
		/// when WriteCurrentLineToFile is called.
		/// </summary>
		void AddValuesToLine( params object[] values );

		/// <summary>
		/// Writes the current line to the file using the given open stream writer.
		/// This clears the current line after writing.
		/// </summary>
		void WriteCurrentLineToFile( StreamWriter writer );
	}
}