using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control that displays the email address of the currently logged-in user as well as log out and change password links.
	/// </summary>
	public class UserInfo: Control, INamingContainer {
		/// <summary>
		/// Standard Library use only. Do not call if there is no authenticated user.
		/// </summary>
		public void LoadData( PageInfo changePasswordPage ) {
			this.AddControlsReturnThis( ( "Logged in as " + AppTools.User.Email ).GetLiteralControl() );
			if( !( UserManagementStatics.SystemProvider is FormsAuthCapableUserManagementProvider ) )
				return;
			this.AddControlsReturnThis( new LiteralControl( "&nbsp;&nbsp;" ),
			                            new EwfLink( changePasswordPage ) { Text = "Change password" },
			                            new LiteralControl( "&nbsp;&bull;&nbsp;" ),
			                            new PostBackButton( new DataModification(),
			                                                () => EwfPage.Instance.EhModifyDataAndRedirect( cn => {
			                                                	UserManagementStatics.LogOutUser();

			                                                	// NOTE: Is this the correct behavior if we are already on a public page?
			                                                	return NetTools.HomeUrl;
			                                                } ),
			                                                new TextActionControlStyle( "Log out" ),
			                                                false ) );
		}
	}
}