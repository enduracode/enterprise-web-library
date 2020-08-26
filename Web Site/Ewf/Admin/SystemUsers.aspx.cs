using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class SystemUsers: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() =>
				UserManagementStatics.UserManagementEnabled ? null : new DisabledResourceMode( "User management is not enabled in this system." );
		}

		protected override void loadData() {
			ph.AddControlsReturnThis(
				EwfTable
					.Create(
						tableActions: new HyperlinkSetup( new EditUser.Info( es.info, null ), "Create User" ).ToCollection(),
						headItems: EwfTableItem.Create( "Email".ToCell().Append( "Role".ToCell() ).Materialize() ).ToCollection() )
					.AddData(
						UserManagementStatics.GetUsers(),
						user => EwfTableItem.Create(
							user.Email.ToCell().Append( user.Role.Name.ToCell() ).Materialize(),
							setup: EwfTableItemSetup.Create(
								activationBehavior: ElementActivationBehavior.CreateRedirectScript( new EditUser.Info( es.info, user.UserId ) ) ) ) )
					.ToCollection()
					.GetControls() );
		}
	}
}