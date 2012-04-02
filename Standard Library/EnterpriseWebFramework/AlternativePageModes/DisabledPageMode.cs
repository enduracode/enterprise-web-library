namespace RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes {
	/// <summary>
	/// A mode that prevents a page or entity setup from being accessed.
	/// </summary>
	public class DisabledPageMode: AlternativePageMode {
		private readonly string message;

		/// <summary>
		/// Creates a disabled page mode object.
		/// </summary>
		public DisabledPageMode( string message ) {
			this.message = message;
		}

		/// <summary>
		/// Gets the message.
		/// </summary>
		public string Message { get { return message; } }
	}
}