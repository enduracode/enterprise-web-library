using System;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An imprecise-number edit control.
	/// </summary>
	public class ImpreciseNumberControl: FormControl<PhrasingComponent> {
		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates an imprecise-number control.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="minValue">The smallest allowed value.</param>
		/// <param name="maxValue">The largest allowed value.</param>
		/// <param name="setup">The setup object for the imprecise-number control.</param>
		/// <param name="valueStep">The allowed granularity of the value. Do not pass zero or a negative number. Pass null to allow any value.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public ImpreciseNumberControl(
			decimal value, decimal minValue, decimal maxValue, ImpreciseNumberControlSetup setup = null, decimal? valueStep = null,
			Action<decimal, Validator> validationMethod = null ) {
			setup = setup ?? ImpreciseNumberControlSetup.Create();
			( Labeler, PageComponent, Validation ) = setup.NumberControlSetup.LabelerAndComponentAndValidationGetter(
				value,
				false,
				minValue,
				maxValue,
				valueStep,
				( postBackValue, validator ) => validationMethod( postBackValue.Value, validator ) );
		}
	}
}