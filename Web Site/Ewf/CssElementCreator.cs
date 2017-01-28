using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	internal class CssElementCreator: ControlCssElementCreator {
		internal const string SelectUserPageBodyCssClass = "ewfSelectUser";
		internal const string ErrorPageBodyCssClass = "ewfError";

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			return new[]
				{ new CssElement( "SelectUserPageBody", "body." + SelectUserPageBodyCssClass ), new CssElement( "ErrorPageBody", "body." + ErrorPageBodyCssClass ) };
		}
	}
}