using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Ui {
	/// <summary>
	/// EWL use only. One big reason for this interface is that ReSharper gets confused when code in a web app contains calls to the copy of the master page in
	/// that web app.
	/// </summary>
	public interface AppEwfUiMasterPage {
		/// <summary>
		/// EWL use only.
		/// </summary>
		void SetPageActions( IReadOnlyCollection<UiActionSetup> actions );

		/// <summary>
		/// EWL use only.
		/// </summary>
		void SetContentFootActions( IReadOnlyCollection<UiButtonSetup> actions );

		/// <summary>
		/// EWL use only.
		/// </summary>
		void SetContentFootControls( params Control[] controls );
	}
}