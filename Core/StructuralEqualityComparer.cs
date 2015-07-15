using System.Collections;
using System.Collections.Generic;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// A generic version of StructuralComparisons.StructuralEqualityComparer.
	/// </summary>
	public class StructuralEqualityComparer<T>: EqualityComparer<T> {
		public override bool Equals( T x, T y ) {
			return StructuralComparisons.StructuralEqualityComparer.Equals( x, y );
		}

		public override int GetHashCode( T obj ) {
			return StructuralComparisons.StructuralEqualityComparer.GetHashCode( obj );
		}
	}
}