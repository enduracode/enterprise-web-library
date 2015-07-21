namespace EnterpriseWebLibrary.InputValidation {
	/// <summary>
	/// Represents a validation error.
	/// </summary>
	public class Error {
		private readonly string message;
		private readonly bool unusableValueReturned;

		internal Error( string message, bool unusableValueReturned ) {
			this.message = message;
			this.unusableValueReturned = unusableValueReturned;
		}

		/// <summary>
		/// The error message.
		/// </summary>
		public string Message { get { return message; } }

		/// <summary>
		/// Returns true if the error resulted in an unusable value being returned.
		/// </summary>
		public bool UnusableValueReturned { get { return unusableValueReturned; } }
	}
}