using System;
using Humanizer;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A form control that navigates to a resource using the entered value as a parameter.
	/// </summary>
	public class NavFormControl {
		/// <summary>
		/// Creates a text control.
		/// </summary>
		/// <param name="setup">The setup object for the control. Do not pass null.</param>
		/// <param name="validationMethod">A function that validates the value entered by the user and returns a result. Do not pass null.</param>
		public static NavFormControl CreateText( NavFormControlSetup setup, Func<string, NavFormControlValidationResult> validationMethod ) =>
			new NavFormControl(
				setup,
				validationResultHandler => {
					return new TextControl(
						"",
						false,
						setup: setup.AutoCompleteResource != null
							       ? TextControlSetup.CreateAutoComplete( setup.AutoCompleteResource, placeholder: setup.Placeholder, triggersActionWhenItemSelected: true )
							       : TextControlSetup.Create( placeholder: setup.Placeholder ),
						validationMethod: ( postBackValue, validator ) => validationResultHandler( validationMethod( postBackValue ), validator ) );
				} );

		/// <summary>
		/// Creates an email-address control.
		/// </summary>
		/// <param name="setup">The setup object for the control. Do not pass null.</param>
		/// <param name="validationMethod">A function that validates the value entered by the user and returns a result. Do not pass null.</param>
		public static NavFormControl CreateEmail( NavFormControlSetup setup, Func<string, NavFormControlValidationResult> validationMethod ) =>
			new NavFormControl(
				setup,
				validationResultHandler => {
					return new EmailAddressControl(
						"",
						false,
						setup: setup.AutoCompleteResource != null
							       ? EmailAddressControlSetup.CreateAutoComplete(
								       setup.AutoCompleteResource,
								       placeholder: setup.Placeholder,
								       triggersActionWhenItemSelected: true )
							       : EmailAddressControlSetup.Create( placeholder: setup.Placeholder ),
						validationMethod: ( postBackValue, validator ) => validationResultHandler( validationMethod( postBackValue ), validator ) );
				} );

		/// <summary>
		/// Creates a numeric-text control with the value expressed as a string.
		/// </summary>
		/// <param name="setup">The setup object for the control. Do not pass null.</param>
		/// <param name="validationMethod">A function that validates the value entered by the user and returns a result. Do not pass null.</param>
		public static NavFormControl CreateNumericTextAsString( NavFormControlSetup setup, Func<string, NavFormControlValidationResult> validationMethod ) =>
			new NavFormControl(
				setup,
				validationResultHandler => {
					return new NumericTextControl(
						"",
						false,
						setup: setup.AutoCompleteResource != null
							       ? NumericTextControlSetup.CreateAutoComplete(
								       setup.AutoCompleteResource,
								       placeholder: setup.Placeholder,
								       triggersActionWhenItemSelected: true )
							       : NumericTextControlSetup.Create( placeholder: setup.Placeholder ),
						validationMethod: ( postBackValue, validator ) => validationResultHandler( validationMethod( postBackValue ), validator ) );
				} );

		/// <summary>
		/// Creates a numeric-text control with the value expressed as an int.
		/// </summary>
		/// <param name="setup">The setup object for the control. Do not pass null.</param>
		/// <param name="validationMethod">A function that validates the value entered by the user and returns a result. Do not pass null.</param>
		public static NavFormControl CreateNumericTextAsInt( NavFormControlSetup setup, Func<int, NavFormControlValidationResult> validationMethod ) =>
			new NavFormControl(
				setup,
				validationResultHandler => {
					var val = new DataValue<int>();
					return val.ToTextControl(
						setup: setup.AutoCompleteResource != null
							       ? NumericTextControlSetup.CreateAutoComplete(
								       setup.AutoCompleteResource,
								       placeholder: setup.Placeholder,
								       triggersActionWhenItemSelected: true )
							       : NumericTextControlSetup.Create( placeholder: setup.Placeholder ),
						value: new SpecifiedValue<int?>( null ),
						additionalValidationMethod: validator => validationResultHandler( validationMethod( val.Value ), validator ) );
				} );

		/// <summary>
		/// Creates a numeric-text control with the value expressed as a long.
		/// </summary>
		/// <param name="setup">The setup object for the control. Do not pass null.</param>
		/// <param name="validationMethod">A function that validates the value entered by the user and returns a result. Do not pass null.</param>
		public static NavFormControl CreateNumericTextAsLong( NavFormControlSetup setup, Func<long, NavFormControlValidationResult> validationMethod ) =>
			new NavFormControl(
				setup,
				validationResultHandler => {
					var val = new DataValue<long>();
					return val.ToTextControl(
						setup: setup.AutoCompleteResource != null
							       ? NumericTextControlSetup.CreateAutoComplete(
								       setup.AutoCompleteResource,
								       placeholder: setup.Placeholder,
								       triggersActionWhenItemSelected: true )
							       : NumericTextControlSetup.Create( placeholder: setup.Placeholder ),
						value: new SpecifiedValue<long?>( null ),
						additionalValidationMethod: validator => validationResultHandler( validationMethod( val.Value ), validator ) );
				} );

		private readonly Func<string, FormItem> formItemGetter;

		private NavFormControl(
			NavFormControlSetup setup, Func<Action<NavFormControlValidationResult, Validator>, FormControl<PhrasingComponent>> formControlGetter ) {
			formItemGetter = postBackId => {
				var destination = new DataValue<ResourceInfo>();
				var postBack = PostBack.CreateFull( id: postBackId, actionGetter: () => new PostBackAction( destination.Value ) );
				return FormState.ExecuteWithDataModificationsAndDefaultAction(
					postBack.ToCollection(),
					() => {
						var formControl = formControlGetter(
							( result, validator ) => {
								if( result.Destination != null )
									destination.Value = result.Destination;
								else
									validator.NoteErrorAndAddMessage( result.ErrorMessage );
							} );
						return new DisplayableElement(
							context => new DisplayableElementData(
								null,
								() => new DisplayableElementLocalData(
									"span",
									focusDependentData: new DisplayableElementFocusDependentData(
										attributes: new ElementAttribute(
											"style",
											"display: inline-block; width: {0}".FormatWith( ( (CssLength)setup.Width ).Value ) ).ToCollection() ) ),
								children: formControl.PageComponent.ToCollection() ) ).ToFormItem( validation: formControl.Validation );
					} );
			};
		}

		public FormItem GetFormItem( string postBackId ) => formItemGetter( postBackId );
	}
}