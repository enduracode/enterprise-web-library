#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An action triggered by a form control or button.
	/// </summary>
	public interface FormAction {
		void AddToPageIfNecessary();
		string GetJsStatements();
	}
}