using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;

// Parameter: int userId

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.Public {
	partial class ConfirmPasswordReset: EwfPage {
		partial class Info {
			public override string ResourceName { get { return ""; } }
		}

		protected override void loadData() {
			var pb = PostBack.CreateFull( actionGetter: () => new PostBackAction( new ExternalResourceInfo( es.info.DestinationUrl ) ) );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Reset Password", new PostBackButton( pb ) ) );

			pb.AddModificationMethod( modifyData );
		}

		private void modifyData() {
			FormsAuthStatics.ResetAndSendPassword( info.UserId );
			AddStatusMessage( StatusMessageType.Info, "Your new password has been sent to your email address." );
		}
	}
}