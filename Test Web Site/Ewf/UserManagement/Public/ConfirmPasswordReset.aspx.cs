using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebSessionState;

// Parameter: int userId

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.UserManagement.Public {
	public partial class ConfirmPasswordReset: EwfPage, DataModifierWithRightButton {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return ""; } }
		}

		protected override void LoadData( DBConnection cn ) {}

		public string RightButtonText { get { return "Reset Password"; } }

		void DataModifierWithRightButton.ValidateFormValues( Validator validator ) {}

		string DataModifierWithRightButton.ModifyData( DBConnection cn ) {
			UserManagementStatics.ResetAndSendPassword( cn, info.UserId );
			StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Info, "Your new password has been sent to your email address." );
			return es.info.DestinationUrl;
		}
	}
}