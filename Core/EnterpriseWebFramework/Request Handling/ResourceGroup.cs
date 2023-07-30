#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A group of resources.
	/// </summary>
	public class ResourceGroup {
		private readonly string name;
		private readonly ResourceBase[] resources;

		/// <summary>
		/// Creates a resource group.
		/// </summary>
		public ResourceGroup( string name, params ResourceBase[] resources ) {
			this.name = name;
			this.resources = resources;
		}

		/// <summary>
		/// Creates a resource group.
		/// </summary>
		public ResourceGroup( params ResourceBase[] resources ): this( "", resources ) {}

		/// <summary>
		/// Gets the name of the resource group.
		/// </summary>
		public string Name { get { return name; } }

		/// <summary>
		/// Gets the list of resources.
		/// </summary>
		public ResourceBase[] Resources { get { return resources; } }
	}
}