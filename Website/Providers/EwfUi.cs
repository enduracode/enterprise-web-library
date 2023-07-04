using EnterpriseWebLibrary.WebSessionState;

namespace EnterpriseWebLibrary.Website.Providers;

internal class EwfUi: AppEwfUiProvider {
	public override IReadOnlyCollection<ActionComponentSetup> GetGlobalNavActions( string postBackIdBase ) {
		var navButtonSetups = new List<ActionComponentSetup>();
		if( CreateSystem.GetInfo().MatchesCurrent() )
			return navButtonSetups;

		// This will hide itself because Contact Us requires a logged-in user, and this website has no users.
		var contactPage = ContactSupport.GetInfo( PageBase.Current.GetUrl() );
		navButtonSetups.Add( new HyperlinkSetup( contactPage, contactPage.ResourceName ) );

		navButtonSetups.Add(
			new ButtonSetup(
				"Test",
				behavior: new MenuButtonBehavior(
					new EwfButton(
						new StandardButtonStyle( "Test method" ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull(
								id: PostBack.GetCompositeId( postBackIdBase, "testMethod" ),
								modificationMethod: () => PageBase.AddStatusMessage( StatusMessageType.Info, "Successful method execution." ) ) ) ).ToCollection() ) ) );

		return navButtonSetups;
	}

	public override IReadOnlyCollection<NavFormControl> GetGlobalNavFormControls() {
		var controls = new List<NavFormControl>();
		if( CreateSystem.GetInfo().MatchesCurrent() )
			return controls;

		controls.Add(
			NavFormControl.CreateText(
				new NavFormControlSetup( 100.ToPixels(), "test" ),
				_ => new NavFormControlValidationResult( "This doesn’t actually work." ) ) );
		controls.Add(
			NavFormControl.CreateText(
				new NavFormControlSetup( 100.ToPixels(), "test" ),
				_ => new NavFormControlValidationResult( "This doesn’t actually work." ) ) );
		return controls;
	}
}