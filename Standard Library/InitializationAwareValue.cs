using System;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// A value that knows whether it has been initialized.
	/// </summary>
	public class InitializationAwareValue<T> {
		private T value;

		/// <summary>
		/// Gets whether the value has been initialized.
		/// </summary>
		public bool Initialized { get; private set; }

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
				this.value = value;
				Initialized = true;
			}
		}
	}
}