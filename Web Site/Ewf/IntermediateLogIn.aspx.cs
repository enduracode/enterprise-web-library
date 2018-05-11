using System;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;

// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class IntermediateLogIn: EwfPage {
		partial class Info {
			protected override void init() {
				// This guarantees that the page will always be secure, even for non intermediate installations.
				if( !EwfConfigurationStatics.AppSupportsSecureConnections )
					throw new ApplicationException();
			}

			public override string ResourceName => "Non-Live Installation Log In";
			protected override bool IsIntermediateInstallationPublicResource => true;
		}

		protected override void loadData() {
			var pb = PostBack.CreateFull(
				firstModificationMethod: () => {
					IntermediateAuthenticationMethods.SetCookie();
					AppRequestState.Instance.IntermediateUserExists = true;
				},
				actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl ) ) );
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				pb.ToCollection(),
				() => {
					ph.AddControlsReturnThis(
						FormItemBlock.CreateFormItemTable(
							formItems: new TextControl(
									"",
									true,
									setup: TextControlSetup.CreateObscured(),
									validationMethod: ( postBackValue, validator ) => {
										// NOTE: Using a single password here is a hack. The real solution is being able to use RSIS credentials, which is a goal.
										var passwordMatch = postBackValue == ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword;
										if( !passwordMatch )
											validator.NoteErrorAndAddMessage( "Incorrect password." );
									} ).ToFormItem( label: "Enter your password for this non-live installation".ToComponents() )
								.ToCollection() ) );
				} );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Log In", new PostBackButton( pb ) ) );
		}
	}
}