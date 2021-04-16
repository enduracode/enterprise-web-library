using EnterpriseWebLibrary.Encryption;
using EnterpriseWebLibrary.WebSessionState;
using Tewl.Tools;

// EwlPage
// Parameter: string returnAndDestinationUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages {
	partial class ChangePassword {
		private DataValue<string> newPassword;

		protected override bool userCanAccessResource => AppTools.User != null;

		protected override PageContent getContent() {
			newPassword = new DataValue<string>();
			return FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull( modificationMethod: modifyData, actionGetter: () => new PostBackAction( new ExternalResource( ReturnAndDestinationUrl ) ) )
					.ToCollection(),
				() => new UiPageContent(
					pageActions: new HyperlinkSetup( new ExternalResource( ReturnAndDestinationUrl ), "Back" ).ToCollection(),
					contentFootActions: new ButtonSetup( "Change Password" ).ToCollection() ).Add(
					FormItemList.CreateStack(
						items: newPassword.GetPasswordModificationFormItems(
							firstLabel: "New password".ToComponents(),
							secondLabel: "Re-type new password".ToComponents() ) ) ) );
		}

		private void modifyData() {
			var password = new Password( newPassword.Value );
			FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
				AppTools.User.UserId,
				AppTools.User.Email,
				AppTools.User.Role.RoleId,
				AppTools.User.LastRequestTime,
				password.Salt,
				password.ComputeSaltedHash(),
				false );
			AddStatusMessage( StatusMessageType.Info, "Your password has been successfully changed. Use it the next time you log in." );
		}
	}
}