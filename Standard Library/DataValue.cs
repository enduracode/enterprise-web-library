namespace RedStapler.StandardLibrary {
	/// <summary>
	/// A value that knows whether it has been initialized and whether it has changed.
	/// </summary>
	public class DataValue<T> {
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
	}
}