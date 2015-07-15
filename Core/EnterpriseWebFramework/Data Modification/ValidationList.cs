namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A list of validations.
	/// </summary>
	public interface ValidationList {
		/// <summary>
		/// Adds all validations from the specified basic validation list.
		/// </summary>
		void AddValidations( BasicValidationList validationList );
	}

	internal interface ValidationListInternal {
		void AddValidation( Validation validation );
	}
}