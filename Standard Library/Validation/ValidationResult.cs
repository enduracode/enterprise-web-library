namespace RedStapler.StandardLibrary.Validation {
	internal class ValidationResult {
		private ErrorCondition errorCondition = ErrorCondition.NoError;
		private string errorMessage = "";

		private ValidationResult() {}

		public string GetErrorMessage( string subject ) {
			return string.Format( errorMessage, subject );
		}

		public ErrorCondition ErrorCondition { get { return errorCondition; } }

		public static ValidationResult Custom( ErrorCondition errorCondition, string errorMessage ) {
			var result = new ValidationResult { errorCondition = errorCondition, errorMessage = errorMessage };
			return result;
		}

		public static ValidationResult NoError() {
			return new ValidationResult();
		}

		public static ValidationResult Invalid() {
			var result = new ValidationResult { errorCondition = ErrorCondition.Invalid, errorMessage = "Please enter a valid {0}." };
			return result;
		}

		public static ValidationResult Empty() {
			var result = new ValidationResult { errorCondition = ErrorCondition.Empty, errorMessage = "Please enter the {0}." };
			return result;
		}

		public static ValidationResult TooSmall( object min, object max ) {
			var result = new ValidationResult
			             	{ errorCondition = ErrorCondition.TooLong, errorMessage = "The {0} must be between " + min + " and " + max + " (inclusive)." };
			return result;
		}

		public static ValidationResult TooLarge( object min, object max ) {
			var result = new ValidationResult
			             	{ errorCondition = ErrorCondition.TooLarge, errorMessage = "The {0} must be between " + min + " and " + max + " (inclusive)." };
			return result;
		}
	}
}