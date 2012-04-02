using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// Standard Library use only. One big reason for this interface is that ReSharper gets confused when code in a web app contains calls to the copy of the
	/// master page in that web app.
	/// </summary>
	public interface AppEwfUiMasterPage {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		void SetContentFootActions( params ActionButtonSetup[] actions );

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		void SetContentFootControls( params Control[] controls );
	}
}