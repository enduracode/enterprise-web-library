using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Parses a line of a Microsoft Excel CSV file using the definition of CSV at http://en.wikipedia.org/wiki/Comma-separated_values.
	/// </summary>
	public class CsvLineParser: Parser {
		private readonly IDictionary columnHeadersToIndexes = new Hashtable(); // NOTE: This can be removed once the parse method is internal

		/// <summary>
		/// Creates a line parser with no header row.  Fields will be access via indexes rather than by column name.
		/// </summary>
		public CsvLineParser() {}

		/// <summary>
		/// Creates a line parser with a header row.  The column names are extracted from the header row, and
		/// parsed CsvLines will allow field access through column name or column index.
		/// </summary>
		public CsvLineParser( string headerLine ) {
			var index = 0;
			foreach( var columnHeader in Parse( headerLine ).Fields ) {
				columnHeadersToIndexes[ columnHeader.ToLower() ] = index;
				index++;
			}
		}

		/// <summary>
		/// Parses a line of a Microsoft Excel CSV file and returns a collection of string fields.
		/// Internal use only.
		/// Use ParseAndProcessAllLines instead.
		/// </summary>
		public ParsedLine Parse( string line ) {
			var fields = new List<string>();
			if( !line.IsNullOrWhiteSpace() ) {
				using( TextReader tr = new StringReader( line ) )
					parseCommaSeparatedFields( tr, fields );
			}
			var parsedLine = new ParsedLine( fields );
			parsedLine.ColumnHeadersToIndexes = columnHeadersToIndexes; // NOTE: This would be unnecessary if this method were internal
			return parsedLine;
		}

		private static void parseCommaSeparatedFields( TextReader tr, List<string> fields ) {
			parseCommaSeparatedField( tr, fields );
			while( tr.Peek() == ',' ) {
				tr.Read();
				parseCommaSeparatedFields( tr, fields );
			}
		}

		private static void parseCommaSeparatedField( TextReader tr, List<string> fields ) {
			if( tr.Peek() != -1 ) {
				string field;
				if( tr.Peek() != '"' )
					field = parseSimpleField( tr );
				else
					field = parseQuotedField( tr );
				fields.Add( field.Trim() );
			}
		}

		private static string parseSimpleField( TextReader tr ) {
			var sb = new StringBuilder();

			var ch = tr.Peek();
			while( ch != -1 && ch != ',' ) {
				sb.Append( (char)tr.Read() );
				ch = tr.Peek();
			}

			return sb.ToString();
		}

		private static string parseQuotedField( TextReader tr ) {
			var sb = new StringBuilder();

			// Skip the opening quote
			tr.Read();

			var ch = tr.Read();
			// Continue until the end of the file or until we reach an unescaped quote.
			while( ch != -1 && !( ch == '"' && tr.Peek() != '"' ) ) {
				// If we encounter an escaped double quote, skip one of the double quotes.
				if( ch == '"' && tr.Peek() == '"' )
					tr.Read();

				sb.Append( (char)ch );
				ch = tr.Read();
			}

			return sb.ToString();
		}
	}
}