using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public static class ErrorMessageControlListBlockStatics {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public class CssElementCreator: ControlCssElementCreator {
			internal const string CssClass = "ewfErrorMessageListBlock";

			/// <summary>
			/// EWL use only.
			/// </summary>
			public static readonly string[] Selectors = ( "div." + CssClass ).ToSingleElementArray();

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new CssElement( "ErrorMessageControlListBlock", Selectors ).ToSingleElementArray();
			}
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static Block CreateErrorMessageListBlock( IEnumerable<string> errors ) {
			// Client code that uses NetTools.BuildBasicLink depends on us not HTML encoding error messages here. If raw or stored user input is ever used in error
			// messages, we are exposed to injection attacks.
			return
				new Block(
					ControlStack.CreateWithControls(
						true,
						errors.Select(
							i =>
							(Control)new PlaceHolder().AddControlsReturnThis( new FontAwesomeIcon( "fa-times-circle", "fa-lg" ), " ".GetLiteralControl(), new Literal { Text = i } ) )
							.ToArray() ) ) { CssClass = CssElementCreator.CssClass };
		}
	}
}