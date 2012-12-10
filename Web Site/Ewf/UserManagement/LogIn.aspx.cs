using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.Public;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.Validation;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	public partial class LogIn: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		private FormsAuthCapableUserManagementProvider provider;
		private EwfTextBox emailAddress;
		private EwfTextBox password;
		private string validatedEmailAddress;
		private FormsAuthCapableUser user;

		protected override void LoadData( DBConnection cn ) {
			provider = (FormsAuthCapableUserManagementProvider)UserManagementStatics.SystemProvider;
			var dm = new DataModification();

			var registeredTable = EwfTable.Create( caption: "Registered users" );
			registeredTable.AddItem(
				new EwfTableItem( new EwfTableCell( "You may log in to this system if you have registered your email address with " + provider.AdministratingCompanyName )
					{
						FieldSpan = 2
					} ) );
			registeredTable.AddItem( new EwfTableItem( "Email address".ToCell(), ( emailAddress = new EwfTextBox( "" ) ).ToCell() ) );
			registeredTable.AddItem( new EwfTableItem( "Password".ToCell(), ( password = new EwfTextBox( "" ) { MasksCharacters = true } ).ToCell() ) );
			dm.AddTopValidationMethod(
				( pbv, validator ) =>
				validatedEmailAddress = UserManagementStatics.ValidateAndGetEmailAddress( validator, emailAddress, "Please enter a valid email address." ) );
			registeredTable.AddItem(
				new EwfTableItem(
					new EwfTableCell(
						new PlaceHolder().AddControlsReturnThis(
							"If you are a first-time user and do not know your password, or if you have forgotten your password, ".GetLiteralControl(),
							new PostBackButton( new DataModification(),
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
			                                                           new PostBackButton( dm,
			                                                                               () =>
			                                                                               EhRedirect( user.MustChangePassword
				                                                                                           ? ChangePassword.Page.GetInfo( info.ReturnUrl ) as PageInfo
				                                                                                           : new ExternalPageInfo( info.ReturnUrl ) ) ) ) );

			UserManagementStatics.SetUpClientSideLogicForLogInPostBack();

			dm.AddModificationMethod( modifyData );
		}

		private void handleSendNewPasswordClick() {
			EhValidateAndModifyDataAndRedirect(
				delegate( Validator validator ) { validatedEmailAddress = UserManagementStatics.ValidateAndGetEmailAddress( validator, emailAddress, "Please enter a valid email address." ); },
				delegate( DBConnection cn ) {
					var userLocal = UserManagementStatics.GetUser( cn, validatedEmailAddress );
					if( userLocal == null )
						throw new EwfException( getUnregisteredEmailMessage() );
					return ConfirmPasswordReset.GetInfo( info.ReturnUrl, userLocal.UserId ).GetUrl();
				} );
		}

		private void modifyData( DBConnection cn ) {
			user = UserManagementStatics.LogInUser( validatedEmailAddress,
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