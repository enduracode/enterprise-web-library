using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Phrasing;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A numeric-text edit control.
/// </summary>
public class NumericTextControl: FormControl<PhrasingComponent> {
	public FormControlLabeler Labeler { get; }
	public PhrasingComponent PageComponent { get; }
	public EwfValidation? Validation { get; }

	/// <summary>
	/// Creates a numeric-text control.
	/// </summary>
	/// <param name="value">Do not pass null.</param>
	/// <param name="allowEmpty"></param>
	/// <param name="setup">The setup object for the numeric-text control.</param>
	/// <param name="minLength"></param>
	/// <param name="maxLength">The maximum number of characters a user can input.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public NumericTextControl(
		string value, bool allowEmpty, NumericTextControlSetup? setup = null, int? minLength = null, int? maxLength = null,
		Action<string, Validator>? validationMethod = null ) {
		setup ??= NumericTextControlSetup.Create();
		( Labeler, PageComponent, Validation ) = setup.TextControlSetup.LabelerAndComponentAndValidationGetter(
			value,
			allowEmpty,
			minLength,
			maxLength,
			( postBackValue, validator ) => {
				if( postBackValue.All( char.IsDigit ) )
					return postBackValue;
				validator.NoteErrorAndAddMessage( "The value must be a number." );
				return null;
			},
			validationMethod );
	}
}