namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An exception that is thrown during data modification.
	/// </summary>
	public class DataModificationException: MultiMessageApplicationException {
		/// <summary>
		/// Creates a new exception with the specified message.
		/// </summary>
		public DataModificationException( string message ): base( new[] { message } ) {}

		/// <summary>
		/// Creates a new exception with the specified messages.
		/// </summary>
		public DataModificationException( params string[] messages ): base( messages ) {}
	}
}