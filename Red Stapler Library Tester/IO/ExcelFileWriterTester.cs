using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibraryTester.IO {
	[ TestFixture ]
	internal class ExcelFileWriterTester {
		private string timestampPrefix;
		private string outputFolderPath;

		[ TestFixtureSetUp ]
		public void InitializeFixture() {
			// Make sure all the tests run have the same prefix
			timestampPrefix = "test_run_" + DateTime.Now.ToString( "yyyy_MM_dd_HH_MM_ss_" );
			outputFolderPath =
				Directory.CreateDirectory( Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ), "Excel Writer Output" ) ).FullName;
		}

		[ Test ]
		public void TestAutofitOnSave() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure autofit works." );
					writer.AutofitOnSave = true;
					addAutofitTestRows( writer, "autofitted" );
					return "autofit_yes_testing";
				} );
			runTest(
				writer => {
					Console.WriteLine( "Making sure not autofitting works." );
					writer.AutofitOnSave = false;
					addAutofitTestRows( writer, "not autofit" );
					return "autofit_no_testing";
				} );
		}

		private static void addAutofitTestRows( ExcelFileWriter writer, string fitOrNot ) {
			writer.DefaultWorksheet.AddHeaderToWorksheet( "This is the default worksheet - hopefully " + fitOrNot );
			writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50", "500", "5000", "5", "5", "5", "5", "50" );
			writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50", "500", "5000", "5", "5", "5", "5", "50" );
			writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50", "500", "5000", "5", "5", "5", "5", "50" );
			writer.DefaultWorksheet.AddRowToWorksheet(
				"500",
				"5000",
				"5",
				"50",
				"500",
				"5000",
				"5",
				"Longer than expected cell value here oh yeah it is.  Super long in fact." );
		}

		[ Test ]
		public void TestNoDefaultWorksheet() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure having no default worksheet works." );
					return "no_default_worksheet_testing";
				},
				false );
		}

		[ Test ]
		public void TestDefaultWorksheet() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure default worksheet works." );

					writer.DefaultWorksheet.AddHeaderToWorksheet( "This is the default worksheet" );
					writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50" );

					return "default_worksheet_testing";
				} );
		}

		[ Test ]
		public void TestRenameDefaultWorksheet() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure renaming the default worksheet works." );

					writer.DefaultWorksheet.AddHeaderToWorksheet( "This is the default worksheet" );
					writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.DefaultWorksheet.AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.DefaultWorksheet.Name = "I'm not Sheet1.";

					return "default_worksheet_renamed_testing";
				} );
		}

		[ Test ]
		public void TestNamedWorksheet() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure named worksheets work." );

					const string worksheetName = "Worksheet Name Test";

					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet();
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );

					return "named_worksheet_testing";
				} );
		}

		[ Test ]
		[ ExpectedException ]
		public void TestReAddingSameWorksheet() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure adding the same worksheet name doesn't create multiples." );

					const string worksheetName = "Worksheet";

					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );

					return "same_sheet_name_testing";
				} );
		}

		[ Test ]
		[ ExpectedException ]
		public void TestReAddingSameWorksheetMinusSpaces() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure adding the same worksheet name without spaces doesn't create multiples." );

					const string worksheetName = "Worksheet Name With Spaces";

					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.AddWorksheet( worksheetName.RemoveCommonNonAlphaNumericCharacters() ).AddRowToWorksheet( "500", "5000", "5", "50" );

					return "same_sheet_name_testing";
				} );
		}

		[ Test ]
		public void TestMultipleNamedWorksheet() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure multiple named worksheets and default worksheets work." );

					const string worksheetName = "Worksheet Name Test";
					const string otherWorksheetName = "Other";
					const string blankWorksheetName = "Blank";

					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );

					writer.DefaultWorksheet.AddHeaderToWorksheet( "This is the default worksheet" );

					writer.AddWorksheet( otherWorksheetName ).AddHeaderToWorksheet( "Another worksheet" );
					writer.AddWorksheet( blankWorksheetName );

					return "multiple_named_worksheets_testing";
				} );
		}

		[ Test ]
		public void TestNamedCells() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure naming cells works." );

					const string worksheetName = "Cells";

					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet();

					writer.GetWorksheetByName( worksheetName ).PutCellValue( "A4", "Cell A4" );
					writer.GetWorksheetByName( worksheetName ).PutCellValue( "B4", "Cell B4" );

					writer.GetWorksheetByName( worksheetName ).PutCellValue( "A5", "Cell A5" );
					writer.GetWorksheetByName( worksheetName ).PutCellValue( "B5", "Cell B5" );

					writer.GetWorksheetByName( worksheetName ).PutCellValue( "A6", "Cell A6" );
					writer.GetWorksheetByName( worksheetName ).PutCellValue( "B6", "Cell B6" );

					writer.GetWorksheetByName( worksheetName ).PutCellValue( "J16", "Cell J16" );

					return "named_cell_testing";
				} );
		}

		[ Test ]
		public void TestGetSafeFileName() {
			/* First assert that the output's what's expected. */
			Console.WriteLine( "Making sure safe filename works." );
			var writer = new ExcelFileWriter();
			const string fileName = "get safe filename testing :" + "_gibberish_here_*$#*&(#@)*(?|\\/@#_end_gibberish";
			var safeFileName = writer.GetSafeFileName( fileName );
			Assert.AreEqual( safeFileName, "GetSafeFilenameTesting_Gibberish_here_$#&(#@)(@#_end_gibberish.xlsx" );

			/* Then actually write it so we're sure the filesystem accepts it too. */
			runTest( actualWriter => fileName );
		}

		[ Test ]
		public void TestLockedHeader() {
			runTest(
				writer => {
					const string worksheetName = "Locker";

					var oneToTen = Enumerable.Range( 1, 10 ).ToArray();
					var oneToOneOhOh = Enumerable.Range( 1, 100 ).ToArray();
					writer.AddWorksheet( worksheetName ).AddHeaderToWorksheet( oneToTen.Select( i => "This is the header for column " + i ).ToArray() );
					foreach( var row in oneToOneOhOh ) {
						var rowText = "The row " + row + " lines above is the {0} column's header row.";
						writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet( oneToTen.Select( i => rowText.FormatWith( i + ordinalEnding( i ) ) ).ToArray() );
					}
					writer.GetWorksheetByName( worksheetName ).FreezeHeaderRow();

					return "locked_header_testing";
				} );
		}

		[ Test ]
		public void TestFormulae() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure a formula works." );

					const string worksheetName = "Formula";

					writer.AddWorksheet( worksheetName ).AddRowToWorksheet( "500", "5000", "5", "50" );
					writer.GetWorksheetByName( worksheetName ).AddRowToWorksheet();

					writer.GetWorksheetByName( worksheetName ).PutCellValue( "A4", "Sum (Should be 5555):" );
					writer.GetWorksheetByName( worksheetName ).PutFormula( "B4", "=SUM(A1:D1)" );

					writer.GetWorksheetByName( worksheetName ).PutCellValue( "A5", "Average (Should be 1388.75):" );
					writer.GetWorksheetByName( worksheetName ).PutFormula( "B5", "=AVERAGE(A1:D1)" );

					writer.GetWorksheetByName( worksheetName ).PutCellValue( "A6", "Average Including 6 Blanks (Should be 1388.75 (and not 555.5)):" );
					writer.GetWorksheetByName( worksheetName ).PutFormula( "B6", "=AVERAGE(A1:J1)" );

					return "formula_testing";
				} );
		}

		[ Test ]
		public void TestSupportedDataTypes() {
			runTest(
				writer => {
					const string str = "This is a test string";
					const string url = "http://www.google.com";
					const string email = "bob@bob.bob";
					const double @double = 1.23;
					var @dateTime = DateTime.Now.ToString();

					Console.WriteLine( "Writing supported data types to the worksheet." );

					writer.DefaultWorksheet.AddHeaderToWorksheet(
						"string '{0}', url '{3}', email '{4}', double '{1}', DateTime '{2}' to the worksheet.".FormatWith( str, @double, @dateTime, url, email ) );
					writer.DefaultWorksheet.AddRowToWorksheet( str );
					writer.DefaultWorksheet.AddRowToWorksheet( url );
					writer.DefaultWorksheet.AddRowToWorksheet( email );
					writer.DefaultWorksheet.AddRowToWorksheet( @double.ToString() );
					writer.DefaultWorksheet.AddRowToWorksheet( @dateTime );

					return "supported_data_types";
				} );
		}

		[ Test ]
		public void TestWritingManyRows() {
			runTest(
				writer => {
					Console.WriteLine( "Inserting counter value from the first 20 columns (A - T), for 1000 columns." );

					for( var counter = 0; counter < 1000; counter++ )
						writer.DefaultWorksheet.AddRowToWorksheet( Enumerable.Range( 0, 20 ).Select( i => counter++.ToString() ).ToArray() );
					return "writing_many_rows";
				} );
		}

		[ Test ]
		public void TestWritingManyColumns() {
			runTest(
				writer => {
					Console.WriteLine( "Writing a sheet with 20 rows and 1000 columns (excel's max is theoretically 255)." );

					var columns = Enumerable.Range( 0, 1000 ).ToList();
					for( var row = 0; row < 20; row++ )
						writer.DefaultWorksheet.AddRowToWorksheet( columns.Select( i => ( row * columns.Max() ) + i.ToString() ).ToArray() );
					return "writing_many_columns";
				} );
		}

		[ Test ]
		[ Ignore( "Long-Running Test" ) ]
		public void TestWritingManyRowsAndColumns() {
			runTest(
				writer => {
					Console.WriteLine( "Writing a sheet with 1000 rows and 1000 columns." );

					var columns = Enumerable.Range( 0, 1000 ).ToList();
					for( var row = 0; row < 1000; row++ )
						writer.DefaultWorksheet.AddRowToWorksheet( columns.Select( i => ( row * columns.Max() ) + i.ToString() ).ToArray() );
					return "writing_many_rows_and_columns";
				} );
		}

		[ Test ]
		public void TestHeaderRow() {
			runTest(
				writer => {
					Console.WriteLine( "Making sure basic header rows work." );

					var oneToTen = Enumerable.Range( 1, 10 ).ToArray();
					writer.DefaultWorksheet.AddHeaderToWorksheet( oneToTen.Select( i => "This is the header for column " + i ).ToArray() );
					writer.DefaultWorksheet.AddRowToWorksheet( oneToTen.Select( i => "The row above is the " + i + ordinalEnding( i ) + " column's header row." ).ToArray() );
					writer.DefaultWorksheet.AddRowToWorksheet(
						oneToTen.Select( i => "The row 2 lines above is the " + i + ordinalEnding( i ) + " column's header row." ).ToArray() );

					return "header_row";
				} );
		}

		[ Test ]
		public void TestTooManyHeaderRows() {
			runTest(
				writer => {
					Console.WriteLine( "Writing a sheet with header rows in the middle of data rows." );

					var oneToTen = Enumerable.Range( 1, 10 ).ToList();
					writer.DefaultWorksheet.AddHeaderToWorksheet( oneToTen.Select( i => "This is the header for column " + i ).ToArray() );
					writer.DefaultWorksheet.AddRowToWorksheet(
						oneToTen.Select( i => "The row above is the header row for the " + i + ordinalEnding( i ) + " column." ).ToArray() );
					writer.DefaultWorksheet.AddRowToWorksheet( oneToTen.Select( i => "The row 2 lines above is the " + i + ordinalEnding( i ) + " column." ).ToArray() );
					writer.DefaultWorksheet.AddHeaderToWorksheet( oneToTen.Select( i => "This is the second header row column " + i ).ToArray() );
					writer.DefaultWorksheet.AddHeaderToWorksheet( oneToTen.Select( i => "This is the third header row column " + i ).ToArray() );
					writer.DefaultWorksheet.AddHeaderToWorksheet( oneToTen.Select( i => "This is the fourth header row column " + i ).ToArray() );
					writer.DefaultWorksheet.AddRowToWorksheet(
						oneToTen.Select( i => "The row above is the fourth header for the " + i + ordinalEnding( i ) + " column." ).ToArray() );

					return "too_many_header_rows";
				} );
		}

		[ Test ]
		public void RealWorldDataTableTest() {
			// This is what a dynamic table would output, a list of rowsetups.
			var rowSetups = new List<RowSetup>();
			rowSetups.Add( new RowSetup { IsHeader = true, CsvLine = new List<string> { "Name", "Company", "Location", "Url", "Phone" } } );

			var names = new[] { "Sofeee", "Cathie", "Ann", "Jan", "Patty", "Cora" };
			var surNames = new[] { "Norjaim", "Carbelt", "Wilson", "Middleberger", "Hewlett", "Taylor", "Danzen" };
			var companies = new[] { "Synergy", "Enterprises", "Dancing", "Buzz", "Local", "of Rochester" };
			var streets = new[] { "Broad Rd", "W Elm St", "Main", "James Ave", "Water Blvd", "West Way" };

			for( var i = 0; i < 72; i++ ) {
				rowSetups.Add(
					new RowSetup
						{
							CsvLine = new List<string>
								{
									// Some random-looking values
									names[ i % names.Count() ] + " " + surNames[ i % surNames.Count() ],
									companies[ ( i + ( i / 2 ) ) % companies.Count() ] + " " + companies[ i % companies.Count() ],
									i + " " + streets[ i % streets.Count() ],
									"http://www.google.com/search?q=" + i,
									i.ToString( "D3" ) + "-867-5309"
								},
							ClickScript = ClickScript.CreateRedirectScript( new ExternalPageInfo( "http://google.com" ) ),
							CssClass = "gibberish_string_for_testing",
							IsHeader = false,
							RankId = i,
							ToolTip = "This is row " + i,
							UniqueIdentifier = "row" + i
						} );
			}

			runTest(
				writer => {
					writer.UseLegacyExcelFormat = true;
					foreach( var rowSetup in rowSetups ) {
						if( rowSetup.IsHeader )
							writer.DefaultWorksheet.AddHeaderToWorksheet( rowSetup.CsvLine.ToArray() );
						else
							writer.DefaultWorksheet.AddRowToWorksheet( rowSetup.CsvLine.ToArray() );
					}
					return "data_table";
				} );
		}

		private void runTest( Func<ExcelFileWriter, string> code, bool includeDefaultWorksheet = true ) {
			var start = DateTime.Now;
			var writer = includeDefaultWorksheet ? new ExcelFileWriter() : new ExcelFileWriter( false );
			var fileName = code( writer );
			var filePath = Path.Combine( outputFolderPath, writer.GetSafeFileName( timestampPrefix + fileName ) );
			var doneCreating = DateTime.Now;
			long size;
			using( var f = File.Create( filePath ) ) {
				writer.SaveToStream( f );
				size = f.Length;
			}
			var doneWriting = DateTime.Now;

			Console.WriteLine( "To view this test file, open '{0}' with Excel.".FormatWith( filePath ) );
			Console.WriteLine(
				"Finished creating {3}kb file in {0}ms, written in another {1}ms, for a total of {2}ms.".FormatWith(
					( doneCreating - start ).TotalMilliseconds,
					( doneWriting - doneCreating ).TotalMilliseconds,
					( doneWriting - start ).TotalMilliseconds,
					( size / 1000 ) ) );
		}

		private static string ordinalEnding( int number ) {
			switch( number % 10 ) {
				case 1:
					return "st";
				case 2:
					return "nd";
				case 3:
					return "rd";
				default:
					return "th";
			}
		}
	}
}