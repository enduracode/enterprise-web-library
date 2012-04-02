namespace RedStapler.StandardLibrary.Configuration {
	/// <summary>
	/// A type of installation.
	/// </summary>
	public enum InstallationType {
		/// <summary>
		/// A development installation.
		/// </summary>
		Development,

		/// <summary>
		/// A live installation.
		/// </summary>
		Live,

		/// <summary>
		/// An intermediate installation, such as a testing or demonstration installation.
		/// </summary>
		Intermediate
	}
}