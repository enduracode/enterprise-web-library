namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface FormControl<out T> where T: PageComponent {
		T PageComponent { get; }
		EwfValidation Validation { get; }
	}
}