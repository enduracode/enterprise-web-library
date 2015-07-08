using System;
using System.Text.RegularExpressions;

namespace RedStapler.StandardLibrary.Validation {
	/// <summary>
	/// A zip code.
	/// </summary>
	public class ZipCode {
		private const string usPattern = @"(?<zip>^\d{5})(-?(?<plus4>\d{4}))?$";
		private const string canadianPattern = @"(?<caZip>^[ABCEGHJKLMNPRSTVXY]{1}\d{1}[A-Z]{1} *\d{1}[A-Z]{1}\d{1}$)";

		private string zip = String.Empty;
		private string plus4 = String.Empty;

		/// <summary>
		/// The normal 5-digit zip code.
		/// </summary>
		public string Zip { get { return zip; } }

		/// <summary>
		/// The optional 4-digit extension. NOTE: Who uses this?
		/// </summary>
		public string Plus4 { get { return plus4; } }

		/// <summary>
		/// The full zip code with 4-digit extension (12345-6789).
		/// </summary>
		public string FullZipCode { get { return StringTools.ConcatenateWithDelimiter( "-", zip, plus4 ); } }

		internal ZipCode() {}

		internal static ZipCode CreateUsZipCode( ValidationErrorHandler errorHandler, string entireZipCode ) {
			var match = Regex.Match( entireZipCode, usPattern );
			if( match.Success )
				return getZipCodeFromValidUsMatch( match );

			return getZipCodeForFailure( errorHandler );
		}

		internal static ZipCode CreateUsOrCanadianZipCode( ValidationErrorHandler errorHandler, string entireZipCode ) {
			var match = Regex.Match( entireZipCode, usPattern );
			if( match.Success )
				return getZipCodeFromValidUsMatch( match );
			if( ( match = Regex.Match( entireZipCode, canadianPattern, RegexOptions.IgnoreCase ) ).Success )
				return new ZipCode { zip = match.Groups[ "caZip" ].Value };

			return getZipCodeForFailure( errorHandler );
		}

		private static ZipCode getZipCodeForFailure( ValidationErrorHandler errorHandler ) {
			errorHandler.SetValidationResult( ValidationResult.Invalid() );
			return new ZipCode();
		}

		private static ZipCode getZipCodeFromValidUsMatch( Match match ) {
			return new ZipCode { zip = match.Groups[ "zip" ].Value, plus4 = match.Groups[ "plus4" ].Value };
		}
	}
}