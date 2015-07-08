using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Represents a line of text from a CSV file that has been parsed into fields that
	/// are accessible through the indexers of this object.
	/// </summary>
	public class ParsedLine {
		private IDictionary columnHeadersToIndexes;
		private readonly List<string> fields;
		private int? lineNumber;

		/// <summary>
		/// Returns true if any field on this line has a non-empty, non-whitespace value.
		/// </summary>
		public bool ContainsData { get; private set; }

		/// <summary>
		/// Returns the line number from the source document that this parsed line was created from.
		/// </summary>
		public int LineNumber {
			get {
				if( lineNumber.HasValue )
					return lineNumber.Value;
				throw new ApplicationException( "Line number has not been initialized and has no meaning." );
				// NOTE: We can get rid of this check when the Parse method in the parser becomes internal.
			}
			internal set { lineNumber = value; }
		}

		internal IDictionary ColumnHeadersToIndexes { set { columnHeadersToIndexes = value ?? new ListDictionary(); } }

		internal ParsedLine( List<string> fields ) {
			this.fields = fields;
			ContainsData = false;
			foreach( var field in fields ) {
				if( !field.IsNullOrWhiteSpace() ) {
					ContainsData = true;
					break;
				}
			}
		}

		internal List<string> Fields { get { return fields; } }

		/// <summary>
		/// Returns the number of fields available in on this line.
		/// </summary>
		public int NumberOfFields { get { return fields.Count; } } // NOTE: Track down everyone who uses this.  Move it up to TabularDataParser if we still need this.

		/// <summary>
		/// Returns the value of the field with the given column index.
		/// Gracefully return empty string when overindexed.  This prevents problems with files that have no value in the last column.
		/// </summary>
		public string this[ int index ] {
			get {
				if( index >= fields.Count )
					return "";
				return fields[ index ];
			}
		}

		/// <summary>
		/// Returns the value of the field with the given column name.
		/// </summary>
		public string this[ string columnName ] {
			get {
				if( columnHeadersToIndexes.Count == 0 )
					throw new InvalidOperationException( "The CSV parser returning this CsvLine was not created with a headerLine with which to populate column names." );

				if( columnName == null )
					throw new ArgumentException( "Column name cannot be null." );

				var index = columnHeadersToIndexes[ columnName.ToLower() ];
				if( index == null ) {
					var keys = "";
					foreach( string key in columnHeadersToIndexes.Keys )
						keys += key + ", ";
					throw new ArgumentException( "Column '" + columnName + "' does not exist.  The columns are: " + keys );
				}

				return this[ (int)index ];
			}
		}

		/// <summary>
		/// Returns a comma-delimited list of fields.
		/// </summary>
		public override string ToString() {
			var text = "";
			foreach( var field in fields )
				text += ", " + field;
			return text.TruncateStart( text.Length - 2 );
		}

		/// <summary>
		/// Returns true if the line contains the given field.
		/// </summary>
		public bool ContainsField( string fieldName ) {
			return new ArrayList( columnHeadersToIndexes.Keys ).Contains( fieldName.ToLower() );
		}
	}
}