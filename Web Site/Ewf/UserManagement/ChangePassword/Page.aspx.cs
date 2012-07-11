using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.ChangePassword {
	public partial class Page: EwfPage, DataModifierWithRightButton {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return ""; } }
			protected override bool userCanAccessPage { get { return AppTools.User != null; } }
		}

		protected override void LoadData( DBConnection cn ) {
			newPassword.MasksCharacters = newPasswordConfirm.MasksCharacters = true;
		}

		string DataModifierWithRightButton.RightButtonText { get { return "Change Password"; } }

		void DataModifierWithRightButton.ValidateFormValues( Validator validator ) {
			UserManagementStatics.ValidatePassword( validator, newPassword, newPasswordConfirm );
		}

		string DataModifierWithRightButton.ModifyData( DBConnection cn ) {
			var password = new Password( newPassword.Value );
			( UserManagementStatics.SystemProvider as FormsAuthCapableUserManagementProvider ).InsertOrUpdateUser( cn,
			                                                                                                       AppTools.User.UserId,
			                                                                                                       AppTools.User.Email,
			                                                                                                       password.Salt,
			                                                                                                       password.ComputeSaltedHash(),
			                                                                                                       AppTools.User.Role.RoleId,
			                                                                                                       AppTools.User.LastRequestDateTime,
			                                                                                                       false );
			StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Info, "Your password has been successfully changed. Use it the next time you log in." );
			return es.info.ReturnAndDestinationUrl;
		}
	}
}