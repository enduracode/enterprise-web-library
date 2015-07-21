using System;

namespace EnterpriseWebLibrary.InputValidation {
	/// <summary>
	/// Represents a phone number consiting of area code, number, and optional extension.
	/// Also supports international numbers.  If IsInternational is true, area code, number, and extension are irrelevant.
	/// </summary>
	public class PhoneNumber {
		private string areaCode = "";
		private string number = "";
		private string extension = "";

		private PhoneNumber() {}

		/// <summary>
		/// Creates a phone number object from the individual parts. Strings are trimmed in case they came from SQL Server char fields.
		/// </summary>
		public static PhoneNumber CreateFromParts( string areaCode, string number, string extension ) {
			// NOTE: We may want to consider getting rid of the lead entirely, because I think a lead implies a US number, and we dump +1 now, so why not dump simply 1?
			return new PhoneNumber { areaCode = areaCode.Trim(), number = number.Trim(), extension = extension.Trim() };
		}

		/// <summary>
		/// Creates an international phone number.  Do not pass null for internationalNumber.
		/// </summary>
		internal static PhoneNumber CreateInternational( string internationalNumber ) {
			return new PhoneNumber { InternationalNumber = internationalNumber.Trim() };
		}

		/// <summary>
		/// Area code. Will never be null and will only be empty if the whole phone number is empty or the phone number is international.
		/// </summary>
		public string AreaCode { get { return areaCode; } }

		/// <summary>
		/// Seven-digit phone number. Will never be null and will only be empty if the whole phone number is empty or the phone number is international.
		/// </summary>
		public string Number { get { return number; } }

		/// <summary>
		/// Optional extension. Will never be null, but may be the empty string.
		/// </summary>
		public string Extension { get { return extension; } }

		/// <summary>
		/// Returns true if this number is international.  If his is true, area code, number, and extension are irrelevant.
		/// </summary>
		public bool IsInternational { get { return InternationalNumber != null; } }

		/// <summary>
		/// Returns the international number.  Has a value if IsInternational is true.  All other fields are irrelevant if this has a value.
		/// </summary>
		public string InternationalNumber { get; private set; }

		// These members deal with formatted phone number strings

		/// <summary>
		/// Creates a phone number object from a standard phone number string, presumably from a database or similar source.
		/// </summary>
		public static PhoneNumber CreateFromStandardPhoneString( string phoneString ) {
			var v = new Validator();
			// NOTE: I don't like how this method is called, which calls a method in validator, which then calls one of the static constructors back here (but not this one! otherwise you are screwed)
			var p = v.GetPhoneNumberAsObject( new ValidationErrorHandler( "" ), phoneString, true, true, false, null );
			// pass "" for the error message subject because we should never get errors
			if( v.ErrorsOccurred )
				throw new ApplicationException( "Unparsable standard phone number string encountered." );
			return p;
		}

		/// <summary>
		/// Returns the standard phone number string for this phone number object, presumably for storage in a database or similar source. Uses 555-555-5555 x1234 style.
		/// </summary>
		public string StandardPhoneString { get { return FormattingMethods.GetPhoneWithDashesFromObject( this ); } }

		/// <summary>
		/// Returns true if this phone number object is empty.
		/// </summary>
		public bool Empty { get { return ( IsInternational && InternationalNumber.Length == 0 ) || ( !IsInternational && number.Length == 0 ); } }
	}
}