using System;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// An exception that is thrown when an unexpected value is encountered, usually in a switch statement.
	/// </summary>
	public class UnexpectedValueException: ApplicationException {
		private static string getMessage( string valueType, string value ) {
			return "Unexpected {0}: '{1}'".FormatWith( valueType, value );
		}

		/// <summary>
		/// Creates an exception from a value type string and a value.
		/// </summary>
		/// <param name="valueType">Do not pass null.</param>
		/// <param name="value"></param>
		public UnexpectedValueException( string valueType, object value ): base( getMessage( valueType, value.ToString() ) ) {}

		/// <summary>
		/// Creates an exception from an enumeration value.
		/// </summary>
		/// <param name="value"></param>
		public UnexpectedValueException( Enum value ): base( getMessage( value.GetType().Name, value.ToEnglish() ) ) {}
	}
}