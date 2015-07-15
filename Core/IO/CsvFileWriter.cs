using System.IO;

namespace EnterpriseWebLibrary.IO {
	/// <summary>
	/// Helps in writing data to a file in CSV format.
	/// </summary>
	public class CsvFileWriter: TabularDataFileWriter {
		private string line = "";

		/// <summary>
		/// Clears the current line.  This does not affect the file at all, it simply undoes any
		/// calls to AddValueToLine made since the last WriteCurrentLineToFile call.
		/// </summary>
		public void ClearLine() {
			line = "";
		}

		/// <summary>
		/// Adds the given value as a column on the current line.  Value may be null.  If
		/// it is not null, val.ToString() determines what is added to the line.
		/// </summary>
		public void AddValueToLine( object val ) {
			if( val == null || val.ToString() == "" )
				line += ",";
			else
				line += "\"" + val.ToString().Replace( "\"", "\"\"" ) + "\",";
		}

		/// <summary>
		/// Add several values to the current line.  Collection can be empty but not null.
		/// </summary>
		public void AddValuesToLine( params object[] values ) {
			foreach( var value in values )
				AddValueToLine( value );
		}

		/// <summary>
		/// Writes the current line to the file using the given open stream writer.
		/// This clears the current line after writing.
		/// </summary>
		public void WriteCurrentLineToFile( StreamWriter writer ) {
			line = line.TrimEnd( ',' );
			writer.WriteLine( line );
			writer.Flush();
			line = "";
		}
	}
}