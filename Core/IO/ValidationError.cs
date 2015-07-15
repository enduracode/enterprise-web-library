namespace EnterpriseWebLibrary.IO {
	/// <summary>
	/// Holds informations about a validation error generated as a result of processing a parsed line.
	/// </summary>
	public class ValidationError {
		private readonly string errorSource;
		private readonly bool isFatal;
		private readonly string errorMessage;

		/// <summary>
		/// Creates a validation error that occurred when processing the given line number.
		/// Error source is an explanation of the place in the original data that caused the error.  For example, "Line 32".
		/// </summary>
		public ValidationError( string errorSource, bool isFatal, string errorMessage ) {
			this.errorSource = errorSource;
			this.errorMessage = errorMessage;
			this.isFatal = isFatal;
		}

		/// <summary>
		/// An explanation of the place in the original data that caused the error.  For example, "Line 32".
		/// </summary>
		public string ErrorSource { get { return errorSource; } }

		/// <summary>
		/// True if this is a fatal error.
		/// </summary>
		public bool IsFatal { get { return isFatal; } }

		/// <summary>
		/// The error message.
		/// </summary>
		public string ErrorMessage { get { return errorMessage; } }
	}
}