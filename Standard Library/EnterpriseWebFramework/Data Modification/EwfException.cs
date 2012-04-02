namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception that is thrown during data modification.
	/// </summary>
	public class EwfException: MultiMessageApplicationException {
		/// <summary>
		/// Creates a new exception with the specified message.
		/// </summary>
		public EwfException( string message ): base( new[] { message } ) {}

		/// <summary>
		/// Creates a new exception with the specified messages.
		/// </summary>
		public EwfException( params string[] messages ): base( messages ) {}
	}
}