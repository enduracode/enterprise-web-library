using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.Public;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.Validation;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	public partial class LogIn: EwfPage, DataModifierWithRightButton {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		private FormsAuthCapableUserManagementProvider provider;
		private string validatedEmailAddress;

		protected override void LoadData( DBConnection cn ) {
			provider = (FormsAuthCapableUserManagementProvider)UserManagementStatics.SystemProvider;
			administratingCompanyName.Text = provider.AdministratingCompanyName;
			logInHelpInstructions.Text = provider.LogInHelpInstructions;
			password.MasksCharacters = true;
			sendNewPasswordButtonPlace.AddControlsReturnThis( new PostBackButton( new DataModification(),
			                                                                      handleSendNewPasswordClick,
			                                                                      new TextActionControlStyle( "click here to immediately send yourself a new password" ),
			                                                                      false ) );
			var specialInstructions = EwfUiStatics.AppProvider.GetSpecialInstructionsForLogInPage();
			if( specialInstructions != null ) {
				standardInstructions.Visible = false;
				specialInstructionsArea.Controls.Add( specialInstructions );
			}

			UserManagementStatics.SetUpClientSideLogicForLogInPostBack();
		}

		public string RightButtonText { get { return "Log In"; } }

		public void ValidateFormValues( Validator validator ) {
			validatedEmailAddress = UserManagementStatics.ValidateAndGetEmailAddress( validator, emailAddress, "Please enter a valid email address." );
		}

		public string ModifyData( DBConnection cn ) {
			var user = UserManagementStatics.LogInUser( validatedEmailAddress,
			                                            password,
			                                            getUnregisteredEmailMessage(),
			                                            "Incorrect password. If you do not know your password, enter your email address and send yourself a new password using the link below." );
			return user.MustChangePassword ? ChangePassword.Page.GetInfo( info.ReturnUrl ).GetUrl() : info.ReturnUrl;
		}

		protected void handleSendNewPasswordClick() {
			EhValidateAndModifyDataAndRedirect(
				delegate( Validator validator ) { validatedEmailAddress = UserManagementStatics.ValidateAndGetEmailAddress( validator, emailAddress, "Please enter a valid email address." ); },
				delegate( DBConnection cn ) {
					var user = UserManagementStatics.GetUser( cn, validatedEmailAddress );
					if( user == null )
						throw new EwfException( getUnregisteredEmailMessage() );
					return ConfirmPasswordReset.GetInfo( info.ReturnUrl, user.UserId ).GetUrl();
				} );
		}

		private string getUnregisteredEmailMessage() {
			return "The email address you entered is not registered.  You must register the email address with " + provider.AdministratingCompanyName +
			       " before using it to log in.  To do this, " + provider.LogInHelpInstructions;
		}
	}
}