using System;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Apply this attribute to the value of an Enum.
	/// </summary>
	public class EnglishAttribute: Attribute {
		public readonly string English;

		public EnglishAttribute( string english ) {
			English = english;
		}
	}
}