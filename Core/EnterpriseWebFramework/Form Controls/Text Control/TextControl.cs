using System;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A plain-text edit control.
	/// </summary>
	public class TextControl: FormControl<PhrasingComponent> {
		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a text control.
		/// </summary>
		/// <param name="value">Do not pass null.</param>
		/// <param name="allowEmpty"></param>
		/// <param name="setup">The setup object for the text control.</param>
		/// <param name="maxLength">The maximum number of characters a user can input.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public TextControl(
			string value, bool allowEmpty, TextControlSetup setup = null, int? maxLength = null, Action<string, Validator> validationMethod = null ) {
			setup = setup ?? TextControlSetup.Create();
			( Labeler, PageComponent, Validation ) = setup.LabelerAndComponentAndValidationGetter(
				value,
				allowEmpty,
				null,
				maxLength,
				( postBackValue, validator ) => postBackValue,
				validationMethod );
		}
	}
}