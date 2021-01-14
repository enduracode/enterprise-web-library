using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class ElementAttribute {
		internal readonly string Name;
		internal readonly string Value;

		/// <summary>
		/// Creates an attribute.
		/// </summary>
		/// <param name="name">Do not pass null or the empty string.</param>
		/// <param name="value">Do not pass null.</param>
		public ElementAttribute( string name, string value ) {
			Name = name;
			Value = value;
		}

		/// <summary>
		/// Creates a boolean attribute.
		/// </summary>
		/// <param name="name">Do not pass null or the empty string.</param>
		public ElementAttribute( string name ) {
			Name = name;
		}
	}

	public static class ElementAttributeExtensionCreators {
		/// <summary>
		/// Concatenates attributes.
		/// </summary>
		public static IEnumerable<ElementAttribute> Concat( this ElementAttribute first, IEnumerable<ElementAttribute> second ) => second.Prepend( first );

		/// <summary>
		/// Returns a sequence of two attributes.
		/// </summary>
		public static IEnumerable<ElementAttribute> Append( this ElementAttribute first, ElementAttribute second ) =>
			Enumerable.Empty<ElementAttribute>().Append( first ).Append( second );
	}
}