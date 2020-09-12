using System.Collections.Generic;
using System.Collections.Immutable;

namespace EnterpriseWebLibrary.TewlContrib {
	public static class CollectionTools {
		/// <summary>
		/// Creates a list from this sequence, enabling elements to be accessed by index.
		/// </summary>
		public static IReadOnlyList<T> MaterializeAsList<T>( this IEnumerable<T> items ) => items.ToImmutableArray();
	}
}