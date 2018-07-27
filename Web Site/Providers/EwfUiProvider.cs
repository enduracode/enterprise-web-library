using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.Providers {
	internal class EwfUiProvider: AppEwfUiProvider {
		public override IReadOnlyCollection<ActionComponentSetup> GetGlobalNavActions() {
			var navButtonSetups = new List<ActionComponentSetup>();
			if( CreateSystem.GetInfo().IsIdenticalToCurrent() )
				return navButtonSetups;

			// This will hide itself because Contact Us requires a logged-in user, and the standard library test web site has no users.
			var contactPage = EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ContactSupport.GetInfo( EwfPage.Instance.InfoAsBaseType.GetUrl() );
			navButtonSetups.Add( new HyperlinkSetup( contactPage, contactPage.ResourceName ) );

			navButtonSetups.Add(
				new ButtonSetup(
					"Test",
					behavior: new MenuButtonBehavior(
						new EwfButton(
							new StandardButtonStyle( "Test method" ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull(
									id: "testMethod",
									firstModificationMethod: () => EwfPage.AddStatusMessage( StatusMessageType.Info, "Successful method execution." ) ) ) ).ToCollection() ) ) );

			return navButtonSetups;
		}

		public override List<LookupBoxSetup> GetGlobalNavLookupBoxSetups() {
			var lookupBoxSetups = new List<LookupBoxSetup>();
			if( CreateSystem.GetInfo().IsIdenticalToCurrent() )
				return lookupBoxSetups;

			lookupBoxSetups.Add( new LookupBoxSetup( 100, "test", "lookup1", delegate { return null; } ) );
			lookupBoxSetups.Add( new LookupBoxSetup( 100, "test", "lookup2", delegate { return null; } ) );
			return lookupBoxSetups;
		}
	}
}