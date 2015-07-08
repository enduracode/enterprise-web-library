using System;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// A value that knows whether it has been initialized and whether it has changed.
	/// </summary>
	public class DataValue<T>: IEquatable<DataValue<T>> {
		private readonly InitializationAwareValue<T> val = new InitializationAwareValue<T>();

		public bool Changed { get; private set; }

		public T Value {
			get { return val.Value; }
			set {
				if( val.Initialized && StandardLibraryMethods.AreEqual( val.Value, value ) )
					return;
				val.Value = value;
				Changed = true;
			}
		}

		public void ClearChanged() {
			Changed = false;
		}

		public override bool Equals( object obj ) {
			return Equals( obj as DataValue<T> );
		}

		public bool Equals( DataValue<T> other ) {
			return other != null && StandardLibraryMethods.AreEqual( val, other.val );
		}

		public override int GetHashCode() {
			return val.GetHashCode();
		}
	}
}