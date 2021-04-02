using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin {
	partial class SystemUsers {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() =>
				UserManagementStatics.UserManagementEnabled ? null : new DisabledResourceMode( "User management is not enabled in this system." );
		}

		protected override PageContent getContent() =>
			new UiPageContent().Add(
				EwfTable.Create(
						tableActions: new HyperlinkSetup( new SystemUser.Info( Es, null ), "Create User" ).ToCollection(),
						headItems: EwfTableItem.Create( "Email".ToCell().Append( "Role".ToCell() ).Materialize() ).ToCollection() )
					.AddData(
						UserManagementStatics.GetUsers(),
						user => EwfTableItem.Create(
							user.Email.ToCell().Append( user.Role.Name.ToCell() ).Materialize(),
							setup: EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( new SystemUser.Info( Es, user.UserId ) ) ) ) ) );
	}
}