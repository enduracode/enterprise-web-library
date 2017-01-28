using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.ChangePassword {
	partial class Page: EwfPage {
		partial class Info {
			public override string ResourceName { get { return ""; } }
			protected override bool userCanAccessResource { get { return AppTools.User != null; } }
		}

		private DataValue<string> newPassword;

		protected override void loadData() {
			var pb = PostBack.CreateFull(
				firstModificationMethod: modifyData,
				actionGetter: () => new PostBackAction( new ExternalResourceInfo( es.info.ReturnAndDestinationUrl ) ) );
			ValidationSetupState.ExecuteWithDataModifications(
				pb.ToCollection(),
				() => {
					newPassword = new DataValue<string>();
					ph.AddControlsReturnThis(
						FormItemBlock.CreateFormItemTable(
							formItems: newPassword.GetPasswordModificationFormItems( firstLabel: "New password", secondLabel: "Re-type new password" ) ) );
					EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Change Password", new PostBackButton() ) );
				} );
		}

		private void modifyData() {
			var password = new Password( newPassword.Value );
			FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
				AppTools.User.UserId,
				AppTools.User.Email,
				AppTools.User.Role.RoleId,
				AppTools.User.LastRequestDateTime,
				password.Salt,
				password.ComputeSaltedHash(),
				false );
			AddStatusMessage( StatusMessageType.Info, "Your password has been successfully changed. Use it the next time you log in." );
		}
	}
}