using System;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A URL edit control.
	/// </summary>
	public class UrlControl: FormControl<PhrasingComponent> {
		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a URL control.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="allowEmpty"></param>
		/// <param name="setup">The setup object for the URL control.</param>
		/// <param name="maxLength">The maximum number of characters a user can input.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public UrlControl( string value, bool allowEmpty, UrlControlSetup setup = null, int? maxLength = null, Action<string, Validator> validationMethod = null ) {
			setup = setup ?? UrlControlSetup.Create();
			( Labeler, PageComponent, Validation ) = setup.TextControlSetup.LabelerAndComponentAndValidationGetter(
				value,
				allowEmpty,
				null,
				maxLength,
				( postBackValue, validator ) => {
					var errorHandler = new ValidationErrorHandler( "URL" );
					var validatedValue = maxLength.HasValue
						                     ? validator.GetUrl( errorHandler, postBackValue, allowEmpty, maxLength.Value )
						                     : validator.GetUrl( errorHandler, postBackValue, allowEmpty );
					return errorHandler.LastResult != ErrorCondition.NoError ? null : validatedValue;
				},
				validationMethod );
		}
	}
}