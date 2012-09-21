using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary {
	public class EnumTools {
		/// <summary>
		/// Gets the values of the specified enumeration type.
		/// </summary>
		// C# doesn't allow constraining the type to an Enum.
		public static IEnumerable<T> GetValues<T>() {
			return Enum.GetValues( typeof( T ) ).Cast<T>();
		}
	}
}