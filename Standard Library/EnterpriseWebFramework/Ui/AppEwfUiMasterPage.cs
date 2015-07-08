using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// EWL use only. One big reason for this interface is that ReSharper gets confused when code in a web app contains calls to the copy of the master page in
	/// that web app.
	/// </summary>
	public interface AppEwfUiMasterPage {
		/// <summary>
		/// EWL use only.
		/// </summary>
		void SetPageActions( params ActionButtonSetup[] actions );

		/// <summary>
		/// EWL use only.
		/// </summary>
		void SetContentFootActions( params ActionButtonSetup[] actions );

		/// <summary>
		/// EWL use only.
		/// </summary>
		void SetContentFootControls( params Control[] controls );
	}
}