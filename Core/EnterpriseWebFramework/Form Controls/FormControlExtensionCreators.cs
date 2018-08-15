using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.InputValidation;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class FormControlExtensionCreators {
		public static TextControl ToTextControl(
			this DataValue<string> dataValue, bool allowEmpty, TextControlSetup setup = null, string value = null, int? maxLength = null,
			Action<Validator> additionalValidationMethod = null ) {
			return new TextControl(
				value ?? dataValue.Value,
				allowEmpty,
				setup: setup,
				maxLength: maxLength,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static EmailAddressControl ToEmailAddressControl(
			this DataValue<string> dataValue, bool allowEmpty, EmailAddressControlSetup setup = null, string value = null, int? maxLength = null,
			Action<Validator> additionalValidationMethod = null ) {
			return new EmailAddressControl(
				value ?? dataValue.Value,
				allowEmpty,
				setup: setup,
				maxLength: maxLength,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static TelephoneNumberControl ToTelephoneNumberControl(
			this DataValue<string> dataValue, bool allowEmpty, TelephoneNumberControlSetup setup = null, string value = null, int? maxLength = null,
			Action<Validator> additionalValidationMethod = null ) {
			return new TelephoneNumberControl(
				value ?? dataValue.Value,
				allowEmpty,
				setup: setup,
				maxLength: maxLength,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static UrlControl ToUrlControl(
			this DataValue<string> dataValue, bool allowEmpty, UrlControlSetup setup = null, string value = null, int? maxLength = null,
			Action<Validator> additionalValidationMethod = null ) {
			return new UrlControl(
				value ?? dataValue.Value,
				allowEmpty,
				setup: setup,
				maxLength: maxLength,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static WysiwygHtmlEditor ToHtmlEditor(
			this DataValue<string> dataValue, bool allowEmpty, WysiwygHtmlEditorSetup setup = null, string value = null, int? maxLength = null,
			Action<Validator> additionalValidationMethod = null ) {
			return new WysiwygHtmlEditor(
				value ?? dataValue.Value,
				allowEmpty,
				( postBackValue, validator ) => {
					dataValue.Value = postBackValue;
					additionalValidationMethod?.Invoke( validator );
				},
				setup: setup,
				maxLength: maxLength );
		}

		/// <summary>
		/// Creates a block checkbox for this data value.
		/// </summary>
		/// <param name="dataValue"></param>
		/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
		/// <param name="setup">The setup object for the checkbox.</param>
		/// <param name="value"></param>
		/// <param name="additionalValidationMethod"></param>
		public static BlockCheckBox ToBlockCheckbox(
			this DataValue<bool> dataValue, IEnumerable<PhrasingComponent> label, BlockCheckBoxSetup setup = null, bool? value = null,
			Action<Validator> additionalValidationMethod = null ) {
			return new BlockCheckBox(
				value ?? dataValue.Value,
				label,
				setup: setup,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		/// <summary>
		/// Creates a block checkbox for this data value.
		/// </summary>
		/// <param name="dataValue"></param>
		/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
		/// <param name="setup">The setup object for the checkbox.</param>
		/// <param name="value"></param>
		/// <param name="additionalValidationMethod"></param>
		public static BlockCheckBox ToBlockCheckbox(
			this DataValue<decimal> dataValue, IEnumerable<PhrasingComponent> label, BlockCheckBoxSetup setup = null, decimal? value = null,
			Action<Validator> additionalValidationMethod = null ) {
			var boolValue = new DataValue<bool> { Value = ( value ?? dataValue.Value ).DecimalToBoolean() };
			return boolValue.ToBlockCheckbox(
				label,
				setup: setup,
				additionalValidationMethod: validator => {
					dataValue.Value = boolValue.Value.BooleanToDecimal();
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DateControl ToDateControl(
			this DataValue<LocalDate> dataValue, DateControlSetup setup = null, SpecifiedValue<LocalDate?> value = null, LocalDate? minValue = null,
			LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			return new DateControl(
				value != null ? value.Value : dataValue.Value,
				false,
				setup: setup,
				minValue: minValue,
				maxValue: maxValue,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DateControl ToDateControl(
			this DataValue<LocalDate?> dataValue, DateControlSetup setup = null, SpecifiedValue<LocalDate?> value = null, bool allowEmpty = true,
			LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			return new DateControl(
				value != null ? value.Value : dataValue.Value,
				allowEmpty,
				setup: setup,
				minValue: minValue,
				maxValue: maxValue,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue;
					additionalValidationMethod?.Invoke( validator );
				} );
		}
	}
}