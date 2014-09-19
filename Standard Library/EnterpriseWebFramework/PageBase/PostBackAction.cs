namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class PostBackAction {
		private readonly PageInfo page;
		private readonly SecondaryResponse secondaryResponse;

		/// <summary>
		/// Creates an action that will navigate to the specified page.
		/// </summary>
		/// <param name="page">Pass null for no navigation.</param>
		public PostBackAction( PageInfo page ) {
			this.page = page;
		}

		/// <summary>
		/// Creates an action that will send a secondary response in a new window/tab or as an attachment.
		/// </summary>
		/// <param name="response">The secondary response.</param>
		public PostBackAction( SecondaryResponse response ) {
			secondaryResponse = response;
		}

		internal PageInfo Page { get { return page; } }
		internal SecondaryResponse SecondaryResponse { get { return secondaryResponse; } }
	}
}