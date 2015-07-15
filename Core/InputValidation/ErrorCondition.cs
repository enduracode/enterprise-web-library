namespace EnterpriseWebLibrary.InputValidation {
	/// <summary>
	/// The list of possible error types.
	/// </summary>
	public enum ErrorCondition {
		/// <summary>
		/// NoError
		/// </summary>
		NoError,

		/// <summary>
		/// Empty
		/// </summary>
		Empty,

		/// <summary>
		/// Invalid
		/// </summary>
		Invalid,

		/// <summary>
		/// TooLong
		/// </summary>
		TooLong,

		/// <summary>
		/// TooShort
		/// </summary>
		TooShort,

		/// <summary>
		/// TooSmall
		/// </summary>
		TooSmall,

		/// <summary>
		/// TooLarge
		/// </summary>
		TooLarge,

		/// <summary>
		/// TooEarly
		/// </summary>
		TooEarly,

		/// <summary>
		/// TooLate
		/// </summary>
		TooLate
	} ;
}