using System;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A telephone-number edit control.
	/// </summary>
	public class TelephoneNumberControl: FormControl<PhrasingComponent> {
		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a telephone-number control.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="allowEmpty"></param>
		/// <param name="setup">The setup object for the telephone-number control.</param>
		/// <param name="maxLength">The maximum number of characters a user can input.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public TelephoneNumberControl(
			string value, bool allowEmpty, TelephoneNumberControlSetup setup = null, int? maxLength = null, Action<string, Validator> validationMethod = null ) {
			setup = setup ?? TelephoneNumberControlSetup.Create();
			( Labeler, PageComponent, Validation ) = setup.TextControlSetup.LabelerAndComponentAndValidationGetter(
				value,
				allowEmpty,
				null,
				maxLength,
				( postBackValue, validator ) => {
					var errorHandler = new ValidationErrorHandler( "telephone number" );
					var validatedValue = validator.GetPhoneNumber( errorHandler, postBackValue, true, allowEmpty, false );
					return errorHandler.LastResult != ErrorCondition.NoError ? null : validatedValue;
				},
				validationMethod );
		}
	}
}