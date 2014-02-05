using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement.Public;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	public partial class LogIn: EwfPage {
		private DataValue<string> emailAddress;
		private FormsAuthCapableUser user;

		protected override void loadData() {
			var logInPb =
				PostBack.CreateFull(
					actionGetter:
						() => new PostBackAction( user.MustChangePassword ? ChangePassword.Page.GetInfo( info.ReturnUrl ) as PageInfo : new ExternalPageInfo( info.ReturnUrl ) ) );
			var newPasswordPb = PostBack.CreateFull( id: "newPw", actionGetter: getSendNewPasswordAction );

			var registeredTable = EwfTable.Create( caption: "Registered users" );
			registeredTable.AddItem(
				new EwfTableItem(
					new EwfTableCell( "You may log in to this system if you have registered your email address with " +
					                  FormsAuthStatics.SystemProvider.AdministratingCompanyName ) { FieldSpan = 2 } ) );

			emailAddress = new DataValue<string>();
			var emailVl = new BasicValidationList();
			registeredTable.AddItem( new EwfTableItem( "Email address".ToCell(),
			                                           emailAddress.GetEmailAddressFormItem( "", "Please enter a valid email address.", emailVl ).ToControl().ToCell() ) );
			logInPb.AddValidations( emailVl );
			newPasswordPb.AddValidations( emailVl );

			var password = new DataValue<string>();
			registeredTable.AddItem( new EwfTableItem( "Password".ToCell(),
			                                           FormItem.Create( "",
			                                                            new EwfTextBox( "", masksCharacters: true ),
			                                                            validationGetter:
				                                                            control =>
				                                                            new Validation( ( pbv, v ) => password.Value = control.GetPostBackValue( pbv ), logInPb ) )
			                                                   .ToControl()
			                                                   .ToCell() ) );

			registeredTable.AddItem(
				new EwfTableItem(
					new EwfTableCell(
						new PlaceHolder().AddControlsReturnThis(
							"If you are a first-time user and do not know your password, or if you have forgotten your password, ".GetLiteralControl(),
							new PostBackButton( newPasswordPb, new TextActionControlStyle( "click here to immediately send yourself a new password." ), usesSubmitBehavior: false ) ) )
						{
							FieldSpan = 2
						} ) );
			ph.AddControlsReturnThis( registeredTable );

			var specialInstructions = EwfUiStatics.AppProvider.GetSpecialInstructionsForLogInPage();
			if( specialInstructions != null )
				ph.AddControlsReturnThis( specialInstructions );
			else {
				var unregisteredTable = EwfTable.Create( caption: "Unregistered users" );
				unregisteredTable.AddItem(
					new EwfTableItem( ( "If you have difficulty logging in, please " + FormsAuthStatics.SystemProvider.LogInHelpInstructions ).ToCell() ) );
				ph.AddControlsReturnThis( unregisteredTable );
			}

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Log In", new PostBackButton( logInPb ) ) );

			var logInMethod = FormsAuthStatics.GetLogInMethod( emailAddress,
			                                                   password,
			                                                   getUnregisteredEmailMessage(),
			                                                   "Incorrect password. If you do not know your password, enter your email address and send yourself a new password using the link below.",
			                                                   logInPb );
			logInPb.AddModificationMethod( () => user = logInMethod() );
		}

		private PostBackAction getSendNewPasswordAction() {
			var userLocal = UserManagementStatics.GetUser( emailAddress.Value );
			if( userLocal == null )
				throw new DataModificationException( getUnregisteredEmailMessage() );
			return new PostBackAction( ConfirmPasswordReset.GetInfo( info.ReturnUrl, userLocal.UserId ) );
		}

		private string getUnregisteredEmailMessage() {
			return "The email address you entered is not registered.  You must register the email address with " +
			       FormsAuthStatics.SystemProvider.AdministratingCompanyName + " before using it to log in.  To do this, " +
			       FormsAuthStatics.SystemProvider.LogInHelpInstructions;
		}
	}
}