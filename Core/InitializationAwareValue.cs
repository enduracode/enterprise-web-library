using System;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// A value that knows whether it has been initialized.
	/// </summary>
	public class InitializationAwareValue<T>: IEquatable<InitializationAwareValue<T>> {
		private T value;

		/// <summary>
		/// Gets whether the value has been initialized.
		/// </summary>
		public bool Initialized { get; private set; }

		// See http://blogs.msdn.com/b/ericlippert/archive/2011/02/28/guidelines-and-rules-for-gethashcode.aspx and http://stackoverflow.com/a/3235535/35349.
		private bool hashed;

		/// <summary>
		/// Gets or sets the value. Throws an exception if you try to get the value before it has been initialized.
		/// </summary>
		public T Value {
			get {
				if( !Initialized )
					throw new ApplicationException( "Value has not been initialized." );
				return value;
			}
			set {
				if( hashed )
					throw new ApplicationException( "Object has been hashed and therefore cannot be changed." );
				this.value = value;
				Initialized = true;
			}
		}

		public override bool Equals( object obj ) {
			return Equals( obj as InitializationAwareValue<T> );
		}

		public bool Equals( InitializationAwareValue<T> other ) {
			return other != null && Initialized && other.Initialized && EwlStatics.AreEqual( value, other.value );
		}

		public override int GetHashCode() {
			// ReSharper disable NonReadonlyFieldInGetHashCode
			hashed = true;
			return value.GetHashCode();
			// ReSharper restore NonReadonlyFieldInGetHashCode
		}
	}
}