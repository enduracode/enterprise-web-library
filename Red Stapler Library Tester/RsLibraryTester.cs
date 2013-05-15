using System;
using System.Collections;
using System.Security.Cryptography;
using NUnit.Framework;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Collections;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibraryTester {
	internal class RsLibraryTester {
		[ MTAThread ]
		public static void Main() {
			AppTools.Init( "Tester", false, new GlobalLogic() );

			StandardLibraryMethods.RunStandardLibraryTests();

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
			Console.WriteLine( StringTools.ConcatenateWithDelimiter( ", ", "one", "two", "three" ) );
			Console.WriteLine( StringTools.ConcatenateWithDelimiter( "|", "", "one", "", "", "two", "", "three ", "   " ) );

			Console.WriteLine( "abcde".Truncate( 4 ) );
			Console.WriteLine( "abcde".TruncateStart( 4 ) );
			Console.WriteLine( NetTools.CombineUrls( @"http://www.redstapler.biz", "/Files", "Carriers", "Hancock/", "blabla.pdf" ) );
			Console.WriteLine( NetTools.CombineUrls( @"http://www.redstapler.biz", "//Files", "Carriers", "Hancock//", "blabla.pdf//" ) );
			Console.WriteLine( NetTools.CombineUrls( @"///http://www.redstapler.biz//", "/Files/", "Carriers/", "Hancock/", "/blabla.pdf/" ) );
			Console.WriteLine( NetTools.CombineUrls( @"http://localhost/ToddPublicWebSite/", "Carriers", "UP", "ComparisonLogo.jpg" ) );

			Console.WriteLine( StandardLibraryMethods.CombinePaths( @"C:\Inetpub\", "Files", "orgs", "box.txt" ) );
			Console.WriteLine( StandardLibraryMethods.CombinePaths( @"C:\Inetpub\", "Files", "orgs", "anotherFolder", "box.txt" ) );
			Console.WriteLine( StandardLibraryMethods.CombinePaths( @"C:\Inetpub\", "Files", @"orgs\" ) );
			Console.WriteLine( StandardLibraryMethods.CombinePaths( @"C:\Inetpub", @"\Files\", @"\orgs", "box.txt" ) );
			Console.WriteLine( StandardLibraryMethods.CombinePaths( @"Inetpub", @"Files" ) );
			Console.WriteLine( StandardLibraryMethods.CombinePaths( @"D:\Source Control Repository\Charette", @"", @"\Aspose.Words.lic" ) );

			Console.WriteLine( StringTools.CamelToEnglish( null ) );
			Console.WriteLine( "".CamelToEnglish() );
			Console.WriteLine( "L".CamelToEnglish() );
			Console.WriteLine( "l".CamelToEnglish() );
			Console.WriteLine( "LeftLeg".CamelToEnglish() );
			Console.WriteLine( "hits you in the Head and the LeftLeg!  That hurts.".CamelToEnglish() );

			var mySet = new Set { "a", "c", "", "b", "fred" };

			var list = new ArrayList( mySet );
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
			Console.WriteLine( "With extension and no delimeters, should succeed since allowGarbage is on. " +
			                   validator.GetPhoneNumber( vp, "585455647612345", true, true, true ) );
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

		private static void errorWriter( Validator validator, ErrorCondition validationResult ) {
			Console.WriteLine( "\nThe following validation resulted in an error: " + validationResult );
		}
	}
}