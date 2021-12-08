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
		// This class name is used by EWF CSS files.
		private static readonly ElementClass passwordContainerClass = new ElementClass( "ewfLogInPasswordContainer" );

		private static readonly ElementClass passwordClass = new ElementClass( "ewfLogInPassword" );
		private static readonly ElementClass loginCodeClass = new ElementClass( "ewfLogInLoginCode" );

		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

		protected override PageContent getContent() {
			AuthenticationStatics.CodeLoginModificationMethod codeLoginMethod = null;
			string destinationUrl = null;
			if( User.Any() ) {
				var postBack = PostBack.CreateFull(
					modificationMethod: () => destinationUrl = codeLoginMethod(
							                          User,
							                          Code,
							                          errorMessage:
							                          "The login link you just used has expired. Please return to the page you were trying to access and send yourself another login email." )
						                          .destinationUrl,
					actionGetter: () => new PostBackAction( new ExternalResource( destinationUrl ) ) );
				return FormState.ExecuteWithDataModificationsAndDefaultAction(
					postBack.ToCollection(),
					() => {
						var logInHiddenFieldsAndMethods = AuthenticationStatics.GetLogInHiddenFieldsAndMethods();
						codeLoginMethod = logInHiddenFieldsAndMethods.modificationMethods.codeLoginMethod;
						return new UiPageContent( pageLoadPostBack: postBack ).Add( new Paragraph( "Please wait.".ToComponents() ) )
							.Add( logInHiddenFieldsAndMethods.hiddenFields );
					} );
			}

			var content = new UiPageContent( omitContentBox: true );

			var codeEntryIsForPasswordReset = ComponentStateItem.Create<bool?>( "codeEntryIsForPasswordReset", null, value => true, false );

			var emailAddress = new DataValue<string>();
			var password = new DataValue<string>();
			var loginCode = new DataValue<string>();
			AuthenticationStatics.PasswordLoginModificationMethod passwordLoginMethod = null;
			AuthenticationStatics.LoginCodeSenderMethod loginCodeSender = null;

			var logInPb = PostBack.CreateFull(
				modificationMethod: () => {
					if( codeEntryIsForPasswordReset.Value.Value.HasValue )
						destinationUrl = codeLoginMethod( emailAddress.Value, loginCode.Value ).destinationUrl;
					else
						passwordLoginMethod( emailAddress, password );
				},
				actionGetter: () => new PostBackAction( new ExternalResource( codeEntryIsForPasswordReset.Value.Value.HasValue ? destinationUrl : ReturnUrl ) ) );

			var authenticationModeUpdateRegion = new UpdateRegionSet();
			const string passwordOrCodeFocusKey = "code";
			var sendCodePb = codeEntryIsForPasswordReset.Value.Value != true
				                 ? PostBack.CreateIntermediate(
					                 authenticationModeUpdateRegion.ToCollection(),
					                 id: "sendCode",
					                 modificationMethod: () => {
						                 loginCodeSender( emailAddress, false, ReturnUrl );
						                 AddStatusMessage( StatusMessageType.Info, "Your login code has been sent to your email address." );
						                 codeEntryIsForPasswordReset.Value.Value = false;
					                 },
					                 reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: passwordOrCodeFocusKey ) )
				                 : null;
			var newPasswordPb = codeEntryIsForPasswordReset.Value.Value != false
				                    ? PostBack.CreateIntermediate(
					                    authenticationModeUpdateRegion.ToCollection(),
					                    id: "newPw",
					                    modificationMethod: () => {
						                    loginCodeSender( emailAddress, true, ReturnUrl );
						                    AddStatusMessage( StatusMessageType.Info, "Your login code has been sent to your email address." );
						                    codeEntryIsForPasswordReset.Value.Value = true;
					                    },
					                    reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: passwordOrCodeFocusKey ) )
				                    : null;

			FormState.ExecuteWithDataModificationsAndDefaultAction(
				logInPb.ToCollection(),
				() => {
					var registeredComponents = new List<FlowComponent>();
					registeredComponents.Add(
						new Paragraph(
							"You may log in to this system if you have registered your email address with {0}."
								.FormatWith( UserManagementStatics.LocalIdentityProvider.AdministratingOrganizationName )
								.ToComponents() ) );

					registeredComponents.Add(
						FormItemList.CreateStack(
							generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Log In" ), enableSubmitButton: true ),
							items: FormState
								.ExecuteWithDataModificationsAndDefaultAction(
									new[] { logInPb, sendCodePb, newPasswordPb }.Where( i => i != null ),
									() => emailAddress.GetEmailAddressFormItem( "Email address".ToComponents() ) )
								.Append(
									codeEntryIsForPasswordReset.Value.Value.HasValue
										? getLoginCodeFormItem( authenticationModeUpdateRegion.ToCollection(), AutofocusCondition.PostBack( passwordOrCodeFocusKey ), loginCode )
										: getPasswordFormItem(
											authenticationModeUpdateRegion.ToCollection(),
											AutofocusCondition.PostBack( passwordOrCodeFocusKey ),
											password,
											new PostBackBehavior( postBack: sendCodePb ) ) )
								.Materialize() ) );

					registeredComponents.Add(
						new Paragraph(
							new PhrasingIdContainer(
								codeEntryIsForPasswordReset.Value.Value.HasValue
									? new ImportantContent( "Having trouble?".ToComponents() ).ToCollection()
										.Concat( " ".ToComponents() )
										.Append(
											new EwfButton(
												new StandardButtonStyle( "Send me another code", buttonSize: ButtonSize.ShrinkWrap ),
												behavior: new PostBackBehavior( postBack: codeEntryIsForPasswordReset.Value.Value.Value ? newPasswordPb : sendCodePb ) ) )
										.Concat( " ".ToComponents() )
										.Append(
											new EwfButton(
												new StandardButtonStyle(
													codeEntryIsForPasswordReset.Value.Value.Value ? "Try password again" : "Log in with password",
													buttonSize: ButtonSize.ShrinkWrap ),
												behavior: new PostBackBehavior(
													postBack: PostBack.CreateIntermediate(
														authenticationModeUpdateRegion.ToCollection(),
														id: "revertToPasswordEntry",
														modificationMethod: () => codeEntryIsForPasswordReset.Value.Value = null,
														reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: passwordOrCodeFocusKey ) ) ) ) )
									: new ImportantContent( "Forgot password?".ToComponents() ).ToCollection()
										.Concat( " ".ToComponents() )
										.Append(
											new EwfButton(
												new StandardButtonStyle( "Set a new password", buttonSize: ButtonSize.ShrinkWrap ),
												behavior: new ConfirmationButtonBehavior(
													new Paragraph( "Are you sure you want to set a new password?".ToComponents() ).Append(
															new Paragraph(
																StringTools.ConcatenateWithDelimiter(
																		" ",
																		"Click \"Continue\" to email yourself a login code.",
																		"After logging in, you will be prompted to change your password to something you will remember, which you may use to log in from that point forward." )
																	.ToComponents() ) )
														.Materialize(),
													postBack: newPasswordPb ) ) ),
								updateRegionSets: authenticationModeUpdateRegion.ToCollection() ).ToCollection() ) );

					var logInHiddenFieldsAndMethods = FormState.ExecuteWithDataModificationsAndDefaultAction(
						new[] { logInPb, sendCodePb, newPasswordPb }.Where( i => i != null ),
						AuthenticationStatics.GetLogInHiddenFieldsAndMethods );

					content.Add(
						new FlowAutofocusRegion(
							AutofocusCondition.InitialRequest(),
							new Section(
								"Registered users",
								registeredComponents,
								style: SectionStyle.Box,
								etherealContent: logInHiddenFieldsAndMethods.hiddenFields.Append( codeEntryIsForPasswordReset ).Materialize() ).ToCollection() ) );

					passwordLoginMethod = logInHiddenFieldsAndMethods.modificationMethods.passwordLoginMethod;
					loginCodeSender = logInHiddenFieldsAndMethods.modificationMethods.loginCodeSender;
					codeLoginMethod = logInHiddenFieldsAndMethods.modificationMethods.codeLoginMethod;
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

		private FormItem getPasswordFormItem(
			IEnumerable<UpdateRegionSet> updateRegionSets, AutofocusCondition autofocusCondition, DataValue<string> password,
			ButtonBehavior sendCodeButtonBehavior ) {
			var control = password.ToTextControl(
				false,
				setup: TextControlSetup.CreateObscured( classes: passwordClass, autoFillTokens: "current-password" ),
				value: "" );
			return new FlowAutofocusRegion(
				autofocusCondition,
				new GenericFlowContainer(
					control.PageComponent.Append<FlowComponent>(
							new GenericFlowContainer(
								new GenericPhrasingContainer( "or".ToComponents() ).Append<PhrasingComponent>(
										new EwfButton( new StandardButtonStyle( "Email Login Code" ), behavior: sendCodeButtonBehavior, classes: loginCodeClass ) )
									.Materialize() ) )
						.Materialize(),
					classes: passwordContainerClass ).ToCollection() ).ToFormItem(
				setup: new FormItemSetup( updateRegionSets: updateRegionSets ),
				label: control.Labeler.CreateLabel( "Password".ToComponents() ),
				validation: control.Validation );
		}

		private FormItem getLoginCodeFormItem( IEnumerable<UpdateRegionSet> updateRegionSets, AutofocusCondition autofocusCondition, DataValue<string> loginCode ) {
			var control = loginCode.ToNumericTextControl( false, value: "", minLength: 6, maxLength: 6 );
			return new FlowAutofocusRegion( autofocusCondition, control.PageComponent.ToCollection() ).ToFormItem(
				setup: new FormItemSetup( updateRegionSets: updateRegionSets ),
				label: control.Labeler.CreateLabel( "Login code".ToComponents() ),
				validation: control.Validation );
		}

		protected override string javaScriptDocumentReadyFunctionCall =>
			"initLogInPage( '.{0}', '.{1}' )".FormatWith( passwordClass.ClassName, loginCodeClass.ClassName );
	}
}