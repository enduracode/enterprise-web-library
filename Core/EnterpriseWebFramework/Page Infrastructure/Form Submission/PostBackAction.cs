namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class PostBackAction {
		internal ResourceInfo Resource { get; }
		internal SecondaryResponse SecondaryResponse { get; }

		/// <summary>
		/// Creates an action that will navigate to the specified resource.
		/// </summary>
		/// <param name="resource">Pass null for no navigation.</param>
		public PostBackAction( ResourceInfo resource ) {
			Resource = resource;
		}

		/// <summary>
		/// Creates an action that will send a secondary response in a new window/tab or as an attachment.
		/// </summary>
		/// <param name="response">The secondary response.</param>
		public PostBackAction( SecondaryResponse response ) {
			SecondaryResponse = response;
		}
	}
}