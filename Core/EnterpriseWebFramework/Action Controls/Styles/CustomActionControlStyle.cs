using System;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A style that renders action controls in a custom way.
	/// </summary>
	public class CustomActionControlStyle: ActionControlStyle {
		private readonly Action<WebControl> setUpControlMethod;

		/// <summary>
		/// Creates a custom action control style with the specified set up method. Do not pass null.
		/// </summary>
		public CustomActionControlStyle( Action<WebControl> setUpControlMethod ) {
			this.setUpControlMethod = setUpControlMethod;
		}

		string ActionControlStyle.Text { get { return ""; } }

		WebControl ActionControlStyle.SetUpControl( WebControl control, string defaultText, Unit width, Unit height, Action<Unit> widthSetter ) {
			control.CssClass = control.CssClass.ConcatenateWithSpace( CssElementCreator.AllStylesClass );
			setUpControlMethod( control );
			return null;
		}

		string ActionControlStyle.GetJsInitStatements( WebControl controlForGetClientUrl ) {
			return "";
		}
	}
}