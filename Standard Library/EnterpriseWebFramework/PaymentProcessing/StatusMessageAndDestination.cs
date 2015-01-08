namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class StatusMessageAndDestination {
		private readonly string message;
		private readonly ResourceInfo destination;

		/// <summary>
		/// Creates a StatusMessageAndDestination.
		/// </summary>
		/// <param name="message">The status message. Do not pass null.</param>
		/// <param name="destination">The resource to which the user will be redirected. Pass null for no redirection.</param>
		public StatusMessageAndDestination( string message, ResourceInfo destination ) {
			this.message = message;
			this.destination = destination;
		}

		internal string Message { get { return message; } }
		internal ResourceInfo Destination { get { return destination; } }
	}
}