using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;
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

		public override IReadOnlyCollection<NavFormControl> GetGlobalNavFormControls() {
			var controls = new List<NavFormControl>();
			if( CreateSystem.GetInfo().IsIdenticalToCurrent() )
				return controls;

			controls.Add(
				NavFormControl.CreateText(
					new NavFormControlSetup( 100.ToPixels(), "test" ),
					v => new NavFormControlValidationResult( "This doesn’t actually work." ) ) );
			controls.Add(
				NavFormControl.CreateText(
					new NavFormControlSetup( 100.ToPixels(), "test" ),
					v => new NavFormControlValidationResult( "This doesn’t actually work." ) ) );
			return controls;
		}
	}
}