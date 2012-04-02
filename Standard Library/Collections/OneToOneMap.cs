using System.Collections.Generic;

namespace RedStapler.StandardLibrary.Collections {
	/// <summary>
	/// A collection of value pairs that provides bidirectional lookup.  Preserves order.
	/// All left values must be unique.
	/// All right values must be unique.
	/// </summary>
	public class OneToOneMap<T1, T2> {
		private readonly Dictionary<T1, T2> leftToRight = new Dictionary<T1, T2>();
		private readonly Dictionary<T2, T1> rightToLeft = new Dictionary<T2, T1>();
		private readonly List<T1> leftValues = new List<T1>();
		private readonly List<T2> rightValues = new List<T2>();
		private readonly List<KeyValuePair<T1, T2>> pairs = new List<KeyValuePair<T1, T2>>();

		/// <summary>
		/// Add a pair of values.
		/// </summary>
		public void Add( T1 left, T2 right ) {
			leftToRight.Add( left, right );
			rightToLeft.Add( right, left );
			leftValues.Add( left );
			rightValues.Add( right );
			pairs.Add( new KeyValuePair<T1, T2>( left, right ) );
		}

		/// <summary>
		/// Given the right value of a pair, returns the corresponding left value.
		/// </summary>
		public T1 GetLeftFromRight( T2 right ) {
			return rightToLeft[ right ];
		}

		/// <summary>
		/// Given the left value of a pair, returns the corresponding right value.
		/// </summary>
		public T2 GetRightFromLeft( T1 left ) {
			return leftToRight[ left ];
		}

		/// <summary>
		/// Returns a collection of all of the left values.
		/// </summary>
		public ICollection<T1> GetAllLeftValues() {
			return leftValues.AsReadOnly();
		}

		/// <summary>
		/// Returns a collection of all of the right values.
		/// </summary>
		public ICollection<T2> GetAllRightValues() {
			return rightValues.AsReadOnly();
		}

		/// <summary>
		/// Returns a list of (left, right) key value pairs.
		/// </summary>
		public ICollection<KeyValuePair<T1, T2>> GetAllPairs() {
			return pairs.AsReadOnly();
		}
	}
}