using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Humanizer;
using Tewl.Tools;

// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement {
	public partial class LogIn: EwfPage {
		private DataValue<string> emailAddress;
		private FormsAuthCapableUser user;

		protected override void loadData() {
			EwfUiStatics.OmitContentBox();

			Tuple<IReadOnlyCollection<EtherealComponent>, Func<FormsAuthCapableUser>> logInHiddenFieldsAndMethod = null;
			var logInPb = PostBack.CreateFull(
				firstModificationMethod: () => user = logInHiddenFieldsAndMethod.Item2(),
				actionGetter: () => new PostBackAction(
					user.MustChangePassword ? ChangePassword.Page.GetInfo( info.ReturnUrl ) as ResourceInfo : new ExternalResource( info.ReturnUrl ) ) );
			var newPasswordPb = PostBack.CreateFull( id: "newPw", actionGetter: getSendNewPasswordAction );

			FormState.ExecuteWithDataModificationsAndDefaultAction(
				logInPb.ToCollection(),
				() => {
					var registeredComponents = new List<FlowComponent>();
					registeredComponents.Add(
						new Paragraph(
							"You may log in to this system if you have registered your email address with {0}"
								.FormatWith( FormsAuthStatics.SystemProvider.AdministratingCompanyName )
								.ToComponents() ) );

					emailAddress = new DataValue<string>();
					var password = new DataValue<string>();
					registeredComponents.Add(
						FormItemList.CreateStack(
							generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Log In" ), enableSubmitButton: true ),
							items: FormState
								.ExecuteWithDataModificationsAndDefaultAction(
									new[] { logInPb, newPasswordPb },
									() => emailAddress.GetEmailAddressFormItem( "Email address".ToComponents() ) )
								.Append(
									password.ToTextControl( true, setup: TextControlSetup.CreateObscured( autoFillTokens: "current-password" ), value: "" )
										.ToFormItem( label: "Password".ToComponents() ) )
								.Materialize() ) );

					if( FormsAuthStatics.PasswordResetEnabled )
						registeredComponents.Add(
							new Paragraph(
								new ImportantContent( "Forgot password?".ToComponents() ).ToCollection()
									.Concat( " ".ToComponents() )
									.Append(
										new EwfButton(
											new StandardButtonStyle( "Send me a new password", buttonSize: ButtonSize.ShrinkWrap ),
											behavior: new PostBackBehavior( postBack: newPasswordPb ) ) )
									.Materialize() ) );

					ph.AddControlsReturnThis(
						new FlowAutofocusRegion(
								AutofocusCondition.InitialRequest(),
								new Section( "Registered users", registeredComponents, style: SectionStyle.Box ).ToCollection() ).ToCollection()
							.GetControls() );

					logInHiddenFieldsAndMethod = FormsAuthStatics.GetLogInHiddenFieldsAndMethod(
						emailAddress,
						password,
						getUnregisteredEmailMessage(),
						"Incorrect password. If you do not know your password, enter your email address and send yourself a new password using the link below." );
					logInHiddenFieldsAndMethod.Item1.AddEtherealControls( this );
				} );

			var specialInstructions = EwfUiStatics.AppProvider.GetSpecialInstructionsForLogInPage();
			if( specialInstructions != null )
				ph.AddControlsReturnThis( specialInstructions );
			else {
				var unregisteredComponents = new List<FlowComponent>();
				unregisteredComponents.Add(
					new Paragraph( "If you have difficulty logging in, please {0}".FormatWith( FormsAuthStatics.SystemProvider.LogInHelpInstructions ).ToComponents() ) );
				ph.AddControlsReturnThis( new Section( "Unregistered users", unregisteredComponents, style: SectionStyle.Box ).ToCollection().GetControls() );
			}
		}

		private PostBackAction getSendNewPasswordAction() {
			var userLocal = UserManagementStatics.GetUser( emailAddress.Value );
			if( userLocal == null )
				throw new DataModificationException( getUnregisteredEmailMessage() );
			return new PostBackAction( ConfirmPasswordReset.GetInfo( userLocal.Email, info.ReturnUrl ) );
		}

		private string getUnregisteredEmailMessage() {
			return "The email address you entered is not registered.  You must register the email address with " +
			       FormsAuthStatics.SystemProvider.AdministratingCompanyName + " before using it to log in.  To do this, " +
			       FormsAuthStatics.SystemProvider.LogInHelpInstructions;
		}
	}
}