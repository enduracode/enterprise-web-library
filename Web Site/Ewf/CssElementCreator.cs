using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	internal class CssElementCreator: ControlCssElementCreator {
		internal static readonly ElementClass SelectUserPageBodyClass = new ElementClass( "ewfSelectUser" );
		internal static readonly ElementClass ErrorPageBodyClass = new ElementClass( "ewfError" );

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
			new[]
				{
					new CssElement( "SelectUserPageBody", "body." + SelectUserPageBodyClass.ClassName ),
					new CssElement( "ErrorPageBody", "body." + ErrorPageBodyClass.ClassName )
				};
	}
}