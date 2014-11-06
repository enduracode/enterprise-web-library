using System;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class IntermediateLogIn: EwfPage {
		partial class Info {
			protected override void init() {
				// This guarantees that the page will always be secure, even for non intermediate installations.
				if( !EwfApp.SupportsSecureConnections )
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
							"Enter your password for this non-live installation".GetLiteralControl(),
							new EwfTextBox( "", masksCharacters: true ),
							validationGetter: control => new Validation(
								                             ( pbv, validator ) => {
									                             // NOTE: Using a single password here is a hack. The real solution is being able to use RSIS credentials, which is a goal.
									                             var passwordMatch = control.GetPostBackValue( pbv ) == AppTools.SystemProvider.IntermediateLogInPassword;
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