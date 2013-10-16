using System;

namespace RedStapler.StandardLibrary {

	/// <summary>
	/// Apply this attribute to the value of an Enum.
	/// </summary>
	public class EnumToEnglishAttribute: Attribute {
		public readonly string English;

		public EnumToEnglishAttribute( string english ) {
			English = english;
		}
	}
}