using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSite.TestPages;

namespace EnterpriseWebLibrary.WebSite {
	public class Global: EwfApp {
		// These methods exist because there is no way to hook into these events from within EWF.

		protected void Application_Start( object sender, EventArgs e ) {
			EwfInitializationOps.InitStatics( new GlobalInitializer() );
		}

		protected void Application_End( object sender, EventArgs e ) {
			EwfInitializationOps.CleanUpStatics();
		}


		protected override IEnumerable<ShortcutUrlResolver> GetShortcutUrlResolvers() {
			yield return new ShortcutUrlResolver(
				"",
				ConnectionSecurity.SecureIfPossible,
				() => {
					var page = ActionControls.GetInfo();
					return page.UserCanAccessResource ? page : null;
				} );

			yield return new ShortcutUrlResolver( "create-system", ConnectionSecurity.SecureIfPossible, () => CreateSystem.GetInfo() );

			foreach( var i in GlobalStatics.ConfigurationXsdFileNames ) {
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