using System;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;

// Parameter: string emailAddress
// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	partial class ConfirmPasswordReset: EwfPage {
		partial class Info {
			internal User User { get; private set; }

			protected override void init() {
				User = UserManagementStatics.GetUser( EmailAddress );
				if( User == null )
					throw new ApplicationException( "emailAddress" );
			}
		}

		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Paragraph(
					StringTools.ConcatenateWithDelimiter(
						" ",
						"Click \"Reset Password\" to email yourself a new password.",
						"Upon receiving your new password, you may immediately use it to log in.",
						"You will then be prompted to change your password to something you will remember, which you may use to log in from that point forward." ) ) );

			EwfUiStatics.SetContentFootActions(
				new ActionButtonSetup(
					"Reset Password",
					new PostBackButton(
						PostBack.CreateFull(
							firstModificationMethod: () => {
								FormsAuthStatics.ResetAndSendPassword( info.User.UserId );
								AddStatusMessage( StatusMessageType.Info, "Your new password has been sent to your email address." );
							},
							actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl ) ) ) ) ) );
		}
	}
}