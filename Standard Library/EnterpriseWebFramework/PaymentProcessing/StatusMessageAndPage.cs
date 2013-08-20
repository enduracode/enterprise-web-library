namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class StatusMessageAndPage {
		private readonly string message;
		private readonly PageInfo page;

		/// <summary>
		/// Creates a StatusMessageAndPage.
		/// </summary>
		/// <param name="message">The status message. Do not pass null.</param>
		/// <param name="page">The page to which the user will be redirected. Pass null for no redirection.</param>
		public StatusMessageAndPage( string message, PageInfo page ) {
			this.message = message;
			this.page = page;
		}

		internal string Message { get { return message; } }
		internal PageInfo Page { get { return page; } }
	}
}