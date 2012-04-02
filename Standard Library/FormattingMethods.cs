using System;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Contains methods that prepare data for display. Data is assumed to be valid.
	/// </summary>
	public static class FormattingMethods {
		/// <summary>
		/// Formats the specified phone number in the 555-555-5555 x1234 style. Does not accept null.
		/// </summary>
		public static string GetPhoneWithDashes( string standardPhoneString ) {
			var phoneNumber = PhoneNumber.CreateFromStandardPhoneString( standardPhoneString );
			return GetPhoneWithDashesFromObject( phoneNumber );
		}

		internal static string GetPhoneWithDashesFromObject( PhoneNumber phoneNumber ) {
			// If anyone ever asks for leading ones to be shown (1-800-555-5555), do it by using a known list of area codes (800, 888).
			var formattedPhoneString = "";
			if( !phoneNumber.Empty ) {
				if( phoneNumber.IsInternational )
					formattedPhoneString = phoneNumber.InternationalNumber;
				else {
					formattedPhoneString += phoneNumber.AreaCode + "-";
					formattedPhoneString += phoneNumber.Number.Insert( 3, "-" );
					formattedPhoneString += phoneNumber.Extension.Length == 0 ? "" : ( " x" + phoneNumber.Extension );
				}
			}
			return formattedPhoneString;
		}

		/// <summary>
		/// Formats the specified phone number in the (555) 555-5555 x1234 style. Does not accept null.
		/// </summary>
		public static string GetPhoneWithAcParens( string standardPhoneString ) {
			var phoneNumber = PhoneNumber.CreateFromStandardPhoneString( standardPhoneString );
			var formattedPhoneString = "";
			if( phoneNumber.IsInternational )
				formattedPhoneString = phoneNumber.InternationalNumber;
			else {
				if( phoneNumber.AreaCode.Length > 0 )
					formattedPhoneString += "(" + phoneNumber.AreaCode + ") ";
				if( phoneNumber.Number.Length > 0 )
					formattedPhoneString += phoneNumber.Number.Insert( 3, "-" );
				if( phoneNumber.Extension.Length > 0 )
					formattedPhoneString += " x" + phoneNumber.Extension;
			}
			return formattedPhoneString;
		}

		/// <summary>
		/// Formats the specified phone number in the 555.555.5555 x1234 style. Does not accept null.
		/// </summary>
		public static string GetPhoneWithDots( string standardPhoneString ) {
			var phoneNumber = PhoneNumber.CreateFromStandardPhoneString( standardPhoneString );
			var formattedPhoneString = "";
			if( !phoneNumber.Empty ) {
				if( phoneNumber.IsInternational )
					formattedPhoneString = phoneNumber.InternationalNumber;
				else {
					formattedPhoneString += phoneNumber.AreaCode + "." + phoneNumber.Number.Insert( 3, "." );
					formattedPhoneString += phoneNumber.Extension.Length == 0 ? "" : ( " x" + phoneNumber.Extension );
				}
			}
			return formattedPhoneString;
		}

		/// <summary>
		/// Extracts the extension digits from the specified phone number. Does not accept null.
		/// </summary>
		public static string GetPhoneExtension( string standardPhoneString ) {
			return PhoneNumber.CreateFromStandardPhoneString( standardPhoneString ).Extension;
		}

		/// <summary>
		/// Formats the specified social security number with dashes. Accepts the empty string, but does not accept null.
		/// </summary>
		public static string GetSocialSecurityNumberWithDashes( string ssn ) {
			if( ssn.Length > 0 )
				return ssn.Substring( 0, 3 ) + "-" + ssn.Substring( 3, 2 ) + "-" + ssn.Substring( 5, 4 );
			return "";
		}

		/// <summary>
		/// Formats the specified address in multi line format. Do not pass null for any parameters.
		/// </summary>
		public static string GetAddressWithNewLines( string deliveryAddress, string city, string stateAbbreviation, string zipCode, string addOnCode ) {
			// The add on code means nothing without the ZIP Code.
			if( zipCode.Length == 0 )
				addOnCode = "";

			return StringTools.ConcatenateWithDelimiter( Environment.NewLine,
			                                             deliveryAddress,
			                                             StringTools.ConcatenateWithDelimiter( " ",
			                                                                                   StringTools.ConcatenateWithDelimiter( ", ", city, stateAbbreviation ),
			                                                                                   StringTools.ConcatenateWithDelimiter( "-", zipCode, addOnCode ) ) );
		}

		/// <summary>
		/// Formats the specified address in a single-line format. Do not pass null for any parameters.
		/// </summary>
		public static string GetAddressOneLine( string deliveryAddress, string city, string stateAbbreviation, string zipCode, string addOnCode ) {
			return GetAddressWithNewLines( deliveryAddress, city, stateAbbreviation, zipCode, addOnCode ).Replace( Environment.NewLine, ", " );
		}

		/// <summary>
		/// Uses GetFormattedBytes to return a string in the form "60.1 GB/s".
		/// </summary>
		public static string GetFormattedBytesPerSecond( long numberOfBytes, TimeSpan elapsedTime ) {
			return GetFormattedBytes( (long)( numberOfBytes / elapsedTime.TotalSeconds ) ) + "/s";
		}

		/// <summary>
		/// Returns the given number of bytes in the most useful way possible. For example, 64 will return 64 bytes. 64,000 will return 62 KB.
		/// 64,500,000,000 will return 60.1 GB. Maximum precision is 3 significant digits. GB is the largest unit returned.
		/// </summary>
		public static string GetFormattedBytes( long numberOfBytes ) {
			const int stepMultiplier = 1024;
			const string doubleFormattingString = "G3";

			if( numberOfBytes < stepMultiplier )
				return numberOfBytes + " bytes";
			if( numberOfBytes < Math.Pow( stepMultiplier, 2 ) )
				return ( numberOfBytes / stepMultiplier ).ToString( doubleFormattingString ) + " KB";
			if( numberOfBytes < Math.Pow( stepMultiplier, 3 ) )
				return ( numberOfBytes / Math.Pow( stepMultiplier, 2 ) ).ToString( doubleFormattingString ) + " MB";
			return ( numberOfBytes / Math.Pow( stepMultiplier, 3 ) ).ToString( doubleFormattingString ) + " GB";
		}
	}
}