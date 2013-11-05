using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class PostBackAction {
		private readonly PageInfo page;
		private readonly FileCreator file;

		/// <summary>
		/// Creates an action that will navigate to the specified page.
		/// </summary>
		/// <param name="page">Pass null for no navigation.</param>
		public PostBackAction( PageInfo page ) {
			this.page = page;
		}

		/// <summary>
		/// Creates an action that will send the specified file, using a client-side redirect.
		/// </summary>
		/// <param name="file"></param>
		public PostBackAction( FileCreator file ) {
			this.file = file;
		}

		internal PageInfo Page { get { return page; } }
		internal FileCreator File { get { return file; } }
	}
}