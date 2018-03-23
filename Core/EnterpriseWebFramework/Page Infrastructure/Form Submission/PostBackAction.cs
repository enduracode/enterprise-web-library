namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class PostBackAction {
		internal ResourceInfo Resource { get; }
		internal PageReloadBehavior ReloadBehavior { get; }

		/// <summary>
		/// Creates an action that will navigate to the specified resource.
		/// </summary>
		/// <param name="resource">Pass null for no navigation.</param>
		public PostBackAction( ResourceInfo resource ) {
			Resource = resource;
		}

		/// <summary>
		/// Creates an action that will reload the page.
		/// </summary>
		/// <param name="reloadBehavior">The reload behavior.</param>
		public PostBackAction( PageReloadBehavior reloadBehavior ) {
			ReloadBehavior = reloadBehavior;
		}
	}
}