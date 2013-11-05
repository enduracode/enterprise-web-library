using System;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	public partial class IntermediateLogIn: EwfPage {
		public partial class Info {
			protected override void init() {
				// This guarantees that the page will always be secure, even for non intermediate installations.
				if( !EwfApp.SupportsSecureConnections )
					throw new ApplicationException();
			}

			public override string PageName { get { return "Non-Live Installation Log In"; } }
			protected override bool IsIntermediateInstallationPublicPage { get { return true; } }
		}

		protected override void loadData() {
			var dm = PostBack.CreateFull();

			ph.AddControlsReturnThis(
				FormItemBlock.CreateFormItemTable(
					formItems:
						FormItem.Create( "Enter your password for this non-live installation",
						                 new EwfTextBox( "", masksCharacters: true ),
						                 validationGetter: control => new Validation( ( pbv, validator ) => {
							                 // NOTE: Using a single password here is a hack. The real solution is being able to use RSIS credentials, which is a goal.
							                 var passwordMatch = control.GetPostBackValue( pbv ) == AppTools.SystemProvider.IntermediateLogInPassword;
							                 if( !passwordMatch )
								                 validator.NoteErrorAndAddMessage( "Incorrect password." );
						                 },
						                                                              dm ) ).ToSingleElementArray() ) );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Log In", new PostBackButton( dm, () => EhRedirect( new ExternalPageInfo( info.ReturnUrl ) ) ) ) );

			dm.AddModificationMethod( () => {
				IntermediateAuthenticationMethods.SetCookie();
				AppRequestState.Instance.IntermediateUserExists = true;
			} );
		}
	}
}