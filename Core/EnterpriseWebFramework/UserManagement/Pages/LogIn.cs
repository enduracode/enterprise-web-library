using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

// EwlPage
// Parameter: string returnUrl
// OptionalParameter: string user
// OptionalParameter: string code

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages {
	partial class LogIn {
		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override PageContent getContent() {
			var content = new UiPageContent( omitContentBox: true );

			DataValue<string> emailAddress = null;
			Tuple<IReadOnlyCollection<EtherealComponent>, Func<User>> logInHiddenFieldsAndMethod = null;
			var logInPb = PostBack.CreateFull(
				modificationMethod: () => logInHiddenFieldsAndMethod.Item2(),
				actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) );
			var newPasswordPb = PostBack.CreateFull(
				id: "newPw",
				modificationMethod: () => {
					AuthenticationStatics.SendLoginCode( emailAddress, true, ReturnUrl );
					AddStatusMessage( StatusMessageType.Info, "Your login code has been sent to your email address." );
				},
				actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) );

			FormState.ExecuteWithDataModificationsAndDefaultAction(
				logInPb.ToCollection(),
				() => {
					var registeredComponents = new List<FlowComponent>();
					registeredComponents.Add(
						new Paragraph(
							"You may log in to this system if you have registered your email address with {0}."
								.FormatWith( UserManagementStatics.LocalIdentityProvider.AdministratingOrganizationName )
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

					registeredComponents.Add(
						new Paragraph(
							new ImportantContent( "Forgot password?".ToComponents() ).ToCollection()
								.Concat( " ".ToComponents() )
								.Append(
									new EwfButton(
										new StandardButtonStyle( "Email me a login code", buttonSize: ButtonSize.ShrinkWrap ),
										behavior: new ConfirmationButtonBehavior(
											new Paragraph( "Are you sure you want to reset your password?".ToComponents() ).Append(
													new Paragraph(
														StringTools.ConcatenateWithDelimiter(
																" ",
																"Click \"Continue\" to email yourself a login code.",
																"After logging in, you will be prompted to change your password to something you will remember, which you may use to log in from that point forward." )
															.ToComponents() ) )
												.Materialize(),
											postBack: newPasswordPb ) ) )
								.Materialize() ) );

					content.Add(
						new FlowAutofocusRegion(
							AutofocusCondition.InitialRequest(),
							new Section( "Registered users", registeredComponents, style: SectionStyle.Box ).ToCollection() ) );

					logInHiddenFieldsAndMethod = AuthenticationStatics.GetLogInHiddenFieldsAndMethod( emailAddress, password );
					content.Add( logInHiddenFieldsAndMethod.Item1 );
				} );

			var specialInstructions = EwfUiStatics.AppProvider.GetSpecialInstructionsForLogInPage();
			if( specialInstructions.Any() )
				content.Add( specialInstructions );
			else {
				var unregisteredComponents = new List<FlowComponent>();
				unregisteredComponents.Add(
					new Paragraph(
						"If you have difficulty logging in, please {0}".FormatWith( UserManagementStatics.LocalIdentityProvider.LogInHelpInstructions ).ToComponents() ) );
				content.Add( new Section( "Unregistered users", unregisteredComponents, style: SectionStyle.Box ) );
			}

			return content;
		}
	}
}