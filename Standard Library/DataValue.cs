using System;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Standard Library use only.
	/// </summary>
	public class DataValue<T> {
		private T val;
		private bool initialized;

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public bool Changed { get; private set; }

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public T Value {
			get {
				if( !initialized )
					throw new ApplicationException( "Column value has not been initialized." );
				return val;
			}
			set {
				if( !initialized || !Equals( value, val ) ) {
					val = value;
					initialized = true;
					Changed = true;
				}
			}
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public void ClearChanged() {
			Changed = false;
		}
	}
}