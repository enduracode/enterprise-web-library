using System;

namespace RedStapler.StandardLibrary {
	public static class EnumTools {
		/// <summary>
		/// Converts this string to a given Enum Value. Case sensitive.
		/// </summary>
		/// C# doesn't allow constraining the value to an Enum
		public static T StringToEnumValue<T>( this string s ) {
			return (T)Enum.Parse( typeof( T ), s, false );
		}
	}
}