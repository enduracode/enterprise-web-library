using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Tewl.Tools;

// Parameter: int? userId

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class EditUser: EwfPage {
		partial class Info {
			internal User User { get; private set; }

			protected override void init() {
				if( UserId.HasValue )
					User = UserManagementStatics.GetUser( UserId.Value, true );
			}

			protected override ResourceInfo createParentResourceInfo() {
				return new SystemUsers.Info( esInfo );
			}

			public override string ResourceName => User == null ? "New User" : User.Email;
		}

		private UserEditor userFieldTable;

		protected override void loadData() {
			if( info.UserId.HasValue )
				EwfUiStatics.SetPageActions(
					new ButtonSetup(
						"Delete User",
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull(
								id: "delete",
								firstModificationMethod: deleteUser,
								actionGetter: () => new PostBackAction( new SystemUsers.Info( es.info ) ) ) ) ).ToCollection() );

			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull( firstModificationMethod: modifyData, actionGetter: () => new PostBackAction( info.ParentResource ) ).ToCollection(),
				() => {
					userFieldTable = new UserEditor( info.UserId );
					ph.AddControlsReturnThis( userFieldTable.ToCollection().GetControls() );

					EwfUiStatics.SetContentFootActions( new ButtonSetup( "OK" ).ToCollection() );
				} );
		}

		private void deleteUser() {
			UserManagementStatics.SystemProvider.DeleteUser( info.User.UserId );
		}

		private void modifyData() {
			if( FormsAuthStatics.FormsAuthEnabled )
				if( info.UserId.HasValue )
					FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
						info.User.UserId,
						userFieldTable.Email.Value,
						userFieldTable.RoleId.Value,
						info.User.LastRequestTime,
						userFieldTable.Salt.Value,
						userFieldTable.SaltedPassword.Value,
						userFieldTable.MustChangePassword.Value );
				else
					FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
						null,
						userFieldTable.Email.Value,
						userFieldTable.RoleId.Value,
						null,
						userFieldTable.Salt.Value,
						userFieldTable.SaltedPassword.Value,
						userFieldTable.MustChangePassword.Value );
			else if( UserManagementStatics.SystemProvider is ExternalAuthUserManagementProvider ) {
				var provider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;
				provider.InsertOrUpdateUser( info.UserId, userFieldTable.Email.Value, userFieldTable.RoleId.Value, info.User?.LastRequestTime );
			}
			userFieldTable.SendEmailIfNecessary();
		}
	}
}