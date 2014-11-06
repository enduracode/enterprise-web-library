using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.WebSessionState;

// Parameter: int userId

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.Public {
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