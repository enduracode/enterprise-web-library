using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.WebSessionState;

// Parameter: int userId

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.Public {
	public partial class ConfirmPasswordReset: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return ""; } }
		}

		protected override void LoadData( DBConnection cn ) {
			var dm = new DataModification();
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Reset Password",
			                                                           new PostBackButton( dm, () => EhRedirect( new ExternalPageInfo( es.info.DestinationUrl ) ) ) ) );

			dm.AddModificationMethod( modifyData );
		}

		private void modifyData() {
			UserManagementStatics.ResetAndSendPassword( info.UserId );
			AddStatusMessage( StatusMessageType.Info, "Your new password has been sent to your email address." );
		}
	}
}