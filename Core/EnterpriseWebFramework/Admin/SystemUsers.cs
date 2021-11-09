using System.Collections.Generic;
using EnterpriseWebLibrary.UserManagement;
using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin {
	partial class SystemUsers {
		protected override AlternativeResourceMode createAlternativeMode() =>
			UserManagementStatics.UserManagementEnabled ? null : new DisabledResourceMode( "User management is not enabled in this system." );

		protected override IEnumerable<UrlPattern> getChildUrlPatterns() => SystemUser.UrlPatterns.UserIdPositiveInt( Es, "create" ).ToCollection();

		protected override PageContent getContent() =>
			new UiPageContent().Add(
				EwfTable.Create(
						tableActions: new HyperlinkSetup( new SystemUser( Es, null ), "Create User" ).ToCollection(),
						headItems: EwfTableItem.Create( "Email".ToCell().Append( "Role".ToCell() ).Materialize() ).ToCollection() )
					.AddData(
						UserManagementStatics.SystemProvider.GetUsers(),
						user => EwfTableItem.Create(
							user.Email.ToCell().Append( user.Role.Name.ToCell() ).Materialize(),
							setup: EwfTableItemSetup.Create( activationBehavior: ElementActivationBehavior.CreateHyperlink( new SystemUser( Es, user.UserId ) ) ) ) ) );
	}
}