#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class NavFormControlValidationResult {
		internal ResourceInfo Destination { get; }
		internal string ErrorMessage { get; }

		/// <summary>
		/// Creates a successful-validation result.
		/// </summary>
		/// <param name="destination">Do not pass null.</param>
		public NavFormControlValidationResult( ResourceInfo destination ) {
			Destination = destination;
		}

		/// <summary>
		/// Creates a failed-validation result.
		/// </summary>
		/// <param name="errorMessage">Do not pass null or the empty string.</param>
		public NavFormControlValidationResult( string errorMessage ) {
			ErrorMessage = errorMessage;
		}
	}
}