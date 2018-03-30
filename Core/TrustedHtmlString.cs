namespace EnterpriseWebLibrary {
	/// <summary>
	/// A trusted HTML string.
	/// </summary>
	public sealed class TrustedHtmlString {
		public string Html { get; }

		/// <summary>
		/// Creates a trusted HTML string.
		/// </summary>
		/// <param name="html">Do not pass null.</param>
		public TrustedHtmlString( string html ) {
			Html = html;
		}
	}
}