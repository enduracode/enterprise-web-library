using System;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A plain-text edit control.
	/// </summary>
	public class TextControl: FormControl<PhrasingComponent> {
		private readonly PhrasingComponent component;
		private readonly EwfValidation validation;

		/// <summary>
		/// Creates a text control.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="allowEmpty"></param>
		/// <param name="validationMethod">The validation method. Do not pass null.</param>
		/// <param name="setup">The setup object for the text control.</param>
		/// <param name="maxLength">The maximum number of characters a user can input.</param>
		public TextControl( string value, bool allowEmpty, Action<string, Validator> validationMethod, TextControlSetup setup = null, int? maxLength = null ) {
			setup = setup ?? TextControlSetup.Create();
			(component, validation) = setup.ComponentAndValidationGetter( value, allowEmpty, maxLength, validationMethod );
		}

		public PhrasingComponent PageComponent => component;
		public EwfValidation Validation => validation;
	}
}