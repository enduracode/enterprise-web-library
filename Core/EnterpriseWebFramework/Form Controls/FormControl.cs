#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface FormControl<out T> where T: PageComponent {
		/// <summary>
		/// Gets the labeler, or null if this control is not labelable.
		/// </summary>
		FormControlLabeler Labeler { get; }

		/// <summary>
		/// Gets the page component.
		/// </summary>
		T PageComponent { get; }

		/// <summary>
		/// Gets the validation, or null if there isn't one.
		/// </summary>
		EwfValidation Validation { get; }
	}
}