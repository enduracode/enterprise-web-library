using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.UserManagement.UserManager {
	public partial class Users: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			protected override bool userCanAccessPage { get { return AppTools.User != null && AppTools.User.Role.CanManageUsers; } }
		}

		protected override void LoadData( DBConnection cn ) {
			userTable.SetUpColumns( new EwfTableColumn( new EwfTableCell( "Email" ), Unit.Percentage( 50 ) ),
			                        new EwfTableColumn( new EwfTableCell( "Role" ), Unit.Percentage( 50 ) ) );
			userTable.AddActionLink( new ActionButtonSetup( "Add new User", new EwfLink( UserNs.Edit.GetInfo( null ) ) ) );
			foreach( var user in UserManagementStatics.GetUsers( cn ) )
				userTable.AddTextRow( new RowSetup { ClickScript = ClickScript.CreateRedirectScript( UserNs.Edit.GetInfo( user.UserId ) ) }, user.Email, user.Role.Name );
		}
	}
}