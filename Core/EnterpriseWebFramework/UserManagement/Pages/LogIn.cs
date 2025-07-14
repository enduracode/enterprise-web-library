using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure;
using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification;
using EnterpriseWebLibrary.UserManagement;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement.Pages;

// EwlPage
// Parameter: string returnUrl
// OptionalParameter: string user
// OptionalParameter: string code
partial class LogIn {
	// This class name is used by EWF CSS files.
	private static readonly ElementClass passwordContainerClass = new( "ewfLogInPasswordContainer" );

	private static readonly ElementClass passwordClass = new( "ewfLogInPassword" );
	private static readonly ElementClass loginCodeButtonClass = new( "ewfLogInLcB" );

	protected override void init() {
		if( new Validator().GetEmailAddress( new ValidationErrorHandler( "value" ), User, true ).Error( out var validatedUser ) is not null ||
		    !string.Equals( validatedUser, User, StringComparison.Ordinal ) )
			throw new Exception( "user" );
	}

	protected override string getResourceName() => authenticatedUserDeniedAccess ? "Access Denied" : base.getResourceName();

	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		parametersModification.User = "";
		parametersModification.Code = "";

		var customContent = AuthenticationStatics.AppProvider.GetLogInPageContent( ReturnUrl, User, Code, authenticatedUserDeniedAccess );
		if( customContent != null )
			return customContent;

		if( User.Length > 0 && Code.Length > 0 ) {
			AuthenticationStatics.CodeLoginModificationMethod? codeLoginMethod = null;
			string? destinationUrl = null;
			var postBack = PostBack.CreateFull(
				modificationMethod: () => destinationUrl = codeLoginMethod!(
						                          User,
						                          Code,
						                          errorMessage:
						                          "The login link you just used has expired. Please return to the page you were on and send yourself another login email." )
					                          .destinationUrl,
				actionGetter: () => new PostBackAction( new ExternalResource( destinationUrl ) ) );
			return FormState.ExecuteWithActions(
				postBack,
				() => {
					var logInHiddenFieldsAndMethods = AuthenticationStatics.GetLogInHiddenFieldsAndMethods();
					codeLoginMethod = logInHiddenFieldsAndMethods.modificationMethods.codeLoginMethod;
					return new UiPageContent( pageLoadPostBack: postBack ).Add( logInHiddenFieldsAndMethods.hiddenFields );
				} );
		}

