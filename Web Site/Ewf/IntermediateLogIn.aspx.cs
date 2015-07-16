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

			public override string ResourceName { get { return "Non-Live Installation Log In"; } }
			protected override bool IsIntermediateInstallationPublicResource { get { return true; } }
		}

		protected override void loadData() {
			var pb = PostBack.CreateFull( actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl ) ) );

			ph.AddControlsReturnThis(
				FormItemBlock.CreateFormItemTable(
					formItems:
						FormItem.Create(
							"Enter your password for this non-live installation",
							new EwfTextBox( "", masksCharacters: true ),
							validationGetter: control => new EwfValidation(
								                             ( pbv, validator ) => {
									                             // NOTE: Using a single password here is a hack. The real solution is being able to use RSIS credentials, which is a goal.
									                             var passwordMatch = control.GetPostBackValue( pbv ) ==
									                                                 ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword;
									                             if( !passwordMatch )
										                             validator.NoteErrorAndAddMessage( "Incorrect password." );
								                             },
								                             pb ) ).ToSingleElementArray() ) );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Log In", new PostBackButton( pb ) ) );

			pb.AddModificationMethod(
				() => {
					IntermediateAuthenticationMethods.SetCookie();
					AppRequestState.Instance.IntermediateUserExists = true;
				} );
		}
	}
}