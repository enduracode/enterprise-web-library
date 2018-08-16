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

		public static DateControl ToDateControl(
			this DataValue<DateTime> dataValue, DateControlSetup setup = null, SpecifiedValue<DateTime?> value = null, LocalDate? minValue = null,
			LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			var nullableValue = new DataValue<DateTime?> { Value = value != null ? value.Value : dataValue.Value };
			return nullableValue.ToDateControl(
				setup: setup,
				allowEmpty: false,
				minValue: minValue,
				maxValue: maxValue,
				additionalValidationMethod: validator => {
					dataValue.Value = nullableValue.Value.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DateControl ToDateControl(
			this DataValue<DateTime?> dataValue, DateControlSetup setup = null, SpecifiedValue<DateTime?> value = null, bool allowEmpty = true,
			LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			var localDateValue =
				new DataValue<LocalDate?> { Value = ( value != null ? value.Value : dataValue.Value ).ToNewUnderlyingValue( LocalDate.FromDateTime ) };
			return localDateValue.ToDateControl(
				setup: setup,
				allowEmpty: allowEmpty,
				minValue: minValue,
				maxValue: maxValue,
				additionalValidationMethod: validator => {
					dataValue.Value = localDateValue.Value.ToNewUnderlyingValue( i => i.ToDateTimeUnspecified() );
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DateAndTimeControl ToDateAndTimeControl(
			this DataValue<LocalDateTime> dataValue, DateAndTimeControlSetup setup = null, SpecifiedValue<LocalDateTime?> value = null, LocalDate? minValue = null,
			LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			return new DateAndTimeControl(
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

		public static DateAndTimeControl ToDateAndTimeControl(
			this DataValue<LocalDateTime?> dataValue, DateAndTimeControlSetup setup = null, SpecifiedValue<LocalDateTime?> value = null, bool allowEmpty = true,
			LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			return new DateAndTimeControl(
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

		public static DateAndTimeControl ToDateAndTimeControl(
			this DataValue<DateTime> dataValue, DateAndTimeControlSetup setup = null, SpecifiedValue<DateTime?> value = null, LocalDate? minValue = null,
			LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			var nullableValue = new DataValue<DateTime?> { Value = value != null ? value.Value : dataValue.Value };
			return nullableValue.ToDateAndTimeControl(
				setup: setup,
				allowEmpty: false,
				minValue: minValue,
				maxValue: maxValue,
				additionalValidationMethod: validator => {
					dataValue.Value = nullableValue.Value.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DateAndTimeControl ToDateAndTimeControl(
			this DataValue<DateTime?> dataValue, DateAndTimeControlSetup setup = null, SpecifiedValue<DateTime?> value = null, bool allowEmpty = true,
			LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator> additionalValidationMethod = null ) {
			var localDateTimeValue =
				new DataValue<LocalDateTime?> { Value = ( value != null ? value.Value : dataValue.Value ).ToNewUnderlyingValue( LocalDateTime.FromDateTime ) };
			return localDateTimeValue.ToDateAndTimeControl(
				setup: setup,
				allowEmpty: allowEmpty,
				minValue: minValue,
				maxValue: maxValue,
				additionalValidationMethod: validator => {
					dataValue.Value = localDateTimeValue.Value.ToNewUnderlyingValue( i => i.ToDateTimeUnspecified() );
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DurationControl ToDurationControl(
			this DataValue<TimeSpan> dataValue, DurationControlSetup setup = null, SpecifiedValue<TimeSpan?> value = null,
			Action<Validator> additionalValidationMethod = null ) {
			return new DurationControl(
				value != null ? value.Value : dataValue.Value,
				false,
				setup: setup,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DurationControl ToDurationControl(
			this DataValue<TimeSpan?> dataValue, DurationControlSetup setup = null, SpecifiedValue<TimeSpan?> value = null, bool allowEmpty = true,
			Action<Validator> additionalValidationMethod = null ) {
			return new DurationControl(
				value != null ? value.Value : dataValue.Value,
				allowEmpty,
				setup: setup,
				validationMethod: ( postBackValue, validator ) => {
					dataValue.Value = postBackValue;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DurationControl ToDurationControl(
			this DataValue<int> dataValue, DurationControlSetup setup = null, SpecifiedValue<int?> value = null,
			Action<Validator> additionalValidationMethod = null ) {
			var nullableValue = new DataValue<int?> { Value = value != null ? value.Value : dataValue.Value };
			return nullableValue.ToDurationControl(
				setup: setup,
				allowEmpty: false,
				additionalValidationMethod: validator => {
					dataValue.Value = nullableValue.Value.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DurationControl ToDurationControl(
			this DataValue<int?> dataValue, DurationControlSetup setup = null, SpecifiedValue<int?> value = null, bool allowEmpty = true,
			Action<Validator> additionalValidationMethod = null ) {
			var timeSpanValue = new DataValue<TimeSpan?>
				{
					Value = ( value != null ? value.Value : dataValue.Value ).ToNewUnderlyingValue( v => TimeSpan.FromSeconds( v ) )
				};
			return timeSpanValue.ToDurationControl(
				setup: setup,
				allowEmpty: allowEmpty,
				additionalValidationMethod: validator => {
					dataValue.Value = timeSpanValue.Value.ToNewUnderlyingValue( i => (int)i.TotalSeconds );
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DurationControl ToDurationControl(
			this DataValue<decimal> dataValue, DurationControlSetup setup = null, SpecifiedValue<decimal?> value = null,
			Action<Validator> additionalValidationMethod = null ) {
			var nullableValue = new DataValue<decimal?> { Value = value != null ? value.Value : dataValue.Value };
			return nullableValue.ToDurationControl(
				setup: setup,
				allowEmpty: false,
				additionalValidationMethod: validator => {
					dataValue.Value = nullableValue.Value.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}

		public static DurationControl ToDurationControl(
			this DataValue<decimal?> dataValue, DurationControlSetup setup = null, SpecifiedValue<decimal?> value = null, bool allowEmpty = true,
			Action<Validator> additionalValidationMethod = null ) {
			var intValue = new DataValue<int?> { Value = (int?)( value != null ? value.Value : dataValue.Value ) };
			return intValue.ToDurationControl(
				setup: setup,
				allowEmpty: allowEmpty,
				additionalValidationMethod: validator => {
					dataValue.Value = intValue.Value;
					additionalValidationMethod?.Invoke( validator );
				} );
		}
	}
}