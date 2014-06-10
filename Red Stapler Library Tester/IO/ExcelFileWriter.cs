using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibraryTester.IO {
	[ TestFixture ]
	internal class ExcelFileWriter {
		public string TimestampPrefix;

		[ TestFixtureSetUp ]
		public void InitializeFixture() {
			// Make sure all the tests run have the same prefix
			TimestampPrefix = "test_run_" + DateTime.Now.ToString( "yyyy_MM_dd_HH_MM_ss" ) + "_";
		}

		[ Test ]
		public void TestSupportedDataTypes() {
			runTest(
				writer => {
					const string str = "This is a test string";
					const double @double = 1.23;
					var @dateTime = DateTime.Now.ToString();

					Console.WriteLine( "Writing string '{0}, double '{1}', DateTime '{2}' to the first row of the worksheet.".FormatWith( str, @double, @dateTime ) );
					writer.DefaultWorksheet.AddRowToWorksheet( str, @double.ToString(), @dateTime );
					return "supported_data_types";
				} );
		}

		[ Test ]
		public void TestWritingManyRows() {
			runTest(
				writer => {
					Console.WriteLine( "Inserting counter value from the first 20 columns (A - T), for 1000 columns.".FormatWith() );

					for( var counter = 0; counter < 1000; counter++ )
						writer.DefaultWorksheet.AddRowToWorksheet( Enumerable.Range( 0, 20 ).Select( i => counter++.ToString() ).ToArray() );
					return "writing_many_rows";
				} );
		}

		[ Test ]
		public void TestWritingManyColumns() {
			runTest(
				writer => {
					Console.WriteLine( "Writing a sheet with 20 rows and 1000 columns (excel's max is theoretically 255).".FormatWith() );

					var columns = Enumerable.Range( 0, 1000 ).ToList();
					for( var row = 0; row < 20; row++ )
						writer.DefaultWorksheet.AddRowToWorksheet( columns.Select( i => ( row * columns.Max() ) + i.ToString() ).ToArray() );
					return "writing_many_columns";
				} );
		}

		[ Test ]
		public void TestWritingManyRowsAndColumns() {
			runTest(
				writer => {
					Console.WriteLine( "Writing a sheet with 1000 rows and 1000 columns.".FormatWith() );

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
					Console.WriteLine( "Making sure basic header rows work.".FormatWith() );

					var oneToTen = Enumerable.Range( 1, 10 ).ToArray();
					writer.DefaultWorksheet.AddHeaderToWorksheet( oneToTen.Select( i => "This is the header for column " + i ).ToArray() );
					writer.DefaultWorksheet.AddRowToWorksheet( oneToTen.Select( i => "The row above is the " + i + ordinalEnding( i ) + " column's header row." ).ToArray() );
					writer.DefaultWorksheet.AddRowToWorksheet( oneToTen.Select( i => "The row 2 lines above is the " + i + ordinalEnding( i ) + " column's header row." ).ToArray() );

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

		private void runTest( Func<StandardLibrary.IO.ExcelFileWriter, string> code ) {
			var writer = new StandardLibrary.IO.ExcelFileWriter();
			var fileName = code( writer );
			var filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ), TimestampPrefix + fileName + FileExtensions.ExcelXlsx );
			using( var f = File.Create( filePath ) )
				writer.SaveToStream( f );

			Console.WriteLine( "To view this test file, open '{0}' with Excel.".FormatWith( filePath ) );
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