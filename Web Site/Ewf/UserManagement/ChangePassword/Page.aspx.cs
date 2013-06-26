using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.ChangePassword {
	public partial class Page: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return ""; } }
			protected override bool userCanAccessPage { get { return AppTools.User != null; } }
		}

		private DataValue<string> newPassword;

		protected override void LoadData( DBConnection cn ) {
			var dm = new DataModification();
			var fib = FormItemBlock.CreateFormItemTable();

			newPassword = new DataValue<string>();
			fib.AddFormItems( FormItem.Create( "New password",
			                                   new EwfTextBox( "" ) { MasksCharacters = true },
			                                   validationGetter: control => new Validation( ( pbv, v ) => newPassword.Value = control.GetPostBackValue( pbv ), dm ) ) );
			var newPasswordConfirm = new DataValue<string>();
			fib.AddFormItems( FormItem.Create( "Re-type new password",
			                                   new EwfTextBox( "" ) { MasksCharacters = true },
			                                   validationGetter:
				                                   control => new Validation( ( pbv, v ) => newPasswordConfirm.Value = control.GetPostBackValue( pbv ), dm ) ) );
			dm.AddTopValidationMethod( ( pbv, validator ) => UserManagementStatics.ValidatePassword( validator, newPassword, newPasswordConfirm ) );

			ph.AddControlsReturnThis( fib );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Change Password",
			                                                           new PostBackButton( dm,
			                                                                               () => EhRedirect( new ExternalPageInfo( es.info.ReturnAndDestinationUrl ) ) ) ) );

			dm.AddModificationMethod( modifyData );
		}

		private void modifyData( DBConnection cn ) {
			var password = new Password( newPassword.Value );
			( UserManagementStatics.SystemProvider as FormsAuthCapableUserManagementProvider ).InsertOrUpdateUser( AppTools.User.UserId,
			                                                                                                       AppTools.User.Email,
			                                                                                                       password.Salt,
			                                                                                                       password.ComputeSaltedHash(),
			                                                                                                       AppTools.User.Role.RoleId,
			                                                                                                       AppTools.User.LastRequestDateTime,
			                                                                                                       false );
			AddStatusMessage( StatusMessageType.Info, "Your password has been successfully changed. Use it the next time you log in." );
		}
	}
}