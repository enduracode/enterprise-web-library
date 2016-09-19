using System;
using System.IO;
using System.Linq;

namespace EnterpriseWebLibrary.IO {
	/// <summary>
	/// Helps in writing data to a file in tab-separated values format.
	/// </summary>
	public class TabDelimitedFileWriter: TabularDataFileWriter {
		private const char delimiter = '\t';
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
				line += delimiter;
			else {
				var s = val.ToString();
				if( s.Contains( delimiter ) || s.Contains( Environment.NewLine ) )
					throw new ApplicationException( "The tab-separated values format does not support tabs or newline sequences in a value." );
				line += s + delimiter;
			}
		}

		/// <summary>
		/// Add several values to the current line.  Collection can be empty but not null.
		/// Values may be null.  If a value is not null, a call to ToString determines what text is added to the line.
		/// This method may be called repeatedly to add several sets of values to the same line. The line is only advanced
		/// when WriteCurrentLineToFile is called.
		/// </summary>
		public void AddValuesToLine( params object[] values ) {
			foreach( var val in values )
				AddValueToLine( val );
		}

		/// <summary>
		/// Writes the current line to the file using the given open text writer.
		/// This clears the current line after writing.
		/// </summary>
		public void WriteCurrentLineToFile( TextWriter writer ) {
			line = line.TrimEnd( delimiter );
			writer.WriteLine( line );
			writer.Flush();
			line = "";
		}
	}
}