using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.Public;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	public partial class LogIn: EwfPage {
		private FormsAuthCapableUserManagementProvider provider;
		private DataValue<string> emailAddress;
		private DataValue<string> password;
		private FormsAuthCapableUser user;

		protected override void loadData() {
			provider = (FormsAuthCapableUserManagementProvider)UserManagementStatics.SystemProvider;
			var logInDm = new DataModification();
			var newPasswordDm = new DataModification();

			var registeredTable = EwfTable.Create( caption: "Registered users" );
			registeredTable.AddItem(
				new EwfTableItem( new EwfTableCell( "You may log in to this system if you have registered your email address with " + provider.AdministratingCompanyName )
					{
						FieldSpan = 2
					} ) );

			emailAddress = new DataValue<string>();
			var emailVl = new BasicValidationList();
			registeredTable.AddItem( new EwfTableItem( "Email address".ToCell(),
			                                           emailAddress.GetEmailAddressFormItem( "", "Please enter a valid email address.", emailVl ).ToControl().ToCell() ) );
			logInDm.AddValidations( emailVl );
			newPasswordDm.AddValidations( emailVl );

			password = new DataValue<string>();
			registeredTable.AddItem( new EwfTableItem( "Password".ToCell(),
			                                           FormItem.Create( "",
			                                                            new EwfTextBox( "" ) { MasksCharacters = true },
			                                                            validationGetter:
				                                                            control =>
				                                                            new Validation( ( pbv, v ) => password.Value = control.GetPostBackValue( pbv ), logInDm ) )
			                                                   .ToControl()
			                                                   .ToCell() ) );

			registeredTable.AddItem(
				new EwfTableItem(
					new EwfTableCell(
						new PlaceHolder().AddControlsReturnThis(
							"If you are a first-time user and do not know your password, or if you have forgotten your password, ".GetLiteralControl(),
							new PostBackButton( newPasswordDm,
							                    handleSendNewPasswordClick,
							                    new TextActionControlStyle( "click here to immediately send yourself a new password" ),
							                    usesSubmitBehavior: false ) ) ) { FieldSpan = 2 } ) );
			ph.AddControlsReturnThis( registeredTable );

			var specialInstructions = EwfUiStatics.AppProvider.GetSpecialInstructionsForLogInPage();
			if( specialInstructions != null )
				ph.AddControlsReturnThis( specialInstructions );
			else {
				var unregisteredTable = EwfTable.Create( caption: "Unregistered users" );
				unregisteredTable.AddItem( new EwfTableItem( ( "If you have difficulty logging in, please " + provider.LogInHelpInstructions ).ToCell() ) );
				ph.AddControlsReturnThis( unregisteredTable );
			}

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Log In",
			                                                           new PostBackButton( logInDm,
			                                                                               () =>
			                                                                               EhRedirect( user.MustChangePassword
				                                                                                           ? ChangePassword.Page.GetInfo( info.ReturnUrl ) as PageInfo
				                                                                                           : new ExternalPageInfo( info.ReturnUrl ) ) ) ) );

			UserManagementStatics.SetUpClientSideLogicForLogInPostBack();

			logInDm.AddModificationMethod( modifyData );
		}

		private void handleSendNewPasswordClick() {
			EhModifyDataAndRedirect( delegate {
				var userLocal = UserManagementStatics.GetUser( emailAddress.Value );
				if( userLocal == null )
					throw new EwfException( getUnregisteredEmailMessage() );
				return ConfirmPasswordReset.GetInfo( info.ReturnUrl, userLocal.UserId ).GetUrl();
			} );
		}

		private void modifyData() {
			user = UserManagementStatics.LogInUser( emailAddress,
			                                        password,
			                                        getUnregisteredEmailMessage(),
			                                        "Incorrect password. If you do not know your password, enter your email address and send yourself a new password using the link below." );
		}

		private string getUnregisteredEmailMessage() {
			return "The email address you entered is not registered.  You must register the email address with " + provider.AdministratingCompanyName +
			       " before using it to log in.  To do this, " + provider.LogInHelpInstructions;
		}
	}
}