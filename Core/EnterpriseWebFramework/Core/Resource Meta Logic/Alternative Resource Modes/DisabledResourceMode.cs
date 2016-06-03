namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A mode that prevents a resource or entity setup from being accessed.
	/// </summary>
	public class DisabledResourceMode: AlternativeResourceMode {
		private readonly string message;

		/// <summary>
		/// Creates a disabled page mode object.
		/// </summary>
		public DisabledResourceMode( string message ) {
			this.message = message;
		}

		/// <summary>
		/// Gets the message.
		/// </summary>
		public string Message { get { return message; } }
	}
}