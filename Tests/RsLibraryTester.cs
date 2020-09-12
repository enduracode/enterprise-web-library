using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using EnterpriseWebLibrary.Collections;
using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.IO;
using EnterpriseWebLibrary.MailMerging;
using Humanizer;
using NUnit.Framework;
using Tewl;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Tests {
	internal class RsLibraryTester {
		[ MTAThread ]
		public static void Main() {
			GlobalInitializationOps.InitStatics( new GlobalInitializer(), "Tester", false );

			EwlStatics.RunStandardLibraryTests();
			testMailMerging();

			Console.WriteLine( new TimeSpan( 0, 0, 0, 0, 4861000 ).ToHourMinuteSecondString() );
			Console.WriteLine( new TimeSpan( 0, 0, 0, 0, 4861000 ).ToHourMinuteString() );
			Console.WriteLine( new TimeSpan( 0, 0, 0, 0, 104861000 ).ToHourMinuteSecondString() );
			Console.WriteLine( new TimeSpan( 0, 0, 0, 0, 104861000 ).ToHourMinuteString() );
			Console.WriteLine( new TimeSpan( 1, 2, 3, 4, 0 ).ToHourMinuteSecondString() );
			Console.WriteLine( new TimeSpan( 1, 2, 3, 4, 0 ).ToHourMinuteString() );
			Console.WriteLine( new TimeSpan( 0, 1, 32 ).ToHourMinuteSecondString() );

			Console.WriteLine( FormattingMethods.GetFormattedBytes( 64 ) );
			Console.WriteLine( FormattingMethods.GetFormattedBytes( 64000 ) );
			Console.WriteLine( FormattingMethods.GetFormattedBytes( 64000000 ) );
			Console.WriteLine( FormattingMethods.GetFormattedBytes( 64500000000 ) );

			Console.WriteLine( "fred".CapitalizeString() );
			Console.WriteLine( "".CapitalizeString() );
			Console.WriteLine( "\n".CapitalizeString() );
			Console.WriteLine( "f".CapitalizeString() );
			Console.WriteLine( "1234f".CapitalizeString() );
			Console.WriteLine( "1234".CapitalizeString() );
			Console.WriteLine( "       f".CapitalizeString() );
			Console.WriteLine( "       ".CapitalizeString() );
			Console.WriteLine( " fred".CapitalizeString() );
			Console.WriteLine( " fred died.".CapitalizeString() );
			Console.WriteLine( ".".CapitalizeString() );
			Console.WriteLine( " .".CapitalizeString() );
			Console.WriteLine( " .fred died.".CapitalizeString() );
			Console.WriteLine( " . fred died.".CapitalizeString() );
			Console.WriteLine( "\nfred".CapitalizeString() );
			Console.WriteLine( " \n fred".CapitalizeString() );
			Console.WriteLine( "\n------\nfred".CapitalizeString() );

			Console.WriteLine( "one two three.csv".ToSafeFileName() );

			Console.WriteLine( "One {one one } two {two}".RemoveTextBetweenStrings( "{", "}" ) );
			Console.WriteLine( "This 'quoted text'.".RemoveTextBetweenStrings( "'", "'" ) );
			Console.WriteLine( "A comments looks like /*A comment.*/.".RemoveTextBetweenStrings( "/*", "*/" ) );
			Console.WriteLine( "body.ewf div.ewfIeWarningBanner table a { font-size:1.5em; }".RemoveTextBetweenStrings( "{", "}" ) );

			Console.WriteLine( "one".ConcatenateWithSpace( "two" ) );
			Console.WriteLine( EnterpriseWebLibrary.StringTools.ConcatenateWithDelimiter( ", ", "one", "two", "three" ) );
			Console.WriteLine( EnterpriseWebLibrary.StringTools.ConcatenateWithDelimiter( "|", "", "one", "", "", "two", "", "three ", "   " ) );

			Console.WriteLine( "abcde".Truncate( 4 ) );
			Console.WriteLine( "abcde".TruncateStart( 4 ) );
			Console.WriteLine( Tewl.Tools.NetTools.CombineUrls( @"http://www.redstapler.biz", "/Files", "Carriers", "Hancock/", "blabla.pdf" ) );
			Console.WriteLine( Tewl.Tools.NetTools.CombineUrls( @"http://www.redstapler.biz", "//Files", "Carriers", "Hancock//", "blabla.pdf//" ) );
			Console.WriteLine( Tewl.Tools.NetTools.CombineUrls( @"///http://www.redstapler.biz//", "/Files/", "Carriers/", "Hancock/", "/blabla.pdf/" ) );
			Console.WriteLine( Tewl.Tools.NetTools.CombineUrls( @"http://localhost/ToddPublicWebSite/", "Carriers", "UP", "ComparisonLogo.jpg" ) );

			Console.WriteLine( EwlStatics.CombinePaths( @"C:\Inetpub\", "Files", "orgs", "box.txt" ) );
			Console.WriteLine( EwlStatics.CombinePaths( @"C:\Inetpub\", "Files", "orgs", "anotherFolder", "box.txt" ) );
			Console.WriteLine( EwlStatics.CombinePaths( @"C:\Inetpub\", "Files", @"orgs\" ) );
			Console.WriteLine( EwlStatics.CombinePaths( @"C:\Inetpub", @"\Files\", @"\orgs", "box.txt" ) );
			Console.WriteLine( EwlStatics.CombinePaths( @"Inetpub", @"Files" ) );
			Console.WriteLine( EwlStatics.CombinePaths( @"D:\Source Control Repository\Charette", @"", @"\Aspose.Words.lic" ) );

			Console.WriteLine( EnterpriseWebLibrary.StringTools.CamelToEnglish( null ) );
			Console.WriteLine( "".CamelToEnglish() );
			Console.WriteLine( "L".CamelToEnglish() );
			Console.WriteLine( "l".CamelToEnglish() );
			Console.WriteLine( "LeftLeg".CamelToEnglish() );
			Console.WriteLine( "hits you in the Head and the LeftLeg!  That hurts.".CamelToEnglish() );

			var mySet = new HashSet<string>
				{
					"a",
					"c",
					"",
					"b",
					"fred"
				};

			var list = new List<string>( mySet );
			foreach( var item in mySet )
				list.Add( item );

			//	mySet = new Set( list );

			var ls = new ListSet<string> { "one", "two", "one", "two" };
			foreach( var item in ls )
				Console.WriteLine( item );

			var validator = new Validator();
			var vp = new ValidationErrorHandler( errorWriter );

			Console.WriteLine( validator.GetUrl( vp, "hTTp://RedStapler.biZ/fRed", false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( validator.GetUrl( vp, "fred", true ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.Write( validator.GetNullableSqlSmallDateTimeExact( vp, "fred", "MM/dd/yyy", false ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( validator.GetInt( vp, "fred" ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter ); // "myGoodInt" );
			Console.WriteLine( validator.GetInt( vp, "-342" ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter ); // "myBadDate" );
			Console.WriteLine( validator.GetSqlSmallDateTimeFromParts( vp, "3", "", "" ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter ); // "myBadDate" );
			Console.WriteLine( validator.GetSqlSmallDateTimeFromParts( vp, "", "", "" ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter ); // "zip" );
			Console.WriteLine( validator.GetZipCode( vp, "14580", true ).FullZipCode );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter ); // "zip" );
			Console.WriteLine( validator.GetZipCode( vp, "14580-1234", true ).FullZipCode );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter ); // "badZip" );
			Console.WriteLine( validator.GetZipCode( vp, "123", false ).FullZipCode );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			Console.WriteLine( "---------------------------------\nPhone Numbers:\n------------------------------------\n" );
			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( validator.GetPhoneNumber( vp, "5854556476", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );


			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( validator.GetPhoneNumber( vp, "585 4556476", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( validator.GetPhoneNumber( vp, "( 585 )455-6476", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( validator.GetPhoneNumber( vp, "(585)455-6476", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( validator.GetPhoneNumber( vp, "585-455-6476", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "With lots of spaces: " + validator.GetPhoneNumber( vp, "585   872   0291  ", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "With x: " + validator.GetPhoneNumber( vp, "5854556476   x   12345", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "With space ext. allowed: " + validator.GetPhoneNumber( vp, "5854556476 12345", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "Toni example 1: " + validator.GetPhoneNumber( vp, "321-663-4810", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "Toni example 2: " + validator.GetPhoneNumber( vp, "585-336-7600 ext 65361", true, true, false ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "Gibberish, should fail^ " + validator.GetPhoneNumber( vp, "sodifuoisafdoiu", true, true, true ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "With space no ext. allowed, should fail^ " + validator.GetPhoneNumber( vp, "5854556476 12345", false, false, true ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "With extension and no delimeters, should fail^ " + validator.GetPhoneNumber( vp, "585455647612345", true, true, false ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine(
				"With extension and no delimeters, should succeed since allowGarbage is on. " + validator.GetPhoneNumber( vp, "585455647612345", true, true, true ) );
			Assert.IsFalse( vp.LastResult != ErrorCondition.NoError );

			vp = new ValidationErrorHandler( errorWriter );
			Console.WriteLine( "Should fail^ " + validator.GetPhoneNumber( vp, "02934", true, true, false ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			Console.WriteLine( "------------------------\nEnd phone numbers.\n-------------------------------\n" );

			vp = new ValidationErrorHandler( errorWriter ); // "badByte" );
			Console.WriteLine( validator.GetByte( vp, "234987234" ) );
			Assert.IsFalse( vp.LastResult == ErrorCondition.NoError );

			var key = Rijndael.Create().Key;
			Console.Write( "Encryption Key: { " );
			foreach( var b in key )
				Console.Write( b + ", " );
			Console.WriteLine();

			Console.WriteLine( "SSN length: " + EncryptionOps.EncryptString( EncryptionOps.GenerateInitVector(), "987654321" ).Length );
		}

		private static void testMailMerging() {
			const string outputFolderName = "MergeOps";
			var outputFolder = EwlStatics.CombinePaths( TestStatics.OutputFolderPath, outputFolderName );
			IoMethods.DeleteFolder( outputFolder );
			Directory.CreateDirectory( outputFolder );

			var inputTestFiles = EwlStatics.CombinePaths( TestStatics.InputTestFilesFolderPath, outputFolderName );
			var wordDocx = EwlStatics.CombinePaths( inputTestFiles, "word.docx" );
			var pdf = EwlStatics.CombinePaths( inputTestFiles, "pdf.pdf" );

			var singleTestRow = new PseudoTableRow( 1 ).ToCollection();
			var testRows = new[] { new PseudoTableRow( 1 ), new PseudoTableRow( 2 ), new PseudoTableRow( 3 ) };
			var singleRowTree = MergeStatics.CreatePseudoTableRowTree( singleTestRow );
			var pseudoTableRowTree = MergeStatics.CreatePseudoTableRowTree( testRows );

			var explanations = new List<Tuple<String, String>>();

			// Single row to merge against

			// Word files

			const string singleRowWordDoc = "SingleRowMsWordDoc" + FileExtensions.WordDocx;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, singleRowWordDoc ) ) ) {
				using( var word = File.OpenRead( wordDocx ) )
					MergeOps.CreateMsWordDoc( singleRowTree, false, word, outputFile );
				explanations.Add( Tuple.Create( singleRowWordDoc, "Should be {0} with only one page, and FullName merged in the upper left.".FormatWith( wordDocx ) ) );
			}

			const string singleRowWordDocAsPdf = "SingleRowMsWordDoc" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, singleRowWordDocAsPdf ) ) )
				MergeOps.CreatePdfFromMsWordDoc( singleRowTree, false, wordDocx, outputFile );
			explanations.Add(
				Tuple.Create( singleRowWordDocAsPdf, "Should be {0} with only one page, FullName merged in the upper left, saved as a PDF.".FormatWith( wordDocx ) ) );

			//Excel
			const string singleRowExcel = "SingleRowExcel" + FileExtensions.ExcelXlsx;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, singleRowExcel ) ) )
				MergeOps.CreateExcelWorkbook( singleRowTree, MergeOps.GetExcelSupportedMergeFields( singleRowTree ), outputFile );
			explanations.Add(
				Tuple.Create(
					singleRowExcel,
					"An Excel file with the first row frozen and bold with the merge field names. Note that only supported field types may be dispalyed. One more row with data should be present." ) );

			// Pdf
			const string singleRowPdf = "SingleRowPdf" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, singleRowPdf ) ) )
				MergeOps.CreatePdf( singleRowTree, false, pdf, outputFile );
			explanations.Add( Tuple.Create( singleRowPdf, "Should be {0} with only one page, FullName filled in and 'Test' displayed.".FormatWith( pdf ) ) );

			// Multiple rows to merge against

			// Word files
			const string multipleRowsWordDoc = "MultipleRowMsWordDoc" + FileExtensions.WordDocx;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, multipleRowsWordDoc ) ) ) {
				using( var word = File.OpenRead( wordDocx ) )
					MergeOps.CreateMsWordDoc( pseudoTableRowTree, false, word, outputFile );
				explanations.Add(
					Tuple.Create( multipleRowsWordDoc, "Should be {0} with three pages, and FullName merged in the upper left.".FormatWith( wordDocx ) ) );
			}

			const string multipleRowsWordDocAsPdf = "MultipleRowMsWordDoc" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, multipleRowsWordDocAsPdf ) ) )
				MergeOps.CreatePdfFromMsWordDoc( pseudoTableRowTree, false, wordDocx, outputFile );
			explanations.Add(
				Tuple.Create( multipleRowsWordDocAsPdf, "Should be {0} with three pages, FullName merged in the upper left, saved as a PDF.".FormatWith( wordDocx ) ) );

			// Excel
			const string multipleRowExcel = "MultipleRowExcel" + FileExtensions.ExcelXlsx;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, multipleRowExcel ) ) )
				MergeOps.CreateExcelWorkbook( pseudoTableRowTree, MergeOps.GetExcelSupportedMergeFields( pseudoTableRowTree ), outputFile );
			explanations.Add(
				Tuple.Create(
					multipleRowExcel,
					"An Excel file with the first row frozen and bold with the merge field names. Note that only supported field types may be dispalyed. Three more row with data should be present." ) );

			// Pdf
			const string multipleRowPdf = "MultipleRowPdf" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, multipleRowPdf ) ) )
				MergeOps.CreatePdf( pseudoTableRowTree, false, pdf, outputFile );
			explanations.Add( Tuple.Create( multipleRowPdf, "Should be {0} with three pages, FullName filled in and 'Test' displayed.".FormatWith( pdf ) ) );

			TestStatics.OutputReadme( outputFolder, explanations );
		}

		private static void errorWriter( Validator validator, ErrorCondition validationResult ) {
			Console.WriteLine( "\nThe following validation resulted in an error: " + validationResult );
		}
	}
}