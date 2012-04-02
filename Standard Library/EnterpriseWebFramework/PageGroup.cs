namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A group of pages.
	/// </summary>
	public class PageGroup {
		private readonly string name;
		private readonly PageInfo[] pages;

		/// <summary>
		/// Creates a page group.
		/// </summary>
		public PageGroup( string name, params PageInfo[] pages ) {
			this.name = name;
			this.pages = pages;
		}

		/// <summary>
		/// Creates a page group.
		/// </summary>
		public PageGroup( params PageInfo[] pages ): this( "", pages ) {}

		/// <summary>
		/// Gets the name of the page group.
		/// </summary>
		public string Name { get { return name; } }

		/// <summary>
		/// Gets the list of pages.
		/// </summary>
		public PageInfo[] Pages { get { return pages; } }
	}
}