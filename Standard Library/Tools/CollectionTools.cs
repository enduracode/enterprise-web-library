using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Tools for collections.
	/// </summary>
	public static class CollectionTools {
		/// <summary>
		/// Sorts the list alphabetically (ascending) based on the ToString value of each element.
		/// </summary>
		public static void SortAlphabetically<T>( this List<T> list ) {
			list.Sort( ( one, two ) => one.ToString().CompareTo( two.ToString() ) );
		}

		/// <summary>
		/// Returns null if the enumeration has no elements.
		/// </summary>
		public static T GetRandomElement<T>( this IEnumerable<T> enumeration ) {
			return enumeration.ElementAtOrDefault( Randomness.GetRandomInt( 0, enumeration.Count() ) );
		}

		/// <summary>
		/// Adds default(T) to the given list until the desired length is reached.
		/// </summary>
		public static List<T> Pad<T>( this IEnumerable<T> enumeration, int length ) {
			return enumeration.Pad( length, () => default( T ) );
		}

		/// <summary>
		/// Adds the given placeholder item to the given list until the desired length is reached.
		/// </summary>
		public static List<T> Pad<T>( this IEnumerable<T> enumeration, int length, Func<T> getNewPlaceholderItem ) {
			var list = new List<T>( enumeration );
			while( list.Count() < length )
				list.Add( getNewPlaceholderItem() );
			return list;
		}

		/// <summary>
		/// Creates a shallow copy of the enumeration, scrambles and returns it.
		/// </summary>
		public static IEnumerable<T> Scramble<T>( this IEnumerable<T> items ) {
			var itemsCopy = new List<T>( items );
			for( var i = 0; i < itemsCopy.Count; i++ ) {
				var randomIndex = Randomness.GetRandomInt( 0, itemsCopy.Count );
				var temp = itemsCopy[ randomIndex ];
				itemsCopy[ randomIndex ] = itemsCopy[ i ];
				itemsCopy[ i ] = temp;
			}
			return itemsCopy;
		}

		/// <summary>
		/// Transforms an IEnumerable into an IEnumerable of Tuple of two items while maintaining the order of the IEnumerable.
		/// </summary>
		public static IEnumerable<Tuple<A, B>> ToTupleEnumeration<A, B, T>( this IEnumerable<T> enumerable, Func<T, A> item1Selector, Func<T, B> item2Selector ) {
			return enumerable.Select( e => Tuple.Create( item1Selector( e ), item2Selector( e ) ) );
		}

		/// <summary>
		/// Gets the values that appear more than once in this sequence.
		/// </summary>
		public static IEnumerable<T> GetDuplicates<T>( this IEnumerable<T> items ) {
			return items.GroupBy( i => i ).Where( i => i.Count() > 1 ).Select( i => i.Key );
		}

		/// <summary>
		/// Returns the last <paramref name="n"/> elements.
		/// </summary>
		public static IEnumerable<T> TakeLast<T>( this IEnumerable<T> c, int n ) {
			if( n < 0 )
				throw new ApplicationException( "'n' was {0} which is less than zero.".FormatWith( n ) );
			var count = c.Count();
			return c.Skip( count - n );
		}
	}
}