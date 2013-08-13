using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// Implement this in your <see cref="AppEwfUiProvider"/> to have full control over
	/// the logo and logged-in user state area of your EWF UI page.
	/// </summary>
	public interface HeaderBlockOverrider {
		Control GetControl();
	}
}