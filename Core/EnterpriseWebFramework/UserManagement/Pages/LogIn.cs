using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

// EwlPage
// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages {
	partial class LogIn {
		private DataValue<string> emailAddress;
		private FormsAuthCapableUser user;

		protected override PageContent getContent() {
			var content = new UiPageContent( omitContentBox: true );

			Tuple<IReadOnlyCollection<EtherealComponent>, Func<FormsAuthCapableUser>> logInHiddenFieldsAndMethod = null;
			var logInPb = PostBack.CreateFull(
				modificationMethod: () => user = logInHiddenFieldsAndMethod.Item2(),
				actionGetter: () => new PostBackAction(
					user.MustChangePassword ? ChangePassword.GetInfo( ReturnUrl ) as ResourceInfo : new ExternalResource( ReturnUrl ) ) );
			var newPasswordPb = PostBack.CreateFull(
				id: "newPw",
				modificationMethod: () => {
					var userLocal = UserManagementStatics.GetUser( emailAddress.Value );
					if( userLocal == null )
						throw new DataModificationException( getUnregisteredEmailMessage() );
					FormsAuthStatics.ResetAndSendPassword( userLocal.UserId );
					AddStatusMessage( StatusMessageType.Info, "Your new password has been sent to your email address." );
				},
				actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) );

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
											behavior: new ConfirmationButtonBehavior(
												new Paragraph( "Are you sure you want to reset your password?".ToComponents() ).Append(
														new Paragraph(
															StringTools.ConcatenateWithDelimiter(
																	" ",
																	"Click \"Continue\" to email yourself a new password.",
																	"Upon receiving your new password, you may immediately use it to log in.",
																	"You will then be prompted to change your password to something you will remember, which you may use to log in from that point forward." )
																.ToComponents() ) )
													.Materialize(),
												postBack: newPasswordPb ) ) )
									.Materialize() ) );

					content.Add(
						new FlowAutofocusRegion(
							AutofocusCondition.InitialRequest(),
							new Section( "Registered users", registeredComponents, style: SectionStyle.Box ).ToCollection() ) );

					logInHiddenFieldsAndMethod = FormsAuthStatics.GetLogInHiddenFieldsAndMethod(
						emailAddress,
						password,
						getUnregisteredEmailMessage(),
						"Incorrect password. If you do not know your password, enter your email address and send yourself a new password using the link below." );
					content.Add( logInHiddenFieldsAndMethod.Item1 );
				} );

			var specialInstructions = EwfUiStatics.AppProvider.GetSpecialInstructionsForLogInPage();
			if( specialInstructions.Any() )
				content.Add( specialInstructions );
			else {
				var unregisteredComponents = new List<FlowComponent>();
				unregisteredComponents.Add(
					new Paragraph( "If you have difficulty logging in, please {0}".FormatWith( FormsAuthStatics.SystemProvider.LogInHelpInstructions ).ToComponents() ) );
				content.Add( new Section( "Unregistered users", unregisteredComponents, style: SectionStyle.Box ) );
			}

			return content;
		}

		private string getUnregisteredEmailMessage() {
			return "The email address you entered is not registered.  You must register the email address with " +
			       FormsAuthStatics.SystemProvider.AdministratingCompanyName + " before using it to log in.  To do this, " +
			       FormsAuthStatics.SystemProvider.LogInHelpInstructions;
		}
	}
}