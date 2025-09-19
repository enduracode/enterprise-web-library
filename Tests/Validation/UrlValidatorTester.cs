using NUnit.Framework;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.Tests.Validation;

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
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestEmail() {
		var result = getValidationResult( "brendan@brendan.com" );
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestObviouslyValidSecureUrl() {
		var result = getValidationResult( "https://www.google.com" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestSchemelessUrl() {
		var result = getValidationResult( "google.com" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestValidUppercaseUrl() {
		var result = getValidationResult( "HTTP://EN.EXAMPLE.ORG/" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestUrlWithPort() {
		var result = getValidationResult( "http://vnc.example.com:5800" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestLocalhostUrl() {
		var result = getValidationResult( "https://localhost:44310" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestLongerUrl() {
		var result = getValidationResult(
			"http://en.example.org/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL/WIKI/URL" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestRidiculouslyLongUrl() {
		var result = getValidationResult(
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
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestValidUrlWithQueryString() {
		var result = getValidationResult( "http://www.google.com?first_name=John&last_name=Doe" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestWeirdFtpUrl() {
		var result = getValidationResult( "ftp://asmith@ftp.example.org" );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	[ Test ]
	public void TestWeirdNewsUrl() {
		/* This should fail cause it doesn't match our protocol whitelist. */
		var result = getValidationResult( "news://rec.gardens.roses" );
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestWord() {
		var result = getValidationResult( "supercalafragilisticexpialadocious" );
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestSentence() {
		var result = getValidationResult( "I am the very model of a modern major general." );
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestInt() {
		var result = getValidationResult( 42.ToString() );
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestDecimal() {
		var result = getValidationResult( 48151623.42m.ToString() );
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestDouble() {
		var result = getValidationResult( 48151623.42.ToString() );
		Assert.That( validator.ErrorsOccurred, Is.True );
	}

	[ Test ]
	public void TestAllowedEmpty() {
		var result = validator.GetUrl( new ValidationErrorHandler( "" ), "", true );
		Assert.That( validator.ErrorsOccurred, Is.False );
		result = validator.GetUrl( new ValidationErrorHandler( "" ), string.Empty, true );
		Assert.That( validator.ErrorsOccurred, Is.False );
		result = validator.GetUrl( new ValidationErrorHandler( "" ), "    ", true );
		Assert.That( validator.ErrorsOccurred, Is.False );
	}

	private string getValidationResult( string possibleUrl ) {
		return validator.GetUrl( new ValidationErrorHandler( "" ), possibleUrl, false ).Value;
	}
}