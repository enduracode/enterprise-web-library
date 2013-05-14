using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Use this to process several lines of any type of tabular data, such as CSVs or fixed-width data files.
	/// </summary>
	public class TabularDataParser {
		/// <summary>
		/// Method that knows how to process a line from a particular file.  The validator is new for each row and has no errors, initially.
		/// </summary>
		public delegate void LineProcessingMethod( DBConnection cn, Validator validator, ParsedLine line );

		private FileReader fileReader;
		private Parser parser;
		private int headerRowsToSkip;
		private bool hasHeaderRow;

		/// <summary>
		/// The number of rows in the file, not including the header rows that were skipped with headerRowsToSkip or hasHeaderRows = true.
		/// This is the number of rows in the file that were parsed.
		/// </summary>
		public int NonHeaderRows { get; private set; }

		/// <summary>
		/// The number of header rows in the file. This is equal to 1 if hasHeaderRow was passed as true, or equal to headerRowsToSkip otherwise.
		/// </summary>
		public int HeaderRows { get { return hasHeaderRow ? 1 : headerRowsToSkip; } }

		/// <summary>
		/// The total number of rows in the file, including any header rows.
		/// </summary>
		public int TotalRows { get { return HeaderRows + NonHeaderRows; } }

		/// <summary>
		/// The number of rows in the file with at least one non-blank field.
		/// This is the number of rows in that file that were processed (the lineHandler callback was performed).
		/// </summary>
		public int RowsContainingData { get; private set; }

		/// <summary>
		/// The number of rows in the file that were processed without encountering any validation errors.
		/// </summary>
		public int RowsWithoutValidationErrors { get; private set; }

		/// <summary>
		/// The number of rows in the file that did encounter validation errors when processed.
		/// </summary>
		public int RowsWithValidationErrors { get { return RowsContainingData - RowsWithoutValidationErrors; } }

		private TabularDataParser() {}

		/// <summary>
		/// Creates a parser designed to parse a file with fixed data column widths. Specify the starting position of each column (using one-based column index).
		/// Characters that take up more than 1 unit of width, such as tabs, can cause problems here.
		/// </summary>
		public static TabularDataParser CreateForFixedWidthFile( string filePath, int headerRowsToSkip, params int[] columnStartPositions ) {
			return new TabularDataParser
			       	{ fileReader = new FileReader( filePath ), headerRowsToSkip = headerRowsToSkip, parser = new FixedWidthParser( columnStartPositions ) };
		}

		/// <summary>
		/// Creates a parser designed to parse a CSV file.  Passing true for hasHeaderRow will result in the first row being used to map
		/// header names to column indices.  This will allow you to access fields using the header name in addition to the column index.
		/// </summary>
		public static TabularDataParser CreateForCsvFile( string filePath, bool hasHeaderRow ) {
			return new TabularDataParser { fileReader = new FileReader( filePath ), hasHeaderRow = hasHeaderRow, parser = new CsvLineParser() };
		}

		/// <summary>
		/// Creates a parser designed to parse a CSV file.  Passing true for hasHeaderRow will result in the first row being used to map
		/// header names to column indices.  This will allow you to access fields using the header name in addition to the column index.
		/// </summary>
		public static TabularDataParser CreateForCsvFile( Stream stream, bool hasHeaderRow ) {
			return new TabularDataParser { fileReader = new FileReader( stream ), hasHeaderRow = hasHeaderRow, parser = new CsvLineParser() };
		}

		/// <summary>
		/// For every line (after headerRowsToSkip) in the file with the given path, calls the line handling method you pass.
		/// The validationErrors collection will hold all validation errors encountered during the processing of all lines.
		/// </summary>
		public void ParseAndProcessAllLines( DBConnection cn, LineProcessingMethod lineHandler, ICollection<ValidationError> validationErrors ) {
			fileReader.ExecuteInStreamReader( delegate( StreamReader reader ) {
				IDictionary columnHeadersToIndexes = null;
				if( hasHeaderRow )
					columnHeadersToIndexes = buildColumnHeadersToIndexesDictionary( reader.ReadLine() );

				for( var i = 0; i < headerRowsToSkip; i++ )
					reader.ReadLine();

				string line;
				for( var lineNumber = HeaderRows + 1; ( line = reader.ReadLine() ) != null; lineNumber++ ) {
					NonHeaderRows++;
					var parsedLine = parser.Parse( line );
					if( parsedLine.ContainsData ) {
						RowsContainingData++;
						parsedLine.LineNumber = lineNumber;
						parsedLine.ColumnHeadersToIndexes = columnHeadersToIndexes;
						var validator = new Validator();
						lineHandler( cn, validator, parsedLine );
						if( validator.ErrorsOccurred ) {
							foreach( var error in validator.Errors )
								validationErrors.Add( new ValidationError( "Line " + lineNumber, error.UnusableValueReturned, error.Message ) );
						}
						else
							RowsWithoutValidationErrors++;
					}
				}
			} );
		}

		private IDictionary buildColumnHeadersToIndexesDictionary( string headerLine ) {
			IDictionary columnHeadersToIndexes = new ListDictionary();
			var index = 0;
			foreach( var columnHeader in parser.Parse( headerLine ).Fields ) {
				columnHeadersToIndexes[ columnHeader.ToLower() ] = index;
				index++;
			}
			return columnHeadersToIndexes;
		}

		//internal static void Test() {
		//  var localPath = StandardLibraryMethods.CombinePaths( Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ), "Salesforce.csv" );
		//  var csvParser = CreateForCsvFile( localPath, true );
		//  var validationErrors = new List<ValidationError>();
		//  // GMS: Can't actually test this because MiniProfiler blows up with a blank database connection.
		//  AppTools.ExecuteInDbConnection( cn => csvParser.ParseAndProcessAllLines( cn, importLine, validationErrors ) );
		//}

		//private static void importLine( DBConnection cn, Validator validator, ParsedLine line ) {}
	}
}