using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.WebSite.TestPages;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite {
	public class Global: EwfApp {
		// These methods exist because there is no way to hook into these events from within EWF.
		protected void Application_Start( object sender, EventArgs e ) {
			ewfApplicationStart( new GlobalLogic() );
		}

		protected void Application_End( object sender, EventArgs e ) {
			ewfApplicationEnd();
		}

		protected override void initializeWebApp() {}

		protected override IEnumerable<ShortcutUrlResolver> GetShortcutUrlResolvers() {
			yield return new ShortcutUrlResolver(
				"",
				ConnectionSecurity.SecureIfPossible,
				() => {
					var page = ActionControls.GetInfo();
					return page.UserCanAccessResource ? page : null;
				} );

			foreach( var i in GlobalLogic.ConfigurationXsdFileNames ) {
				var fileName = i;
				yield return
					new ShortcutUrlResolver( "ConfigurationSchemas/" + fileName.EnglishToPascal(), ConnectionSecurity.NonSecure, () => GetSchema.GetInfo( fileName ) );
			}
		}

		protected override List<ResourceInfo> GetStyleSheets() {
			return new List<ResourceInfo> { new TestCss.Info() };
		}
	}
}