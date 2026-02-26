namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A post-back value.
	/// </summary>
	public sealed class PostBackValue<T> {
		private readonly T value;
		private readonly bool changedOnPostBack;

		/// <summary>
		/// Creates a post-back value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="changedOnPostBack">Pass true if the value changed on this post back.</param>
		public PostBackValue( T value, bool changedOnPostBack ) {
			this.value = value;
			this.changedOnPostBack = changedOnPostBack;
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		public T Value { get { return value; } }

		/// <summary>
		/// Gets whether the value changed on this post back.
		/// </summary>
		public bool ChangedOnPostBack { get { return changedOnPostBack; } }
	}
}