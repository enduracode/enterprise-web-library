using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// Implement this in your <see cref="AppEwfUiProvider"/> to override the EWF UI app-logo and user-info control.
	/// </summary>
	public interface AppLogoAndUserInfoControlOverrider {
		Control GetAppLogoAndUserInfoControl();
	}
}