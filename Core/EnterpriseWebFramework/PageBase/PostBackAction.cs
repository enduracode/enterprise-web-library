namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class PostBackAction {
		private readonly ResourceInfo resource;
		private readonly SecondaryResponse secondaryResponse;

		/// <summary>
		/// Creates an action that will navigate to the specified resource.
		/// </summary>
		/// <param name="resource">Pass null for no navigation.</param>
		public PostBackAction( ResourceInfo resource ) {
			this.resource = resource;
		}

		/// <summary>
		/// Creates an action that will send a secondary response in a new window/tab or as an attachment.
		/// </summary>
		/// <param name="response">The secondary response.</param>
		public PostBackAction( SecondaryResponse response ) {
			secondaryResponse = response;
		}

		internal ResourceInfo Resource { get { return resource; } }
		internal SecondaryResponse SecondaryResponse { get { return secondaryResponse; } }
	}
}