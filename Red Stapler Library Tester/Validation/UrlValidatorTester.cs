using NUnit.Framework;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibraryTester.Validation {
	[ TestFixture ]
	internal class UrlValidatorTester {
		private Validator validator;

		[ SetUp ]
		public void SetupValidator() {
			validator = new Validator();
		}

		[ Test ]
		public void TestObviouslyValidUrl() {
			var result = getValidationResult( "http://www.google.com" );
			Assert.False( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestEmail() {
			var result = getValidationResult( "brendan@brendan.com" );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestObviouslyValidSecureUrl() {
			var result = getValidationResult( "https://www.google.com" );
			Assert.False( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestValidUppercaseUrl() {
			var result = getValidationResult( "HTTP://EN.EXAMPLE.ORG/" );
			Assert.False( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestUrlWithPort() {
			var result = getValidationResult( "http://vnc.example.com:5800" );
			Assert.False( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestLongerUrl() {
			var result =
				getValidationResult(
					"http://en.example.org/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" );
			Assert.False( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestRidiculouslyLongUrl() {
			var result =
				getValidationResult(
					"http://en.example.org/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" +
					"/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestValidUrlWithQueryString() {
			var result = getValidationResult( "http://www.google.com?first_name=John&last_name=Doe" );
			Assert.False( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestWeirdFtpUrl() {
			var result = getValidationResult( "ftp://asmith@ftp.example.org" );
			Assert.False( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestWeirdNewsUrl() {
			/* This should fail cause it doesn't match our protocol whitelist. */
			var result = getValidationResult( "news://rec.gardens.roses" );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestWord() {
			var result = getValidationResult( "supercalafragilisticexpialadocious" );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestSentence() {
			var result = getValidationResult( "I am the very model of a modern major general." );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestInt() {
			var result = getValidationResult( 42.ToString() );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestDecimal() {
			var result = getValidationResult( 48151623.42m.ToString() );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestDouble() {
			var result = getValidationResult( 48151623.42.ToString() );
			Assert.True( validator.ErrorsOccurred );
		}

		[ Test ]
		public void TestAllowedEmpty() {
			var result = validator.GetUrl( new ValidationErrorHandler( "" ), "", true );
			Assert.False( validator.ErrorsOccurred );
			result = validator.GetUrl( new ValidationErrorHandler( "" ), string.Empty, true );
			Assert.False( validator.ErrorsOccurred );
			result = validator.GetUrl( new ValidationErrorHandler( "" ), "    ", true );
			Assert.False( validator.ErrorsOccurred );
		}

		private string getValidationResult( string possibleUrl ) {
			return validator.GetUrl( new ValidationErrorHandler( "" ), possibleUrl, false );
		}
	}
}