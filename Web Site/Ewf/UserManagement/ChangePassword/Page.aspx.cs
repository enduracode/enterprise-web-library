using RedStapler.StandardLibrary.Encryption;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.ChangePassword {
	partial class Page: EwfPage {
		partial class Info {
			public override string ResourceName { get { return ""; } }
			protected override bool userCanAccessResource { get { return AppTools.User != null; } }
		}

		private DataValue<string> newPassword;

		protected override void loadData() {
			var pb = PostBack.CreateFull( actionGetter: () => new PostBackAction( new ExternalResourceInfo( es.info.ReturnAndDestinationUrl ) ) );
			var fib = FormItemBlock.CreateFormItemTable();

			newPassword = new DataValue<string>();
			fib.AddFormItems(
				FormItem.Create(
					"New password".GetLiteralControl(),
					new EwfTextBox( "", masksCharacters: true ),
					validationGetter: control => new Validation( ( pbv, v ) => newPassword.Value = control.GetPostBackValue( pbv ), pb ) ) );
			var newPasswordConfirm = new DataValue<string>();
			fib.AddFormItems(
				FormItem.Create(
					"Re-type new password".GetLiteralControl(),
					new EwfTextBox( "", masksCharacters: true ),
					validationGetter: control => new Validation( ( pbv, v ) => newPasswordConfirm.Value = control.GetPostBackValue( pbv ), pb ) ) );
			pb.AddTopValidationMethod( ( pbv, validator ) => FormsAuthStatics.ValidatePassword( validator, newPassword, newPasswordConfirm ) );

			ph.AddControlsReturnThis( fib );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Change Password", new PostBackButton( pb ) ) );

			pb.AddModificationMethod( modifyData );
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