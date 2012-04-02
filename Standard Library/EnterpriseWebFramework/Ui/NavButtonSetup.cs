namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements {
	/// <summary>
	/// NOTE: Do not use this class. It will be deleted.
	/// </summary>
	public class NavButtonSetup {
		private readonly string text;
		private readonly PageInfo pageInfo;
		private readonly string url;

		/// <summary>
		/// Creates a nav button setup object with the specified text and URL.
		/// </summary>
		public NavButtonSetup( string text, PageInfo pageInfo ) {
			this.text = text;
			this.pageInfo = pageInfo;
		}

		/// <summary>
		/// Do not use. This will become obsolete.
		/// </summary>
		public NavButtonSetup( string text, string url ) {
			this.text = text;
			this.url = url;
		}

		/// <summary>
		/// The text to show.
		/// </summary>
		public string Text { get { return text; } }

		/// <summary>
		/// The URL to eventually navigate to.
		/// </summary>
		public string Url { get { return url ?? pageInfo.GetUrl(); } }
	}
}