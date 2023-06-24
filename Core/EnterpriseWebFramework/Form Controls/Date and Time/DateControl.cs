using NodaTime;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A date edit control.
/// </summary>
public class DateControl: FormControl<PhrasingComponent> {
	public FormControlLabeler Labeler { get; }
	public PhrasingComponent PageComponent { get; }
	public EwfValidation Validation { get; }

	/// <summary>
	/// Creates a date control.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="allowEmpty"></param>
	/// <param name="setup">The setup object for the date control.</param>
	/// <param name="minValue">The earliest acceptable date.</param>
	/// <param name="maxValue">The latest acceptable date.</param>
	/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
	public DateControl(
		LocalDate? value, bool allowEmpty, DateControlSetup setup = null, LocalDate? minValue = null, LocalDate? maxValue = null,
		Action<LocalDate?, Validator> validationMethod = null ) {
		setup ??= DateControlSetup.Create();
		( Labeler, PageComponent, Validation ) = setup.LabelerAndComponentAndValidationGetter( value, allowEmpty, minValue, maxValue, validationMethod );
	}
}