using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RedStapler.StandardLibrary.Validation {
	/// <summary>
	/// Contains high-level validation methods. Each validation method returns an object that is the value of the validated result. This value is meaningless if
	/// ValidationErrorHandler.LastResult is anything other than ErrorCondition.NoError. This property or the ErrorsOccurred property on this class should be
	/// checked before using returned values.
	/// </summary>
	public class Validator {
		// NOTE: There is already something called this.  Also, put it in its own file.
		private delegate T ValidationMethod<T>();

		internal static readonly DateTime SqlSmallDateTimeMinValue = new DateTime( 1900, 1, 1 );
		internal static readonly DateTime SqlSmallDateTimeMaxValue = new DateTime( 2079, 6, 6 );

		/// <summary>
		/// Most of our SQL Server decimal columns are specified as (9,2). This is the minimum value that will fit in such a column.
		/// </summary>
		public const decimal SqlDecimalDefaultMin = -9999999.99m;

		/// <summary>
		/// Most of our SQL Server decimal columns are specified as (9,2). This is the maximum value that will fit in such a column.
		/// </summary>
		public const decimal SqlDecimalDefaultMax = 9999999.99m;

		private readonly List<Error> errors = new List<Error>();

		/// <summary>
		/// The maximum length for a URL as dictated by the limitations of Internet Explorer. This is safely the maximum size for a URL.
		/// </summary>
		public const int MaxUrlLength = 2048;

		/// <summary>
		/// Returns true if any errors have been encountered during validation so far.  This can be true even while ErrorMessages.Count == 0
		/// and Errors.Count == 0, since NoteError may have been called.
		/// </summary>
		public bool ErrorsOccurred { get; private set; }

		/// <summary>
		/// Returns true if at least one unusable value has been returned since this Validator was created.  An unusable value return
		/// is defined as any time a Get... fails validation and the Validator is forced to return something other than
		/// a good default value.  An example of an unusable value would be a call to GetInt that fails validation.  An
		/// example of a usable value is a call to GetNullableInt, with allowEmpty = true, that fails validation.
		/// </summary>
		public bool UnusableValuesReturned {
			get {
				foreach( var error in errors ) {
					if( error.UnusableValueReturned )
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Returns a deep copy of the list of error messages associated with the validation performed by this validator so far. It's possible for ErrorsOccurred to
		/// be true while ErrorsMessages.Count == 0, since NoteError may have been called.
		/// </summary>
		public List<string> ErrorMessages {
			get {
				var errorMessages = new List<string>();
				foreach( var error in errors )
					errorMessages.Add( error.Message );
				return errorMessages;
			}
		}

		/// <summary>
		/// Returns a deep copy of the list of errors associated with the validation performed by this validator so far. It's possible for ErrorsOccurred to
		/// be true while Errors.Count == 0, since NoteError may have been called.
		/// </summary>
		public List<Error> Errors { get { return new List<Error>( errors ); } }

		/// <summary>
		/// Sets the ErrorsOccurred flag.
		/// </summary>
		public void NoteError() {
			ErrorsOccurred = true;
		}

		/// <summary>
		/// Sets the ErrorsOccurred flag and adds an error message to this validator. Use this if you want to add your own error message to the same collection
		/// that the error handlers use.
		/// </summary>
		public void NoteErrorAndAddMessage( string message ) {
			AddError( new Error( message, false ) );
		}

		// NOTE: The following method could replace both of the above methods.
		/// <summary>
		/// Sets the ErrorsOccurred flag and add the given error messages to this validator. Use this if you want to add your own error messages to the same collection
		/// that the error handlers use.
		/// </summary>
		public void NoteErrorAndAddMessages( params string[] messages ) {
			foreach( var message in messages )
				NoteErrorAndAddMessage( message );
		}

		internal void AddError( Error error ) {
			NoteError();
			errors.Add( error );
		}

		/// <summary>
		/// Accepts either true/false (case-sensitive) or 1/0.
		/// Returns the validated boolean type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public bool GetBoolean( ValidationErrorHandler errorHandler, string booleanAsString ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<bool>(
				errorHandler,
				booleanAsString,
				false,
				delegate { return validateBoolean( booleanAsString, errorHandler ); } );
		}

		/// <summary>
		/// Accepts either true/false (case-sensitive) or 1/0.
		/// Returns the validated boolean type from the given string and validation package.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public bool? GetNullableBoolean( ValidationErrorHandler errorHandler, string booleanAsString, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<bool?>(
				errorHandler,
				booleanAsString,
				allowEmpty,
				delegate { return validateBoolean( booleanAsString, errorHandler ); } );
		}

		private static bool validateBoolean( string booleanAsString, ValidationErrorHandler errorHandler ) {
			if( booleanAsString.IsNullOrWhiteSpace() )
				errorHandler.SetValidationResult( ValidationResult.Empty() );
			else if( booleanAsString != "1" && booleanAsString != "0" && booleanAsString != true.ToString() && booleanAsString != false.ToString() )
				errorHandler.SetValidationResult( ValidationResult.Invalid() );

			return booleanAsString == "1" || booleanAsString == true.ToString();
		}

		/// <summary>
		/// Returns the validated byte type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public byte GetByte( ValidationErrorHandler errorHandler, string byteAsString ) {
			return GetByte( errorHandler, byteAsString, byte.MinValue, byte.MaxValue );
		}

		/// <summary>
		/// Returns the validated byte type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public byte GetByte( ValidationErrorHandler errorHandler, string byteAsString, byte min, byte max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<byte>(
				errorHandler,
				byteAsString,
				false,
				delegate { return validateGenericIntegerType<byte>( errorHandler, byteAsString, min, max ); } );
		}

		/// <summary>
		/// Returns the validated byte type from the given string and validation package.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public byte? GetNullableByte( ValidationErrorHandler errorHandler, string byteAsString, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<byte?>(
				errorHandler,
				byteAsString,
				allowEmpty,
				delegate { return validateGenericIntegerType<byte>( errorHandler, byteAsString, byte.MinValue, byte.MaxValue ); } );
		}

		/// <summary>
		/// Returns the validated short type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public short GetShort( ValidationErrorHandler errorHandler, string shortAsString ) {
			return GetShort( errorHandler, shortAsString, short.MinValue, short.MaxValue );
		}

		/// <summary>
		/// Returns the validated short type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public short GetShort( ValidationErrorHandler errorHandler, string shortAsString, short min, short max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<short>(
				errorHandler,
				shortAsString,
				false,
				delegate { return validateGenericIntegerType<short>( errorHandler, shortAsString, min, max ); } );
		}

		/// <summary>
		/// Returns the validated short type from the given string and validation package.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public short? GetNullableShort( ValidationErrorHandler errorHandler, string shortAsString, bool allowEmpty ) {
			return GetNullableShort( errorHandler, shortAsString, allowEmpty, short.MinValue, short.MaxValue );
		}

		/// <summary>
		/// Returns the validated short type from the given string and validation package.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public short? GetNullableShort( ValidationErrorHandler errorHandler, string shortAsString, bool allowEmpty, short min, short max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<short?>(
				errorHandler,
				shortAsString,
				allowEmpty,
				delegate { return validateGenericIntegerType<short>( errorHandler, shortAsString, min, max ); } );
		}

		/// <summary>
		/// Returns the validated int type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public int GetInt( ValidationErrorHandler errorHandler, string intAsString ) {
			return GetInt( errorHandler, intAsString, int.MinValue, int.MaxValue );
		}

		/// <summary>
		/// Returns the validated int type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public int GetInt( ValidationErrorHandler errorHandler, string intAsString, int min, int max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<int>(
				errorHandler,
				intAsString,
				false,
				delegate { return validateGenericIntegerType<int>( errorHandler, intAsString, min, max ); } );
		}

		/// <summary>
		/// Returns the validated int type from the given string and validation package.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public int? GetNullableInt( ValidationErrorHandler errorHandler, string intAsString, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<int?>(
				errorHandler,
				intAsString,
				allowEmpty,
				delegate { return validateGenericIntegerType<int>( errorHandler, intAsString, int.MinValue, int.MaxValue ); } );
		}

		private static T validateGenericIntegerType<T>( ValidationErrorHandler errorHandler, string valueAsString, long minValue, long maxValue ) {
			long intResult = 0;

			if( valueAsString.IsNullOrWhiteSpace() )
				errorHandler.SetValidationResult( ValidationResult.Empty() );
			else {
				try {
					intResult = Convert.ToInt64( valueAsString );

					if( intResult > maxValue )
						errorHandler.SetValidationResult( ValidationResult.TooLarge( minValue, maxValue ) );
					else if( intResult < minValue )
						errorHandler.SetValidationResult( ValidationResult.TooSmall( minValue, maxValue ) );
				}
				catch( FormatException ) {
					errorHandler.SetValidationResult( ValidationResult.Invalid() );
				}
				catch( OverflowException ) {
					errorHandler.SetValidationResult( ValidationResult.Invalid() );
				}
			}

			if( errorHandler.LastResult != ErrorCondition.NoError )
				return default( T );
			return (T)Convert.ChangeType( intResult, typeof( T ) );
		}

		/// <summary>
		/// Returns a validated float type from the given string, validation package, and min/max restrictions.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public float GetFloat( ValidationErrorHandler errorHandler, string floatAsString, float min, float max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<float>(
				errorHandler,
				floatAsString,
				false,
				delegate { return validateFloat( floatAsString, errorHandler, min, max ); } );
		}

		/// <summary>
		/// Returns a validated float type from the given string, validation package, and min/max restrictions.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public float? GetNullableFloat( ValidationErrorHandler errorHandler, string floatAsString, bool allowEmpty, float min, float max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<float?>(
				errorHandler,
				floatAsString,
				allowEmpty,
				delegate { return validateFloat( floatAsString, errorHandler, min, max ); } );
		}

		private static float validateFloat( string floatAsString, ValidationErrorHandler errorHandler, float min, float max ) {
			float floatValue = 0;
			if( floatAsString.IsNullOrWhiteSpace() )
				errorHandler.SetValidationResult( ValidationResult.Empty() );
			else {
				try {
					floatValue = float.Parse( floatAsString );
				}
				catch( FormatException ) {
					errorHandler.SetValidationResult( ValidationResult.Invalid() );
				}
				catch( OverflowException ) {
					errorHandler.SetValidationResult( ValidationResult.Invalid() );
				}

				if( floatValue < min )
					errorHandler.SetValidationResult( ValidationResult.TooSmall( min, max ) );
				else if( floatValue > max )
					errorHandler.SetValidationResult( ValidationResult.TooLarge( min, max ) );
			}

			return floatValue;
		}


		/// <summary>
		/// Returns a validated decimal type from the given string and validation package.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public Decimal GetDecimal( ValidationErrorHandler errorHandler, string decimalAsString ) {
			return GetDecimal( errorHandler, decimalAsString, decimal.MinValue, decimal.MaxValue );
		}

		/// <summary>
		/// Returns a validated decimal type from the given string, validation package, and min/max restrictions.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public Decimal GetDecimal( ValidationErrorHandler errorHandler, string decimalAsString, Decimal min, Decimal max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<decimal>(
				errorHandler,
				decimalAsString,
				false,
				delegate { return validateDecimal( decimalAsString, errorHandler, min, max ); } );
		}

		/// <summary>
		/// Returns a validated decimal type from the given string and validation package.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public Decimal? GetNullableDecimal( ValidationErrorHandler errorHandler, string decimalAsString, bool allowEmpty ) {
			return GetNullableDecimal( errorHandler, decimalAsString, allowEmpty, decimal.MinValue, decimal.MaxValue );
		}

		/// <summary>
		/// Returns a validated decimal type from the given string, validation package, and min/max restrictions.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public Decimal? GetNullableDecimal( ValidationErrorHandler errorHandler, string decimalAsString, bool allowEmpty, Decimal min, Decimal max ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<decimal?>(
				errorHandler,
				decimalAsString,
				allowEmpty,
				delegate { return validateDecimal( decimalAsString, errorHandler, min, max ); } );
		}

		private static decimal validateDecimal( string decimalAsString, ValidationErrorHandler errorHandler, decimal min, decimal max ) {
			if( decimalAsString.IsNullOrWhiteSpace() ) {
				errorHandler.SetValidationResult( ValidationResult.Empty() );
				return 0;
			}

			Decimal decimalVal = 0;
			try {
				decimalVal = decimal.Parse( decimalAsString );
				if( decimalVal < min )
					errorHandler.SetValidationResult( ValidationResult.TooSmall( min, max ) );
				else if( decimalVal > max )
					errorHandler.SetValidationResult( ValidationResult.TooLarge( min, max ) );
			}
			catch {
				errorHandler.SetValidationResult( ValidationResult.Invalid() );
			}

			return decimalVal;
		}

		/// <summary>
		/// Returns a validated string from the given string and restrictions.
		/// If allowEmpty true and an empty string or null is given, the empty string is returned.
		/// Automatically trims whitespace from edges of returned string.
		/// </summary>
		public string GetString( ValidationErrorHandler errorHandler, string text, bool allowEmpty ) {
			return GetString( errorHandler, text, allowEmpty, int.MaxValue );
		}

		/// <summary>
		/// Returns a validated string from the given string and restrictions.
		/// If allowEmpty true and an empty string or null is given, the empty string is returned.
		/// Automatically trims whitespace from edges of returned string.
		/// </summary>
		public string GetString( ValidationErrorHandler errorHandler, string text, bool allowEmpty, int maxLength ) {
			return GetString( errorHandler, text, allowEmpty, 0, maxLength );
		}

		/// <summary>
		/// Returns a validated string from the given string and restrictions.
		/// If allowEmpty true and an empty string or null is given, the empty string is returned.
		/// Automatically trims whitespace from edges of returned string.
		/// </summary>
		public string GetString( ValidationErrorHandler errorHandler, string text, bool allowEmpty, int minLength, int maxLength ) {
			return handleEmptyAndReturnEmptyStringIfInvalid(
				errorHandler,
				text,
				allowEmpty,
				delegate {
					var errorMessage = "The length of the " + errorHandler.Subject + " must be between " + minLength + " and " + maxLength + " characters.";
					if( text.Length > maxLength )
						errorHandler.SetValidationResult( ValidationResult.Custom( ErrorCondition.TooLong, errorMessage ) );
					else if( text.Length < minLength )
						errorHandler.SetValidationResult( ValidationResult.Custom( ErrorCondition.TooShort, errorMessage ) );

					return text.Trim();
				} );
		}

		/// <summary>
		/// Returns a validated email address from the given string and restrictions.
		/// If allowEmpty true and the empty string or null is given, the empty string is returned.
		/// Automatically trims whitespace from edges of returned string.
		/// The maxLength defaults to 254 per this source: http://en.wikipedia.org/wiki/E-mail_address#Syntax
		/// If you pass a different value for maxLength, you'd better have a good reason.
		/// </summary>
		public string GetEmailAddress( ValidationErrorHandler errorHandler, string emailAddress, bool allowEmpty, int maxLength = 254 ) {
			return handleEmptyAndReturnEmptyStringIfInvalid(
				errorHandler,
				emailAddress,
				allowEmpty,
				delegate {
					// Validate as a string with same restrictions - if it fails on that, return
					emailAddress = GetString( errorHandler, emailAddress, allowEmpty, maxLength );
					if( errorHandler.LastResult == ErrorCondition.NoError ) {
						// [^@ \n] means any character but a @ or a newline or a space.  This forces only one @ to exist.
						//([^@ \n\.]+\.)+ forces any positive number of (anything.)s to exist in a row.  Doesn't allow "..".
						// Allows anything.anything123-anything@anything.anything123.anything
						const string localPartUnconditionallyPermittedCharacters = @"[a-z0-9!#\$%&'\*\+\-/=\?\^_`\{\|}~]";
						const string localPart = "(" + localPartUnconditionallyPermittedCharacters + @"+\.?)*" + localPartUnconditionallyPermittedCharacters + "+";
						const string domainUnconditionallyPermittedCharacters = @"[a-z0-9-]";
						const string domain = "(" + domainUnconditionallyPermittedCharacters + @"+\.)+" + domainUnconditionallyPermittedCharacters + "+";
						// The first two conditions are for performance only.
						if( !emailAddress.Contains( "@" ) || !emailAddress.Contains( "." ) ||
						    !Regex.IsMatch( emailAddress, "^" + localPart + "@" + domain + "$", RegexOptions.IgnoreCase ) )
							errorHandler.SetValidationResult( ValidationResult.Invalid() );
						// Max length is already checked by the string validation
						// NOTE: We should really enforce the max length of the domain portion and the local portion individually as well.
					}
					return emailAddress;
				} );
		}

		/// <summary>
		/// Returns a validated URL.
		/// </summary>
		public string GetUrl( ValidationErrorHandler errorHandler, string url, bool allowEmpty ) {
			return GetUrl( errorHandler, url, allowEmpty, MaxUrlLength );
		}


		private static readonly string[] validSchemes = new[] { "http", "https", "ftp" };

		/// <summary>
		/// Returns a validated URL. Note that you may run into problems with certain browsers if you pass a length longer than 2048.
		/// </summary>
		public string GetUrl( ValidationErrorHandler errorHandler, string url, bool allowEmpty, int maxUrlLength ) {
			return handleEmptyAndReturnEmptyStringIfInvalid(
				errorHandler,
				url,
				allowEmpty,
				delegate {
					url = GetString( errorHandler, validSchemes.Any( s => url.StartsWithIgnoreCase( s ) ) ? url : "http://" + url, true, maxUrlLength );
					if( errorHandler.LastResult == ErrorCondition.NoError ) {
						try {
							// Don't allow relative URLs
							var uri = new Uri( url, UriKind.Absolute );
							// Must be a valid DNS-style hostname or IP address
							// Must contain at least one '.', to prevent just host names
							// Must be one of the common web browser-accessible schemes
							if( uri.HostNameType != UriHostNameType.Dns && uri.HostNameType != UriHostNameType.IPv4 && uri.HostNameType != UriHostNameType.IPv6 ||
							    !uri.Host.Any( c => c == '.' ) || !validSchemes.Any( s => s == uri.Scheme ) )
								throw new UriFormatException();
						}
						catch( UriFormatException ) {
							errorHandler.SetValidationResult( ValidationResult.Invalid() );
						}
					}
					return url;
				} );
		}

		/// <summary>
		/// The same as GetPhoneNumber, except the given default area code will be prepended on the phone number if necessary.
		/// This is useful when working with data that had the area code omitted because the number was local.
		/// </summary>
		public string GetPhoneNumberWithDefaultAreaCode(
			ValidationErrorHandler errorHandler, string completePhoneNumber, bool allowExtension, bool allowEmpty, bool allowSurroundingGarbage, string defaultAreaCode ) {
			var validator = new Validator(); // We need to use a separate one so that erroneous error messages don't get left in the collection
			var fakeHandler = new ValidationErrorHandler( "" );

			validator.GetPhoneNumber( fakeHandler, completePhoneNumber, allowExtension, allowEmpty, allowSurroundingGarbage );
			if( fakeHandler.LastResult != ErrorCondition.NoError ) {
				fakeHandler = new ValidationErrorHandler( "" );
				validator.GetPhoneNumber( fakeHandler, defaultAreaCode + completePhoneNumber, allowExtension, allowEmpty, allowSurroundingGarbage );
				// If the phone number was invalid without the area code, but is valid with the area code, we really validate using the default
				// area code and then return.  In all other cases, we return what would have happened without tacking on the default area code.
				if( fakeHandler.LastResult == ErrorCondition.NoError )
					return GetPhoneNumber( errorHandler, defaultAreaCode + completePhoneNumber, allowExtension, allowEmpty, allowSurroundingGarbage );
			}

			return GetPhoneNumber( errorHandler, completePhoneNumber, allowExtension, allowEmpty, allowSurroundingGarbage );
		}

		/// <summary>
		/// Returns a validated phone number as a standard phone number string given the complete phone number with optional
		/// extension as a string. If allow empty is true and an empty string or null is given, the empty string is returned.
		/// Pass true for allow surrounding garbage if you want to allow "The phone number is 585-455-6476yadayada." to be parsed into 585-455-6476
		/// and count as a valid phone number.
		/// </summary>
		public string GetPhoneNumber(
			ValidationErrorHandler errorHandler, string completePhoneNumber, bool allowExtension, bool allowEmpty, bool allowSurroundingGarbage ) {
			return GetPhoneWithLastFiveMapping( errorHandler, completePhoneNumber, allowExtension, allowEmpty, allowSurroundingGarbage, null );
		}

		/// <summary>
		/// Returns a validated phone number as a standard phone number string given the complete phone number with optional extension or the last five digits of the number and a dictionary of single
		/// digits to five-digit groups that become the first five digits of the full number.  If allow empty is true and an empty string or null is given, the empty string is returned.
		/// </summary>
		public string GetPhoneWithLastFiveMapping(
			ValidationErrorHandler errorHandler, string input, bool allowExtension, bool allowEmpty, bool allowSurroundingGarbage, Dictionary<string, string> firstFives ) {
			return GetPhoneNumberAsObject( errorHandler, input, allowExtension, allowEmpty, allowSurroundingGarbage, firstFives ).StandardPhoneString;
		}

		internal PhoneNumber GetPhoneNumberAsObject(
			ValidationErrorHandler errorHandler, string input, bool allowExtension, bool allowEmpty, bool allowSurroundingGarbage, Dictionary<string, string> firstFives ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid(
				errorHandler,
				input,
				allowEmpty,
				delegate {
					var invalidPrefix = "The " + errorHandler.Subject + " (" + input + ") is invalid.";
					// Remove all of the valid delimiter characters so we can just deal with numbers and whitespace
					input = input.RemoveCharacters( new[] { '-', '(', ')', '.' } ).Trim();

					var invalidMessage = invalidPrefix +
					                     " Phone numbers may be entered in any format, such as or xxx-xxx-xxxx, with an optional extension up to 5 digits long.  International numbers should begin with a '+' sign.";
					var phoneNumber = PhoneNumber.CreateFromParts( "", "", "" );

					// NOTE: AllowSurroundingGarbage does not apply to first five or international numbers.

					// First-five shortcut (intra-org phone numbers)
					if( firstFives != null && Regex.IsMatch( input, @"^\d{5}$" ) ) {
						string firstFive;
						if( firstFives.ContainsKey( input.Substring( 0, 1 ) ) ) {
							firstFive = firstFives[ input.Substring( 0, 1 ) ];
							phoneNumber = PhoneNumber.CreateFromParts( firstFive.Substring( 0, 3 ), firstFive.Substring( 3 ) + input, "" );
						}
						else
							errorHandler.SetValidationResult( ValidationResult.Custom( ErrorCondition.Invalid, "The five digit phone number you entered isn't recognized." ) );
					}
						// International phone numbers
						// We require a country code and then at least 7 digits (but if country code is more than one digit, we require fewer subsequent digits).
						// We feel this is a reasonable limit to ensure that they are entering an actual phone number, but there is no source for this limit.
						// We have no idea why we ever began accepting letters, but it's risky to stop accepting them and the consequences of accepting them are small.
					else if( Regex.IsMatch( input, @"\+\s*[0|2-9]([a-zA-Z,#/ \.\(\)\*]*[0-9]){7}" ) )
						phoneNumber = PhoneNumber.CreateInternational( input );
						// Validated it as a North American Numbering Plan phone number
					else {
						var regex = @"(?<lead>\+?1)?\s*(?<ac>\d{3})\s*(?<num1>\d{3})\s*(?<num2>\d{4})\s*?(?:(?:x|\s|ext|ext\.|extension)\s*(?<ext>\d{1,5}))?\s*";
						if( !allowSurroundingGarbage )
							regex = "^" + regex + "$";

						var match = Regex.Match( input, regex );

						if( match.Success ) {
							var areaCode = match.Groups[ "ac" ].Value;
							var number = match.Groups[ "num1" ].Value + match.Groups[ "num2" ].Value;
							var extension = match.Groups[ "ext" ].Value;
							phoneNumber = PhoneNumber.CreateFromParts( areaCode, number, extension );
							if( !allowExtension && phoneNumber.Extension.Length > 0 ) {
								errorHandler.SetValidationResult(
									ValidationResult.Custom( ErrorCondition.Invalid, invalidPrefix + " Extensions are not permitted in this field. Use the separate extension field." ) );
							}
						}
						else
							errorHandler.SetValidationResult( ValidationResult.Custom( ErrorCondition.Invalid, invalidMessage ) );
					}
					return phoneNumber;
				},
				PhoneNumber.CreateFromParts( "", "", "" ) );
		}

		/// <summary>
		/// Returns a validated phone number extension as a string.
		/// If allow empty is true and the empty string or null is given, the empty string is returned.
		/// </summary>
		public string GetPhoneNumberExtension( ValidationErrorHandler errorHandler, string extension, bool allowEmpty ) {
			return handleEmptyAndReturnEmptyStringIfInvalid(
				errorHandler,
				extension,
				allowEmpty,
				delegate {
					extension = extension.Trim();
					if( !Regex.IsMatch( extension, @"^ *(?<ext>\d{1,5}) *$" ) )
						errorHandler.SetValidationResult( ValidationResult.Invalid() );

					return extension;
				} );
		}

		/// <summary>
		/// Returns a validated social security number from the given string and restrictions.
		/// If allowEmpty true and an empty string or null is given, the empty string is returned.
		/// </summary>
		public string GetSocialSecurityNumber( ValidationErrorHandler errorHandler, string ssn, bool allowEmpty ) {
			return GetNumber( errorHandler, ssn, 9, allowEmpty, "-" );
		}

		/// <summary>
		/// Gets a string of the given length whose characters are only numeric values, after throwing out all acceptable garbage characters.
		/// Example: A social security number (987-65-4321) would be GetNumber( errorHandler, ssn, 9, true, "-" ).
		/// </summary>
		public string GetNumber( ValidationErrorHandler errorHandler, string text, int numberOfDigits, bool allowEmpty, params string[] acceptableGarbageStrings ) {
			return handleEmptyAndReturnEmptyStringIfInvalid(
				errorHandler,
				text,
				allowEmpty,
				delegate {
					foreach( var garbageString in acceptableGarbageStrings )
						text = text.Replace( garbageString, "" );
					text = text.Trim();
					if( !Regex.IsMatch( text, @"^\d{" + numberOfDigits + "}$" ) )
						errorHandler.SetValidationResult( ValidationResult.Invalid() );
					return text;
				} );
		}

		/// <summary>
		/// Gets a validated United States zip code object given the complete zip code with optional +4 digits.
		/// </summary>
		public ZipCode GetZipCode( ValidationErrorHandler errorHandler, string zipCode, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid(
				errorHandler,
				zipCode,
				allowEmpty,
				delegate { return ZipCode.CreateUsZipCode( errorHandler, zipCode ); },
				new ZipCode() );
		}


		/// <summary>
		/// Gets a validated US or Canadian zip code.
		/// </summary>
		public ZipCode GetUsOrCanadianZipCode( ValidationErrorHandler errorHandler, string zipCode, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid(
				errorHandler,
				zipCode,
				allowEmpty,
				delegate { return ZipCode.CreateUsOrCanadianZipCode( errorHandler, zipCode ); },
				new ZipCode() );
		}

		/// <summary>
		/// Returns the validated DateTime type from the given string and validation package.
		/// It is restricted to the Sql SmallDateTime range of 1/1/1900 up to 6/6/2079.
		/// Passing an empty string or null will result in ErrorCondition.Empty.
		/// </summary>
		public DateTime GetSqlSmallDateTime( ValidationErrorHandler errorHandler, string dateAsString ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<DateTime>(
				errorHandler,
				dateAsString,
				false,
				delegate { return validateDateTime( errorHandler, dateAsString, null, SqlSmallDateTimeMinValue, SqlSmallDateTimeMaxValue ); } );
		}

		/// <summary>
		/// Returns the validated DateTime type from the given string and validation package.
		/// It is restricted to the Sql SmallDateTime range of 1/1/1900 up to 6/6/2079.
		/// If allowEmpty is true and the given string is empty, null will be returned.
		/// </summary>
		public DateTime? GetNullableSqlSmallDateTime( ValidationErrorHandler errorHandler, string dateAsString, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<DateTime?>(
				errorHandler,
				dateAsString,
				allowEmpty,
				delegate { return validateDateTime( errorHandler, dateAsString, null, SqlSmallDateTimeMinValue, SqlSmallDateTimeMaxValue ); } );
		}

		/// <summary>
		/// Returns the validated DateTime type from the given date part strings and validation package.
		/// It is restricted to the Sql SmallDateTime range of 1/1/1900 up to 6/6/2079.
		/// Passing an empty string or null for each date part will result in ErrorCondition.Empty.
		/// Passing an empty string or null for only some date parts will result in ErrorCondition.Invalid.
		/// </summary>
		public DateTime GetSqlSmallDateTimeFromParts( ValidationErrorHandler errorHandler, string month, string day, string year ) {
			return GetSqlSmallDateTime( errorHandler, makeDateFromParts( month, day, year ) );
		}

		/// <summary>
		/// Returns the validated DateTime type from the given date part strings and validation package.
		/// It is restricted to the Sql SmallDateTime range of 1/1/1900 up to 6/6/2079.
		/// If allowEmpty is true and each date part string is empty, null will be returned.
		/// Passing an empty string or null for only some date parts will result in ErrorCondition.Invalid.
		/// </summary>
		public DateTime? GetNullableSqlSmallDateTimeFromParts( ValidationErrorHandler errorHandler, string month, string day, string year, bool allowEmpty ) {
			return GetNullableSqlSmallDateTime( errorHandler, makeDateFromParts( month, day, year ), allowEmpty );
		}

		private static string makeDateFromParts( string month, string day, string year ) {
			var date = month + '/' + day + '/' + year;
			if( date == "//" )
				date = "";
			return date;
		}

		/// <summary>
		/// Returns the validated DateTime type from a date string and an exact match pattern.'
		/// Pattern specifies the date format, such as "MM/dd/yyyy".
		/// </summary>
		public DateTime GetSqlSmallDateTimeExact( ValidationErrorHandler errorHandler, string dateAsString, string pattern ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<DateTime>(
				errorHandler,
				dateAsString,
				false,
				delegate { return validateSqlSmallDateTimeExact( errorHandler, dateAsString, pattern ); } );
		}

		/// <summary>
		/// Returns the validated DateTime type from a date string and an exact match pattern.'
		/// Pattern specifies the date format, such as "MM/dd/yyyy".
		/// </summary>
		public DateTime? GetNullableSqlSmallDateTimeExact( ValidationErrorHandler errorHandler, string dateAsString, string pattern, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<DateTime?>(
				errorHandler,
				dateAsString,
				allowEmpty,
				delegate { return validateSqlSmallDateTimeExact( errorHandler, dateAsString, pattern ); } );
		}

		private static DateTime validateSqlSmallDateTimeExact( ValidationErrorHandler errorHandler, string dateAsString, string pattern ) {
			DateTime date;
			if( !DateTime.TryParseExact( dateAsString, pattern, CultureInfo.CurrentCulture, DateTimeStyles.None, out date ) )
				errorHandler.SetValidationResult( ValidationResult.Invalid() );
			else
				validateNativeDateTime( errorHandler, date, false, SqlSmallDateTimeMinValue, SqlSmallDateTimeMaxValue );

			return date;
		}

		private static DateTime validateDateTime( ValidationErrorHandler errorHandler, string dateAsString, string[] formats, DateTime min, DateTime max ) {
			var date = DateTime.Now;
			try {
				date = formats != null ? DateTime.ParseExact( dateAsString, formats, null, DateTimeStyles.None ) : DateTime.Parse( dateAsString );
				validateNativeDateTime( errorHandler, date, false, min, max );
			}
			catch( FormatException ) {
				errorHandler.SetValidationResult( ValidationResult.Invalid() );
			}
			catch( ArgumentOutOfRangeException ) {
				// Undocumented exception that there are reports of being thrown
				errorHandler.SetValidationResult( ValidationResult.Invalid() );
			}
			catch( ArgumentNullException ) {
				errorHandler.SetValidationResult( ValidationResult.Empty() );
			}

			return date;
		}

		private static void validateNativeDateTime( ValidationErrorHandler errorHandler, DateTime? date, bool allowEmpty, DateTime minDate, DateTime maxDate ) {
			if( date == null && !allowEmpty )
				errorHandler.SetValidationResult( ValidationResult.Empty() );
			else if( date.HasValue ) {
				var minMaxMessage = " It must be between " + minDate + " and " + maxDate + ".";
				if( date < minDate )
					errorHandler.SetValidationResult( ValidationResult.Custom( ErrorCondition.TooEarly, "The " + errorHandler.Subject + " is too early." + minMaxMessage ) );
				else if( date >= maxDate )
					errorHandler.SetValidationResult( ValidationResult.Custom( ErrorCondition.TooLate, "The " + errorHandler.Subject + " is too late." + minMaxMessage ) );
			}
		}

		/// <summary>
		/// Validates the date using given allowEmpty, min, and max constraints.
		/// </summary>
		public DateTime? GetNullableDateTime(
			ValidationErrorHandler handler, string dateAsString, string[] formats, bool allowEmpty, DateTime minDate, DateTime maxDate ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<DateTime?>(
				handler,
				dateAsString,
				allowEmpty,
				() => validateDateTime( handler, dateAsString, formats, minDate, maxDate ) );
		}

		/// <summary>
		/// Validates the date using given min and max constraints.
		/// </summary>
		public DateTime GetDateTime( ValidationErrorHandler handler, string dateAsString, string[] formats, DateTime minDate, DateTime maxDate ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<DateTime>(
				handler,
				dateAsString,
				false,
				() => validateDateTime( handler, dateAsString, formats, minDate, maxDate ) );
		}

		/// <summary>
		/// Validates the given time span.
		/// </summary>
		public TimeSpan? GetNullableTimeSpan( ValidationErrorHandler handler, TimeSpan? timeSpan, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<TimeSpan?>( handler, timeSpan, allowEmpty, delegate { return timeSpan; } );
		}

		/// <summary>
		/// Validates the given time span.
		/// </summary>
		public TimeSpan GetTimeSpan( ValidationErrorHandler handler, TimeSpan? timeSpan ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<TimeSpan>(
				handler,
				timeSpan,
				false,
				delegate { return timeSpan == null ? default( TimeSpan ) : timeSpan.Value; } );
		}

		/// <summary>
		/// Validates the given time span.
		/// </summary>
		public TimeSpan? GetNullableTimeOfDayTimeSpan( ValidationErrorHandler handler, string timeSpanAsString, string[] formats, bool allowEmpty ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<TimeSpan?>(
				handler,
				timeSpanAsString,
				allowEmpty,
				() => validateDateTime( handler, timeSpanAsString, formats, DateTime.MinValue, DateTime.MaxValue ).TimeOfDay );
		}

		/// <summary>
		/// Validates the given time span.
		/// </summary>
		public TimeSpan GetTimeOfDayTimeSpan( ValidationErrorHandler handler, string timeSpanAsString, string[] formats ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<TimeSpan>(
				handler,
				timeSpanAsString,
				false,
				() => validateDateTime( handler, timeSpanAsString, formats, DateTime.MinValue, DateTime.MaxValue ).TimeOfDay );
		}

		private T executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid<T>(
			ValidationErrorHandler handler, object valueAsObject, bool allowEmpty, ValidationMethod<T> method, T customDefaultReturnValue = default( T ) ) {
			var result = customDefaultReturnValue;
			if( !isEmpty( handler, valueAsObject, allowEmpty ) )
				result = method();

			// If there was an error of any kind, the result becomes the default value
			if( handler.LastResult != ErrorCondition.NoError )
				result = customDefaultReturnValue;

			handler.HandleResult( this, !allowEmpty );
			return result;
		}

		private string handleEmptyAndReturnEmptyStringIfInvalid(
			ValidationErrorHandler handler, object valueAsObject, bool allowEmpty, ValidationMethod<string> method ) {
			return executeValidationMethodAndHandleEmptyAndReturnDefaultIfInvalid( handler, valueAsObject, allowEmpty, method, "" );
		}

		/// <summary>
		/// Determines if the given field is empty, and if it is empty, it
		/// assigns the correct ErrorCondition to the validation package's ValidationResult
		/// and adds an error message to the errors collection.
		/// Returns true if val is empty and should not be validated further.  Returns false
		/// if value is not empty and validation should continue.  Validation methods should
		/// return immediately with no further action if this method returns true.
		/// </summary>
		private static bool isEmpty( ValidationErrorHandler errorHandler, object valueAsObject, bool allowEmpty ) {
			var isEmpty = valueAsObject.ObjectToString( true ).Trim().Length == 0;

			if( !allowEmpty && isEmpty )
				errorHandler.SetValidationResult( ValidationResult.Empty() );

			return isEmpty;
		}

		internal static void Test() {
			testEmailAddress( "gregory.michael.smalter@redstapler.biz", true );
			testEmailAddress( "greg.smalter@redstapler.biz", true );
			testEmailAddress( "Greg.Smalter@Redstapler.Biz", true );
			testEmailAddress( "gregsmalter@elysianerebus.net", true );
			testEmailAddress( "gregsmalterredstaplerbiz", false );
			testEmailAddress( "gregsmalter@redstaplerbiz", false );
			testEmailAddress( "greg smalter@redstapler.biz", false );
			testEmailAddress( "@redstapler.biz", false );
			testEmailAddress( "greg..smalter@redstapler.biz", false );
			testEmailAddress( ".gregsmalter@redstapler.biz", false );
			testEmailAddress( "gregsmalter.@redstapler.biz", false );
			testEmailAddress( "greg.smalter@redstapler.bizgreg.smalter@redstapler.biz", false );
			testEmailAddress( ";;;;greg.smalter@redstapler.biz;;;", false );
			testEmailAddress( "1@2.com", true );
			testEmailAddress( "greg:smalter@redstapler.biz", false );
			testEmailAddress( "greg;smalter@redstapler.biz", false );
			testEmailAddress( "greg@rle.mit.edu", true );
			testEmailAddress( "greg@rle.mit..edu", false );
			testEmailAddress( "greg@edu", false );
			testEmailAddress( "greg@.rle.mit.edu", false );
			testEmailAddress( "greg@rle.mit.edu.", false );
		}

		private static void testEmailAddress( string email, bool shouldBeValid ) {
			var vp = new ValidationErrorHandler( "" );
			new Validator().GetEmailAddress( vp, email, false, 100 );
			Assert.IsTrue( ( vp.LastResult == ErrorCondition.NoError ) == shouldBeValid );
		}
	}
}