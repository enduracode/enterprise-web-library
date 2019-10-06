using System;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;

// Parameter: string returnUrl
// OptionalParameter: string password
// OptionalParameter: bool hideWarnings

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class IntermediateLogIn: EwfPage {
		partial class Info {
			protected override void init() {
				if( !ConfigurationStatics.IsIntermediateInstallation )
					throw new ApplicationException( "installation type" );

				if( Password.Any() && Password != ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword )
					throw new ApplicationException( "password" );
			}

			public override string ResourceName => "Non-Live Installation Log In";
			protected override bool IsIntermediateInstallationPublicResource => true;
		}

		private bool pageViewDataModificationsExecuted;

		protected override Action getPageViewDataModificationMethod() {
			pageViewDataModificationsExecuted = true;

			if( !info.Password.Any() )
				return null;
			return () => logIn( info.HideWarnings );
		}

		protected override void loadData() {
			if( info.Password.Any() ) {
				if( !pageViewDataModificationsExecuted )
					throw new ApplicationException( "Page-view data modifications did not execute." );

				ph.AddControlsReturnThis( new Paragraph( "Please wait.".ToComponents() ).ToCollection().GetControls() );
				StandardLibrarySessionState.Instance.SetInstantClientSideNavigation( new ExternalResourceInfo( info.ReturnUrl ).GetUrl() );
				return;
			}

			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull(
						firstModificationMethod: () => logIn( false ),
						actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl ) ) )
					.ToCollection(),
				() => {
					ph.AddControlsReturnThis(
						FormItemList.CreateStack(
								items: new TextControl(
										"",
										true,
										setup: TextControlSetup.CreateObscured(),
										validationMethod: ( postBackValue, validator ) => {
											// NOTE: Using a single password here is a hack. The real solution is being able to use RSIS credentials, which is a goal.
											var passwordMatch = postBackValue == ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword;
											if( !passwordMatch )
												validator.NoteErrorAndAddMessage( "Incorrect password." );
										} ).ToFormItem( label: "Enter your password for this non-live installation".ToComponents() )
									.ToCollection() )
							.ToCollection()
							.GetControls() );

					EwfUiStatics.SetContentFootActions( new ButtonSetup( "Log In" ).ToCollection() );
				} );
		}

		private void logIn( bool hideWarnings ) {
			NonLiveInstallationStatics.SetIntermediateAuthenticationCookie();
			AppRequestState.Instance.IntermediateUserExists = true;
			if( hideWarnings )
				NonLiveInstallationStatics.SetWarningsHiddenCookie();
		}
	}
}