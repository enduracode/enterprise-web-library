using System;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A number edit control.
	/// </summary>
	public class NumberControl: FormControl<PhrasingComponent> {
		public FormControlLabeler Labeler { get; }
		public PhrasingComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a number control.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="allowEmpty"></param>
		/// <param name="setup">The setup object for the number control.</param>
		/// <param name="minValue">The smallest allowed value.</param>
		/// <param name="maxValue">The largest allowed value.</param>
		/// <param name="valueStep">The allowed granularity of the value. Do not pass zero or a negative number. Pass null to allow any value.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public NumberControl(
			decimal? value, bool allowEmpty, NumberControlSetup setup = null, decimal? minValue = null, decimal? maxValue = null, decimal? valueStep = null,
			Action<decimal?, Validator> validationMethod = null ) {
			setup = setup ?? NumberControlSetup.Create();
			( Labeler, PageComponent, Validation ) = setup.LabelerAndComponentAndValidationGetter(
				value,
				allowEmpty,
				minValue,
				maxValue,
				valueStep,
				validationMethod );
		}
	}
}