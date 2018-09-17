using System;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An email-address edit control.
	/// </summary>
	public class EmailAddressControl: FormControl<PhrasingComponent> {
		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates an email-address control.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="allowEmpty"></param>
		/// <param name="setup">The setup object for the email-address control.</param>
		/// <param name="maxLength">The maximum number of characters a user can input.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public EmailAddressControl(
			string value, bool allowEmpty, EmailAddressControlSetup setup = null, int? maxLength = null, Action<string, Validator> validationMethod = null ) {
			setup = setup ?? EmailAddressControlSetup.Create();
			( Labeler, PageComponent, Validation ) = setup.TextControlSetup.LabelerAndComponentAndValidationGetter(
				value,
				allowEmpty,
				null,
				maxLength,
				( postBackValue, validator ) => {
					var errorHandler = new ValidationErrorHandler( "email address" );
					var validatedValue = maxLength.HasValue
						                     ? validator.GetEmailAddress( errorHandler, postBackValue, allowEmpty, maxLength: maxLength.Value )
						                     : validator.GetEmailAddress( errorHandler, postBackValue, allowEmpty );
					return errorHandler.LastResult != ErrorCondition.NoError ? null : validatedValue;
				},
				validationMethod );
		}
	}
}