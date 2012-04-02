using System.Collections;

namespace RedStapler.StandardLibrary.Validation {
	/// <summary>
	/// Common data required to validate and build an error message for a piece of data.
	/// </summary>
	public class ValidationPackage {
		private readonly string subject;
		private readonly IDictionary customMessages;
		private ErrorCondition errorCondition = ErrorCondition.NoError;

		/// <summary>
		/// The subject of the error message, if one needs to be generated.
		/// </summary>
		public string Subject { get { return subject; } }

		/// <summary>
		/// The map of ErrorConditions to error messages overrides.
		/// </summary>
		public IDictionary CustomMessages { get { return customMessages; } }

		/// <summary>
		/// Returns the ErrorCondition resulting from the validation of the data
		/// associated with this package.
		/// </summary>
		public ErrorCondition ValidationResult { set { errorCondition = value; } get { return errorCondition; } }

		/// <summary>
		/// Create a new package with which to validate a piece of data.
		/// </summary>
		/// <param name="subject">The subject of the error message, if one needs to be generated.</param>
		/// <param name="customMessages">The map of ErrorConditions to error messages overrides. Use null for no overrides.</param>
		public ValidationPackage( string subject, IDictionary customMessages ) {
			this.subject = subject;
			this.customMessages = customMessages;
		}

		/// <summary>
		/// Create a new package with which to validate a piece of data without
		/// specifying any custom messages overrides.
		/// </summary>
		/// <param name="subject">The subject of the error message, if one needs to be generated.</param>
		public ValidationPackage( string subject ): this( subject, null ) {}
	}
}