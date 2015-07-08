namespace RedStapler.StandardLibrary.MailMerging {
	/// <summary>
	/// An exception that is thrown during a mail merging operation and that is caused by a template problem.
	/// </summary>
	public class MailMergingException: MultiMessageApplicationException {
		/// <summary>
		/// Creates a mail merging exception with the specified messages.
		/// </summary>
		public MailMergingException( params string[] messages ): base( messages ) {}
	}
}