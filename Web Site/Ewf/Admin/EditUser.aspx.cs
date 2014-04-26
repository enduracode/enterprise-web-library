using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

// Parameter: int? userId

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class EditUser: EwfPage {
		public partial class Info {
			internal User User { get; private set; }

			protected override void init() {
				if( UserId.HasValue )
					User = UserManagementStatics.GetUser( UserId.Value, true );
			}

			protected override PageInfo createParentPageInfo() {
				return new SystemUsers.Info( esInfo );
			}

			public override string PageName { get { return User == null ? "New User" : User.Email; } }
		}

		private UserFieldTable userFieldTable;

		protected override void loadData() {
			if( info.UserId.HasValue ) {
				EwfUiStatics.SetPageActions(
					new ActionButtonSetup(
						"Delete User",
						new PostBackButton(
							PostBack.CreateFull( id: "delete", firstModificationMethod: deleteUser, actionGetter: () => new PostBackAction( new SystemUsers.Info( es.info ) ) ) ) ) );
			}

			var pb = PostBack.CreateFull( actionGetter: () => new PostBackAction( es.info.ParentPage ) );

			userFieldTable = new UserFieldTable();
			userFieldTable.LoadData( info.UserId, pb );
			ph.AddControlsReturnThis( userFieldTable );
			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "OK", new PostBackButton( pb ) ) );

			pb.AddModificationMethod( modifyData );
		}

		private void deleteUser() {
			UserManagementStatics.SystemProvider.DeleteUser( info.User.UserId );
		}

		private void modifyData() {
			if( FormsAuthStatics.FormsAuthEnabled ) {
				if( info.UserId.HasValue ) {
					FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
						info.User.UserId,
						userFieldTable.Email,
						userFieldTable.Salt,
						userFieldTable.SaltedPassword,
						userFieldTable.RoleId,
						info.User.LastRequestDateTime,
						userFieldTable.MustChangePassword );
				}
				else {
					FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
						null,
						userFieldTable.Email,
						userFieldTable.Salt,
						userFieldTable.SaltedPassword,
						userFieldTable.RoleId,
						null,
						userFieldTable.MustChangePassword );
				}
			}
			else if( UserManagementStatics.SystemProvider is ExternalAuthUserManagementProvider ) {
				var provider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;
				provider.InsertOrUpdateUser( info.UserId, userFieldTable.Email, userFieldTable.RoleId, info.User != null ? info.User.LastRequestDateTime : null );
			}
			userFieldTable.SendEmailIfNecessary();
		}
	}
}