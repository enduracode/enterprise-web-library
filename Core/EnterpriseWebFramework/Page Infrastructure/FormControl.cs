namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface FormControl<out T> where T: PageComponent {
		FormValue FormValue { get; }
		T PageComponent { get; }
	}
}