namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A group of resources.
	/// </summary>
	public class ResourceGroup {
		private readonly string name;
		private readonly ResourceInfo[] resources;

		/// <summary>
		/// Creates a resource group.
		/// </summary>
		public ResourceGroup( string name, params ResourceInfo[] resources ) {
			this.name = name;
			this.resources = resources;
		}

		/// <summary>
		/// Creates a resource group.
		/// </summary>
		public ResourceGroup( params ResourceInfo[] resources ): this( "", resources ) {}

		/// <summary>
		/// Gets the name of the resource group.
		/// </summary>
		public string Name { get { return name; } }

		/// <summary>
		/// Gets the list of resources.
		/// </summary>
		public ResourceInfo[] Resources { get { return resources; } }
	}
}