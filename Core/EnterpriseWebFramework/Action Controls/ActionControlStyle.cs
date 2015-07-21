using System;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A display style for action controls.
	/// </summary>
	public interface ActionControlStyle {
		/// <summary>
		/// EWL use only. Gets the text. Returns the empty string if the style does not support text.
		/// </summary>
		string Text { get; }

		// NOTE: I find it very confusing that implementors of this both modify the given control and also return it. Only one client to this method uses the return value.
		/// <summary>
		/// Sets up the specified action control with this style. Returns the control containing the text.
		/// </summary>
		WebControl SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter );

		/// <summary>
		/// EWL use only. Gets the JavaScript init statements for the style.
		/// </summary>
		string GetJsInitStatements( WebControl controlForGetClientUrl );
	}
}