		return new UiPageContent( omitContentBox: true ).Add( authenticatedUserDeniedAccess ? getAuthenticatedUserDeniedAccessComponents() : getLogInComponents() );
	}

	private bool authenticatedUserDeniedAccess => SystemUser.Current is not null && !string.Equals( GetUrl(), EwfRequest.Current!.Url, StringComparison.Ordinal );

	private IReadOnlyCollection<FlowComponent> getAuthenticatedUserDeniedAccessComponents() =>
		new Section(
			new Paragraph(
					"You’re already logged in, but do not have access to this page. It’s possible that you had access in the past and that it was revoked."
						.ToComponents() )
				.Append(
					new Paragraph(
						new EwfHyperlink( AuthenticationStatics.GetDefaultLogInPage( ReturnUrl ), new ButtonHyperlinkStyle( "Log In as Another User" ) ).ToCollection() ) )
				.Materialize(),
			style: SectionStyle.Box ).ToCollection();

	private IReadOnlyCollection<FlowComponent> getLogInComponents() {
		var components = new List<FlowComponent>();
		var autoRegistrationSetup = AuthenticationStatics.AppProvider.GetLogInPageAutoUserRegistrationSetup();

		var codeEntryIsForPasswordReset = ComponentStateItem.Create<bool?>( "codeEntryIsForPasswordReset", User.Length > 0 ? false : null, _ => true, false );

		var emailAddress = new DataValue<string>( User.Length > 0, existingValueGetter: () => User );
		var password = new DataValue<string>( false );
		var loginCode = new DataValue<string>( false );
		AuthenticationStatics.PasswordLoginModificationMethod? passwordLoginMethod = null;
		AuthenticationStatics.LoginCodeSenderMethod? loginCodeSender = null;
		AuthenticationStatics.CodeLoginModificationMethod? codeLoginMethod = null;

		string? destinationUrl = null;
		var logInPb = PostBack.CreateFull(
			modificationMethod: () => {
				if( codeEntryIsForPasswordReset.Value.HasValue )
					destinationUrl = codeLoginMethod!( emailAddress.Value, loginCode.Value ).destinationUrl;
				else
					passwordLoginMethod!( emailAddress.Value, password );
			},
			actionGetter: () => new PostBackAction( new ExternalResource( codeEntryIsForPasswordReset.Value.HasValue ? destinationUrl : ReturnUrl ) ) );

		var authenticationModeUpdateRegion = new UpdateRegionSet();
		const string passwordOrCodeFocusKey = "code";
		var sendCodePb = codeEntryIsForPasswordReset.Value != true
			                 ? PostBack.CreateIntermediate(
				                 authenticationModeUpdateRegion,
				                 id: "sendCode",
				                 modificationMethod: () => {
					                 loginCodeSender!(
						                 emailAddress.Value,
						                 false,
						                 ReturnUrl,
						                 newUserRoleId: autoRegistrationSetup?.GetRoleIdForEmailAddress( emailAddress.Value ) );
					                 codeEntryIsForPasswordReset.Value = false;
				                 },
				                 reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: passwordOrCodeFocusKey ) )
			                 : null;
		var newPasswordPb = codeEntryIsForPasswordReset.Value != false
			                    ? PostBack.CreateIntermediate(
				                    authenticationModeUpdateRegion,
				                    id: "newPw",
				                    modificationMethod: () => {
					                    loginCodeSender!(
						                    emailAddress.Value,
						                    true,
						                    ReturnUrl,
						                    newUserRoleId: autoRegistrationSetup?.GetRoleIdForEmailAddress( emailAddress.Value ) );
					                    codeEntryIsForPasswordReset.Value = true;
				                    },
				                    reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: passwordOrCodeFocusKey ) )
			                    : null;

		FormState.ExecuteWithActions(
			logInPb,
			() => {
				var registeredComponents = new List<FlowComponent>();

				if( autoRegistrationSetup is null )
					registeredComponents.Add(
						new Paragraph(
							"You may log in to this system if you have registered your email address with {0}."
								.FormatWith( UserManagementStatics.LocalIdentityProvider.AdministratingOrganizationName )
								.ToComponents() ) );

				registeredComponents.Add(
					FormItemList.CreateStack( generalSetup: new FormItemListSetup( buttonSetup: new ButtonSetup( "Log In" ), enableSubmitButton: true ) )
						.AddItems(
							FormState
								.ExecuteWithActions(
									logInPb.Add( sendCodePb ).Add( newPasswordPb ),
									() => emailAddress.GetEmailAddressFormItem( "Email address".ToComponents() ) )
								.Append(
									codeEntryIsForPasswordReset.Value.HasValue
										? getLoginCodeFormItem(
											authenticationModeUpdateRegion,
											User.Length > 0 ? AutofocusCondition.InitialRequest() : AutofocusCondition.PostBack( passwordOrCodeFocusKey ),
											loginCode )
										: getPasswordFormItem(
											authenticationModeUpdateRegion,
											AutofocusCondition.PostBack( passwordOrCodeFocusKey ),
											password,
											new PostBackBehavior( postBack: sendCodePb ) ) )
								.Materialize() ) );

				registeredComponents.Add(
					new Paragraph(
						new PhrasingIdContainer(
							codeEntryIsForPasswordReset.Value.HasValue
								? new ImportantContent( "Having trouble?".ToComponents() ).ToCollection()
									.Concat( " ".ToComponents() )
									.Append(
										new EwfButton(
											new StandardButtonStyle( "Send me another code", buttonSize: ButtonSize.ShrinkWrap ),
											behavior: new PostBackBehavior( postBack: codeEntryIsForPasswordReset.Value.Value ? newPasswordPb : sendCodePb ) ) )
									.Concat( " ".ToComponents() )
									.Append(
										new EwfButton(
											new StandardButtonStyle(
												codeEntryIsForPasswordReset.Value.Value ? "Try password again" : "Log in with password",
												buttonSize: ButtonSize.ShrinkWrap ),
											behavior: new PostBackBehavior(
												postBack: PostBack.CreateIntermediate(
													authenticationModeUpdateRegion,
													id: "revertToPasswordEntry",
													modificationMethod: () => codeEntryIsForPasswordReset.Value = null,
													reloadBehaviorGetter: () => new PageReloadBehavior( focusKey: passwordOrCodeFocusKey ) ) ) ) )
								: new ImportantContent( "Forgot password?".ToComponents() ).Concat( " ".ToComponents() )
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
							updateRegionSets: authenticationModeUpdateRegion ).ToCollection() ) );

				var logInHiddenFieldsAndMethods = FormState.ExecuteWithActions(
					logInPb.Add( sendCodePb ).Add( newPasswordPb ),
					AuthenticationStatics.GetLogInHiddenFieldsAndMethods );

				components.Add(
					new FlowAutofocusRegion(
						User.Length > 0 ? null : AutofocusCondition.InitialRequest(),
						new Section(
							autoRegistrationSetup is null ? "Registered users" : "",
							registeredComponents,
							style: SectionStyle.Box,
							etherealContent: logInHiddenFieldsAndMethods.hiddenFields.Append( codeEntryIsForPasswordReset ).Materialize() ).ToCollection() ) );

				passwordLoginMethod = logInHiddenFieldsAndMethods.modificationMethods.passwordLoginMethod;
				loginCodeSender = logInHiddenFieldsAndMethods.modificationMethods.loginCodeSender;
				codeLoginMethod = logInHiddenFieldsAndMethods.modificationMethods.codeLoginMethod;
			} );

		var specialInstructions = AuthenticationStatics.AppProvider.GetLogInPageSpecialInstructions();
		if( specialInstructions.Any() )
			components.AddRange( specialInstructions );
		else {
			var instructionPhrase = UserManagementStatics.LocalIdentityProvider.LogInHelpInstructions;
			components.Add(
				new Section(
					autoRegistrationSetup is null ? "Unregistered users" : "Not receiving login codes?",
					new Paragraph(
						autoRegistrationSetup is null || !autoRegistrationSetup.AllowedEmailAddressDomains.Any()
							? $"If you have difficulty logging in, please {instructionPhrase}".ToComponents()
							: "If you are not receiving login codes, you may not be registered.".ToComponents()
								.Concat( $" We only automatically register email addresses that end in {autoRegistrationSetup.AllowedDomainsListPhrase}.".ToComponents() )
								.Concat( $" For help please {instructionPhrase}".ToComponents() )
								.Materialize() ).ToCollection(),
					style: SectionStyle.Box ) );
		}

		return components;
	}

	private FormItem getPasswordFormItem(
		UpdateRegionSetsParameter updateRegionSets, AutofocusCondition autofocusCondition, DataValue<string> password, ButtonBehavior sendCodeButtonBehavior ) {
		var control = password.ToTextControl( false, setup: TextControlSetup.CreateObscured( classes: passwordClass, autoFillTokens: "current-password" ) );
		return new FlowAutofocusRegion(
			autofocusCondition,
			new GenericFlowContainer(
				control.PageComponent.Append<FlowComponent>(
						new GenericFlowContainer(
							new GenericPhrasingContainer( "or".ToComponents() ).Append<PhrasingComponent>(
									new EwfButton( new StandardButtonStyle( "Email Login Code" ), behavior: sendCodeButtonBehavior, classes: loginCodeButtonClass ) )
								.Materialize() ) )
					.Materialize(),
				classes: passwordContainerClass ).ToCollection() ).ToFormItem(
			setup: new FormItemSetup( updateRegionSets: updateRegionSets ),
			label: control.Labeler.CreateLabel( "Password".ToComponents() ),
			validation: control.Validation );
	}

	private FormItem getLoginCodeFormItem( UpdateRegionSetsParameter updateRegionSets, AutofocusCondition autofocusCondition, DataValue<string> loginCode ) {
		var control = loginCode.ToNumericTextControl( false, maxLength: 10 );
		return new FlowAutofocusRegion( autofocusCondition, control.PageComponent.ToCollection() ).ToFormItem(
			setup: new FormItemSetup( updateRegionSets: updateRegionSets ),
			label: control.Labeler.CreateLabel( "Login code".ToComponents() ),
			validation: control.Validation );
	}

	protected override string javaScriptPageInitFunctionCall =>
		"initLogInPage( '.{0}', '.{1}' )".FormatWith( passwordClass.ClassName, loginCodeButtonClass.ClassName );
}