#nullable disable warnings
using System.Globalization;
using JetBrains.Annotations;
using NodaTime;
using Tewl.InputValidation;
using Tewl.Oracle;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public static class FormControlExtensionCreators {
	public static TextControl ToTextControl(
		this AbstractDataValue<string> dataValue, bool allowEmpty, TextControlSetup? setup = null, string? value = null, int? minLength = null,
		int? maxLength = null, Action<Validator>? additionalValidationMethod = null ) {
		return new TextControl(
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			allowEmpty,
			setup: setup,
			minLength: minLength,
			maxLength: maxLength,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static EmailAddressControl ToEmailAddressControl(
		this AbstractDataValue<string> dataValue, bool allowEmpty, EmailAddressControlSetup? setup = null, string? value = null, int? maxLength = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new EmailAddressControl(
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			allowEmpty,
			setup: setup,
			maxLength: maxLength,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static TelephoneNumberControl ToTelephoneNumberControl(
		this AbstractDataValue<string> dataValue, bool allowEmpty, TelephoneNumberControlSetup? setup = null, string? value = null, int? maxLength = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new TelephoneNumberControl(
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			allowEmpty,
			setup: setup,
			maxLength: maxLength,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static UrlControl ToUrlControl(
		this AbstractDataValue<string> dataValue, bool allowEmpty, UrlControlSetup? setup = null, string? value = null, int? maxLength = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new UrlControl(
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			allowEmpty,
			setup: setup,
			maxLength: maxLength,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static WysiwygHtmlEditor ToHtmlEditor(
		this AbstractDataValue<string> dataValue, bool allowEmpty, WysiwygHtmlEditorSetup? setup = null, string? value = null, int? maxLength = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new WysiwygHtmlEditor(
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			allowEmpty,
			( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			},
			setup: setup,
			maxLength: maxLength );
	}

	public static NumericTextControl ToNumericTextControl(
		this AbstractDataValue<string> dataValue, bool allowEmpty, NumericTextControlSetup? setup = null, string? value = null, int? minLength = null,
		int? maxLength = null, Action<Validator>? additionalValidationMethod = null ) {
		return new NumericTextControl(
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			allowEmpty,
			setup: setup,
			minLength: minLength,
			maxLength: maxLength,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumericTextControl ToTextControl(
		this AbstractDataValue<int> dataValue, NumericTextControlSetup? setup = null, SpecifiedValue<int?>? value = null, int? minValue = null,
		int? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (int?)v );
		return nullableValue.ToTextControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumericTextControl ToTextControl(
		this AbstractDataValue<int?> dataValue, NumericTextControlSetup? setup = null, SpecifiedValue<int?>? value = null, bool allowEmpty = true,
		int? minValue = null, int? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		var longValue = dataValue.CreateNewValue( v => (long?)v );
		return longValue.ToTextControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<long?>( value.Value ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue,
			maxValue: maxValue ?? int.MaxValue,
			additionalValidationMethod: validator => {
				dataValue.Value = (int?)longValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumericTextControl ToTextControl(
		this AbstractDataValue<long> dataValue, NumericTextControlSetup? setup = null, SpecifiedValue<long?>? value = null, long? minValue = null,
		long? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (long?)v );
		return nullableValue.ToTextControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumericTextControl ToTextControl(
		this AbstractDataValue<long?> dataValue, NumericTextControlSetup? setup = null, SpecifiedValue<long?>? value = null, bool allowEmpty = true,
		long? minValue = null, long? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		minValue ??= 1;
		maxValue ??= long.MaxValue;
		if( minValue.Value < 1 || maxValue.Value < 1 )
			throw new ApplicationException( "minValue and maxValue must be positive integers." );

		var v = value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null;
		if( v.HasValue && ( v.Value < minValue.Value || v.Value > maxValue.Value ) )
			throw new ApplicationException( "The value must be between minValue and maxValue." );

		return new NumericTextControl(
			v?.ToString( "D", CultureInfo.InvariantCulture ) ?? "",
			allowEmpty,
			setup: setup,
			maxLength: maxValue.Value.ToString( "D", CultureInfo.InvariantCulture ).Length,
			validationMethod: ( postBackValue, validator ) => {
				if( postBackValue.Any() ) {
					if( !long.TryParse( postBackValue, NumberStyles.None, CultureInfo.InvariantCulture, out var result ) || result > maxValue.Value ) {
						validator.NoteErrorAndAddMessage( "The value is too large." );
						setup?.ValidationErrorNotifier?.Invoke();
						return;
					}
					if( result < minValue.Value ) {
						validator.NoteErrorAndAddMessage( "The value is too small." );
						setup?.ValidationErrorNotifier?.Invoke();
						return;
					}

					dataValue.Value = result;
				}
				else
					dataValue.Value = null;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<int> dataValue, NumberControlSetup? setup = null, SpecifiedValue<int?>? value = null, int? minValue = null, int? maxValue = null,
		int? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (int?)v );
		return nullableValue.ToNumberControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<int?> dataValue, NumberControlSetup? setup = null, SpecifiedValue<int?>? value = null, bool allowEmpty = true, int? minValue = null,
		int? maxValue = null, int? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var longValue = dataValue.CreateNewValue( v => (long?)v );
		return longValue.ToNumberControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<long?>( value.Value ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue ?? int.MinValue,
			maxValue: maxValue ?? int.MaxValue,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = (int?)longValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<long> dataValue, NumberControlSetup? setup = null, SpecifiedValue<long?>? value = null, long? minValue = null, long? maxValue = null,
		long? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (long?)v );
		return nullableValue.ToNumberControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<long?> dataValue, NumberControlSetup? setup = null, SpecifiedValue<long?>? value = null, bool allowEmpty = true,
		long? minValue = null, long? maxValue = null, long? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var decimalValue = dataValue.CreateNewValue( v => (decimal?)v );
		return decimalValue.ToNumberControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<decimal?>( value.Value ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue ?? long.MinValue,
			maxValue: maxValue ?? long.MaxValue,
			valueStep: valueStep ?? 1,
			additionalValidationMethod: validator => {
				dataValue.Value = (long?)decimalValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<short> dataValue, NumberControlSetup? setup = null, SpecifiedValue<short?>? value = null, short? minValue = null,
		short? maxValue = null, short? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (short?)v );
		return nullableValue.ToNumberControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<short?> dataValue, NumberControlSetup? setup = null, SpecifiedValue<short?>? value = null, bool allowEmpty = true,
		short? minValue = null, short? maxValue = null, short? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var longValue = dataValue.CreateNewValue( v => (long?)v );
		return longValue.ToNumberControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<long?>( value.Value ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue ?? short.MinValue,
			maxValue: maxValue ?? short.MaxValue,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = (short?)longValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<byte> dataValue, NumberControlSetup? setup = null, SpecifiedValue<byte?>? value = null, byte? minValue = null, byte? maxValue = null,
		byte? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (byte?)v );
		return nullableValue.ToNumberControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<byte?> dataValue, NumberControlSetup? setup = null, SpecifiedValue<byte?>? value = null, bool allowEmpty = true,
		byte? minValue = null, byte? maxValue = null, byte? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var longValue = dataValue.CreateNewValue( v => (long?)v );
		return longValue.ToNumberControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<long?>( value.Value ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue ?? byte.MinValue,
			maxValue: maxValue ?? byte.MaxValue,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = (byte?)longValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<decimal> dataValue, NumberControlSetup? setup = null, SpecifiedValue<decimal?>? value = null, decimal? minValue = null,
		decimal? maxValue = null, decimal? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		return new NumberControl(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			false,
			setup: setup,
			minValue: minValue,
			maxValue: maxValue,
			valueStep: valueStep,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static NumberControl ToNumberControl(
		this AbstractDataValue<decimal?> dataValue, NumberControlSetup? setup = null, SpecifiedValue<decimal?>? value = null, bool allowEmpty = true,
		decimal? minValue = null, decimal? maxValue = null, decimal? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		return new NumberControl(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			allowEmpty,
			setup: setup,
			minValue: minValue,
			maxValue: maxValue,
			valueStep: valueStep,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static ImpreciseNumberControl ToImpreciseNumberControl(
		this AbstractDataValue<int> dataValue, int minValue, int maxValue, ImpreciseNumberControlSetup? setup = null, int? value = null, int? valueStep = null,
		Action<Validator>? additionalValidationMethod = null ) {
		var longValue = dataValue.CreateNewValue( v => (long)v );
		return longValue.ToImpreciseNumberControl(
			minValue,
			maxValue,
			setup: setup,
			value: value,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = (int)longValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static ImpreciseNumberControl ToImpreciseNumberControl(
		this AbstractDataValue<long> dataValue, long minValue, long maxValue, ImpreciseNumberControlSetup? setup = null, long? value = null, long? valueStep = null,
		Action<Validator>? additionalValidationMethod = null ) {
		var decimalValue = dataValue.CreateNewValue( v => (decimal)v );
		return decimalValue.ToImpreciseNumberControl(
			minValue,
			maxValue,
			setup: setup,
			value: value,
			valueStep: valueStep ?? 1,
			additionalValidationMethod: validator => {
				dataValue.Value = (long)decimalValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static ImpreciseNumberControl ToImpreciseNumberControl(
		this AbstractDataValue<short> dataValue, short minValue, short maxValue, ImpreciseNumberControlSetup? setup = null, short? value = null,
		short? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		var longValue = dataValue.CreateNewValue( v => (long)v );
		return longValue.ToImpreciseNumberControl(
			minValue,
			maxValue,
			setup: setup,
			value: value,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = (short)longValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static ImpreciseNumberControl ToImpreciseNumberControl(
		this AbstractDataValue<byte> dataValue, byte minValue, byte maxValue, ImpreciseNumberControlSetup? setup = null, byte? value = null, byte? valueStep = null,
		Action<Validator>? additionalValidationMethod = null ) {
		var longValue = dataValue.CreateNewValue( v => (long)v );
		return longValue.ToImpreciseNumberControl(
			minValue,
			maxValue,
			setup: setup,
			value: value,
			valueStep: valueStep,
			additionalValidationMethod: validator => {
				dataValue.Value = (byte)longValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static ImpreciseNumberControl ToImpreciseNumberControl(
		this AbstractDataValue<decimal> dataValue, decimal minValue, decimal maxValue, ImpreciseNumberControlSetup? setup = null, decimal? value = null,
		decimal? valueStep = null, Action<Validator>? additionalValidationMethod = null ) {
		return new ImpreciseNumberControl(
			value ?? ( dataValue.DataExists ? dataValue.Value : 0 ),
			minValue,
			maxValue,
			setup: setup,
			valueStep: valueStep,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a checkbox for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the checkbox.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static Checkbox ToCheckbox(
		this AbstractDataValue<bool> dataValue, IReadOnlyCollection<PhrasingComponent> label, CheckboxSetup? setup = null, bool? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new Checkbox(
			value ?? ( dataValue.DataExists && dataValue.Value ),
			label,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a checkbox for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the checkbox.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static Checkbox ToCheckbox(
		this AbstractDataValue<decimal> dataValue, IReadOnlyCollection<PhrasingComponent> label, CheckboxSetup? setup = null, decimal? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		var boolValue = dataValue.CreateNewValue( v => v.DecimalToBoolean() );
		return boolValue.ToCheckbox(
			label,
			setup: setup,
			value: value?.DecimalToBoolean(),
			additionalValidationMethod: validator => {
				dataValue.Value = boolValue.Value.BooleanToDecimal();
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a flow checkbox for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the flow checkbox.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static FlowCheckbox ToFlowCheckbox(
		this AbstractDataValue<bool> dataValue, IReadOnlyCollection<PhrasingComponent> label, FlowCheckboxSetup? setup = null, bool? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new FlowCheckbox(
			value ?? ( dataValue.DataExists && dataValue.Value ),
			label,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a flow checkbox for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="label">The checkbox label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the flow checkbox.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static FlowCheckbox ToFlowCheckbox(
		this AbstractDataValue<decimal> dataValue, IReadOnlyCollection<PhrasingComponent> label, FlowCheckboxSetup? setup = null, decimal? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		var boolValue = dataValue.CreateNewValue( v => v.DecimalToBoolean() );
		return boolValue.ToFlowCheckbox(
			label,
			setup: setup,
			value: value?.DecimalToBoolean(),
			additionalValidationMethod: validator => {
				dataValue.Value = boolValue.Value.BooleanToDecimal();
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a radio button for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="group">The group of which this radio button will be a part. Do not pass null.</param>
	/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the radio button.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static Checkbox ToRadioButton(
		this AbstractDataValue<bool> dataValue, RadioButtonGroup group, IReadOnlyCollection<PhrasingComponent> label, RadioButtonSetup? setup = null,
		bool? value = null, Action<Validator>? additionalValidationMethod = null ) {
		return group.CreateRadioButton(
			value ?? ( dataValue.DataExists && dataValue.Value ),
			label,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a radio button for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="group">The group of which this radio button will be a part. Do not pass null.</param>
	/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the radio button.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static Checkbox ToRadioButton(
		this AbstractDataValue<decimal> dataValue, RadioButtonGroup group, IReadOnlyCollection<PhrasingComponent> label, RadioButtonSetup? setup = null,
		decimal? value = null, Action<Validator>? additionalValidationMethod = null ) {
		var boolValue = dataValue.CreateNewValue( v => v.DecimalToBoolean() );
		return boolValue.ToRadioButton(
			group,
			label,
			setup: setup,
			value: value?.DecimalToBoolean(),
			additionalValidationMethod: validator => {
				dataValue.Value = boolValue.Value.BooleanToDecimal();
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a flow radio button for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="group">The group of which this radio button will be a part. Do not pass null.</param>
	/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the flow radio button.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static FlowCheckbox ToFlowRadioButton(
		this AbstractDataValue<bool> dataValue, RadioButtonGroup group, IReadOnlyCollection<PhrasingComponent> label, FlowRadioButtonSetup? setup = null,
		bool? value = null, Action<Validator>? additionalValidationMethod = null ) {
		return group.CreateFlowRadioButton(
			value ?? ( dataValue.DataExists && dataValue.Value ),
			label,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	/// <summary>
	/// Creates a flow radio button for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="group">The group of which this radio button will be a part. Do not pass null.</param>
	/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the flow radio button.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static FlowCheckbox ToFlowRadioButton(
		this AbstractDataValue<decimal> dataValue, RadioButtonGroup group, IReadOnlyCollection<PhrasingComponent> label, FlowRadioButtonSetup? setup = null,
		decimal? value = null, Action<Validator>? additionalValidationMethod = null ) {
		var boolValue = dataValue.CreateNewValue( v => v.DecimalToBoolean() );
		return boolValue.ToFlowRadioButton(
			group,
			label,
			setup: setup,
			value: value?.DecimalToBoolean(),
			additionalValidationMethod: validator => {
				dataValue.Value = boolValue.Value.BooleanToDecimal();
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<bool?> ToRadioList(
		this AbstractDataValue<bool> dataValue, RadioListSetup<bool?> setup, SpecifiedValue<bool?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<bool?> ToRadioList(
		this AbstractDataValue<bool?> dataValue, RadioListSetup<bool?> setup, string defaultValueItemLabel = "None", SpecifiedValue<bool?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<int?> ToRadioList(
		this AbstractDataValue<int> dataValue, RadioListSetup<int?> setup, SpecifiedValue<int?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<int?> ToRadioList(
		this AbstractDataValue<int?> dataValue, RadioListSetup<int?> setup, string defaultValueItemLabel = "None", SpecifiedValue<int?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<long?> ToRadioList(
		this AbstractDataValue<long> dataValue, RadioListSetup<long?> setup, SpecifiedValue<long?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<long?> ToRadioList(
		this AbstractDataValue<long?> dataValue, RadioListSetup<long?> setup, string defaultValueItemLabel = "None", SpecifiedValue<long?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<string> ToRadioList(
		this AbstractDataValue<string> dataValue, RadioListSetup<string> setup, string defaultValueItemLabel = "", string? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateRadioList(
			setup,
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			defaultValueItemLabel: defaultValueItemLabel,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<decimal?> ToRadioList(
		this AbstractDataValue<decimal> dataValue, RadioListSetup<decimal?> setup, SpecifiedValue<decimal?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<decimal?> ToRadioList(
		this AbstractDataValue<decimal?> dataValue, RadioListSetup<decimal?> setup, string defaultValueItemLabel = "None", SpecifiedValue<decimal?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateRadioList(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<bool?> ToDropDown(
		this AbstractDataValue<bool> dataValue, DropDownSetup<bool?> setup, SpecifiedValue<bool?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			placeholderIsValid: false,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<bool?> ToDropDown(
		this AbstractDataValue<bool?> dataValue, DropDownSetup<bool?> setup, string defaultValueItemLabel, bool placeholderIsValid = true,
		SpecifiedValue<bool?>? value = null, Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			placeholderIsValid: placeholderIsValid,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<int?> ToDropDown(
		this AbstractDataValue<int> dataValue, DropDownSetup<int?> setup, SpecifiedValue<int?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			placeholderIsValid: false,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<int?> ToDropDown(
		this AbstractDataValue<int?> dataValue, DropDownSetup<int?> setup, string defaultValueItemLabel, bool placeholderIsValid = true,
		SpecifiedValue<int?>? value = null, Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			placeholderIsValid: placeholderIsValid,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<long?> ToDropDown(
		this AbstractDataValue<long> dataValue, DropDownSetup<long?> setup, SpecifiedValue<long?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			placeholderIsValid: false,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<long?> ToDropDown(
		this AbstractDataValue<long?> dataValue, DropDownSetup<long?> setup, string defaultValueItemLabel, bool placeholderIsValid = true,
		SpecifiedValue<long?>? value = null, Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			placeholderIsValid: placeholderIsValid,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<string> ToDropDown(
		this AbstractDataValue<string> dataValue, DropDownSetup<string> setup, string defaultValueItemLabel = "", bool placeholderIsValid = false,
		string? value = null, Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateDropDown(
			setup,
			value ?? ( dataValue.DataExists ? dataValue.Value : "" ),
			defaultValueItemLabel: defaultValueItemLabel,
			placeholderIsValid: placeholderIsValid,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static SelectList<decimal?> ToDropDown(
		this AbstractDataValue<decimal> dataValue, DropDownSetup<decimal?> setup, SpecifiedValue<decimal?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		if( setup.Items.Any( i => !i.Id.HasValue ) )
			throw new ApplicationException( "An item with a null ID cannot be a valid selection." );
		return SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: "",
			placeholderIsValid: false,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static SelectList<decimal?> ToDropDown(
		this AbstractDataValue<decimal?> dataValue, DropDownSetup<decimal?> setup, string defaultValueItemLabel, bool placeholderIsValid = true,
		SpecifiedValue<decimal?>? value = null, Action<Validator>? additionalValidationMethod = null ) =>
		SelectList.CreateDropDown(
			setup,
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			defaultValueItemLabel: defaultValueItemLabel,
			placeholderIsValid: placeholderIsValid,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<bool?> ToFreeFormRadioList(
		this AbstractDataValue<bool> dataValue, FreeFormRadioListSetup<bool?>? setup = null, SpecifiedValue<bool?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			value == null || value.Value.HasValue ? null : false,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<bool?> ToFreeFormRadioList(
		this AbstractDataValue<bool?> dataValue, bool? noSelectionIsValid, FreeFormRadioListSetup<bool?>? setup = null, SpecifiedValue<bool?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			noSelectionIsValid,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<int?> ToFreeFormRadioList(
		this AbstractDataValue<int> dataValue, FreeFormRadioListSetup<int?>? setup = null, SpecifiedValue<int?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			value == null || value.Value.HasValue ? null : false,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<int?> ToFreeFormRadioList(
		this AbstractDataValue<int?> dataValue, bool? noSelectionIsValid, FreeFormRadioListSetup<int?>? setup = null, SpecifiedValue<int?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			noSelectionIsValid,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<long?> ToFreeFormRadioList(
		this AbstractDataValue<long> dataValue, FreeFormRadioListSetup<long?>? setup = null, SpecifiedValue<long?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			value == null || value.Value.HasValue ? null : false,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<long?> ToFreeFormRadioList(
		this AbstractDataValue<long?> dataValue, bool? noSelectionIsValid, FreeFormRadioListSetup<long?>? setup = null, SpecifiedValue<long?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			noSelectionIsValid,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<string> ToFreeFormRadioList(
		this AbstractDataValue<string> dataValue, bool? noSelectionIsValid, FreeFormRadioListSetup<string>? setup = null, string? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			noSelectionIsValid,
			value ?? dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<decimal?> ToFreeFormRadioList(
		this AbstractDataValue<decimal> dataValue, FreeFormRadioListSetup<decimal?>? setup = null, SpecifiedValue<decimal?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			value == null || value.Value.HasValue ? null : false,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static FreeFormRadioList<decimal?> ToFreeFormRadioList(
		this AbstractDataValue<decimal?> dataValue, bool? noSelectionIsValid, FreeFormRadioListSetup<decimal?>? setup = null,
		SpecifiedValue<decimal?>? value = null, Action<Validator>? additionalValidationMethod = null ) =>
		FreeFormRadioList.Create(
			noSelectionIsValid,
			value != null ? value.Value : dataValue.Value,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	/// <summary>
	/// Creates a checkbox list for this data value.
	/// </summary>
	/// <param name="dataValue"></param>
	/// <param name="setup">The setup object for the checkbox list. Do not pass null.</param>
	/// <param name="value"></param>
	/// <param name="additionalValidationMethod"></param>
	public static CheckboxList<ItemIdType> ToCheckboxList<ItemIdType>(
		this AbstractDataValue<IEnumerable<ItemIdType>> dataValue, CheckboxListSetup<ItemIdType> setup, IEnumerable<ItemIdType>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new CheckboxList<ItemIdType>(
			setup,
			value ?? ( dataValue.DataExists ? dataValue.Value : [ ] ),
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DateControl ToDateControl(
		this AbstractDataValue<LocalDate> dataValue, DateControlSetup? setup = null, SpecifiedValue<LocalDate?>? value = null, LocalDate? minValue = null,
		LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) =>
		new(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			false,
			setup: setup,
			minValue: minValue,
			maxValue: maxValue,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static DateControl ToDateControl(
		this AbstractDataValue<LocalDate?> dataValue, DateControlSetup? setup = null, SpecifiedValue<LocalDate?>? value = null, bool allowEmpty = true,
		LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) =>
		new(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			allowEmpty,
			setup: setup,
			minValue: minValue,
			maxValue: maxValue,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static DateControl ToDateControl(
		this AbstractDataValue<DateTime> dataValue, DateControlSetup? setup = null, SpecifiedValue<DateTime?>? value = null, LocalDate? minValue = null,
		LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (DateTime?)v );
		return nullableValue.ToDateControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DateControl ToDateControl(
		this AbstractDataValue<DateTime?> dataValue, DateControlSetup? setup = null, SpecifiedValue<DateTime?>? value = null, bool allowEmpty = true,
		LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		var localDateValue = dataValue.CreateNewValue( v => v.ToNewUnderlyingValue( LocalDate.FromDateTime ) );
		return localDateValue.ToDateControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<LocalDate?>( value.Value.ToNewUnderlyingValue( LocalDate.FromDateTime ) ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue,
			maxValue: maxValue,
			additionalValidationMethod: validator => {
				dataValue.Value = localDateValue.Value.ToNewUnderlyingValue( i => i.ToDateTimeUnspecified() );
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static TimeControl ToTimeControl(
		this AbstractDataValue<LocalTime> dataValue, TimeControlSetup? setup = null, SpecifiedValue<LocalTime?>? value = null, LocalTime? minValue = null,
		LocalTime? maxValue = null, int minuteInterval = 15, Action<Validator>? additionalValidationMethod = null ) =>
		new(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			false,
			setup: setup,
			minValue: minValue,
			maxValue: maxValue,
			minuteInterval: minuteInterval,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static TimeControl ToTimeControl(
		this AbstractDataValue<LocalTime?> dataValue, TimeControlSetup? setup = null, SpecifiedValue<LocalTime?>? value = null, bool allowEmpty = true,
		LocalTime? minValue = null, LocalTime? maxValue = null, int minuteInterval = 15, Action<Validator>? additionalValidationMethod = null ) =>
		new(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			allowEmpty,
			setup: setup,
			minValue: minValue,
			maxValue: maxValue,
			minuteInterval: minuteInterval,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );

	public static TimeControl ToTimeControl(
		this AbstractDataValue<TimeSpan> dataValue, TimeControlSetup? setup = null, SpecifiedValue<TimeSpan?>? value = null, LocalTime? minValue = null,
		LocalTime? maxValue = null, int minuteInterval = 15, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (TimeSpan?)v );
		return nullableValue.ToTimeControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			minuteInterval: minuteInterval,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static TimeControl ToTimeControl(
		this AbstractDataValue<TimeSpan?> dataValue, TimeControlSetup? setup = null, SpecifiedValue<TimeSpan?>? value = null, bool allowEmpty = true,
		LocalTime? minValue = null, LocalTime? maxValue = null, int minuteInterval = 15, Action<Validator>? additionalValidationMethod = null ) {
		var localTimeValue = dataValue.CreateNewValue( timeSpanValue => timeSpanValue.ToNewUnderlyingValue( v => LocalTime.FromTicksSinceMidnight( v.Ticks ) ) );
		return localTimeValue.ToTimeControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<LocalTime?>( value.Value.ToNewUnderlyingValue( v => LocalTime.FromTicksSinceMidnight( v.Ticks ) ) ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue,
			maxValue: maxValue,
			minuteInterval: minuteInterval,
			additionalValidationMethod: validator => {
				dataValue.Value = localTimeValue.Value.ToNewUnderlyingValue( v => new TimeSpan( v.TickOfDay ) );
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DateAndTimeControl ToDateAndTimeControl(
		this AbstractDataValue<LocalDateTime> dataValue, DateAndTimeControlSetup? setup = null, SpecifiedValue<LocalDateTime?>? value = null,
		LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		return new DateAndTimeControl(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
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
		this AbstractDataValue<LocalDateTime?> dataValue, DateAndTimeControlSetup? setup = null, SpecifiedValue<LocalDateTime?>? value = null,
		bool allowEmpty = true, LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		return new DateAndTimeControl(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
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
		this AbstractDataValue<DateTime> dataValue, DateAndTimeControlSetup? setup = null, SpecifiedValue<DateTime?>? value = null, LocalDate? minValue = null,
		LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (DateTime?)v );
		return nullableValue.ToDateAndTimeControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			minValue: minValue,
			maxValue: maxValue,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DateAndTimeControl ToDateAndTimeControl(
		this AbstractDataValue<DateTime?> dataValue, DateAndTimeControlSetup? setup = null, SpecifiedValue<DateTime?>? value = null, bool allowEmpty = true,
		LocalDate? minValue = null, LocalDate? maxValue = null, Action<Validator>? additionalValidationMethod = null ) {
		var localDateTimeValue = dataValue.CreateNewValue( v => v.ToNewUnderlyingValue( LocalDateTime.FromDateTime ) );
		return localDateTimeValue.ToDateAndTimeControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<LocalDateTime?>( value.Value.ToNewUnderlyingValue( LocalDateTime.FromDateTime ) ) : null,
			allowEmpty: allowEmpty,
			minValue: minValue,
			maxValue: maxValue,
			additionalValidationMethod: validator => {
				dataValue.Value = localDateTimeValue.Value.ToNewUnderlyingValue( i => i.ToDateTimeUnspecified() );
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DurationControl ToDurationControl(
		this AbstractDataValue<Duration> dataValue, DurationControlSetup? setup = null, SpecifiedValue<Duration?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		return new DurationControl(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			false,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DurationControl ToDurationControl(
		this AbstractDataValue<Duration?> dataValue, DurationControlSetup? setup = null, SpecifiedValue<Duration?>? value = null, bool allowEmpty = true,
		Action<Validator>? additionalValidationMethod = null ) {
		return new DurationControl(
			value is not null ? value.Value : dataValue.DataExists ? dataValue.Value : null,
			allowEmpty,
			setup: setup,
			validationMethod: ( postBackValue, validator ) => {
				dataValue.Value = postBackValue;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DurationControl ToDurationControl(
		this AbstractDataValue<int> dataValue, DurationControlSetup? setup = null, SpecifiedValue<int?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (int?)v );
		return nullableValue.ToDurationControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DurationControl ToDurationControl(
		this AbstractDataValue<int?> dataValue, DurationControlSetup? setup = null, SpecifiedValue<int?>? value = null, bool allowEmpty = true,
		Action<Validator>? additionalValidationMethod = null ) {
		var durationValue = dataValue.CreateNewValue( intValue => intValue.ToNewUnderlyingValue( v => Duration.FromSeconds( v ) ) );
		return durationValue.ToDurationControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<Duration?>( value.Value.ToNewUnderlyingValue( v => Duration.FromSeconds( v ) ) ) : null,
			allowEmpty: allowEmpty,
			additionalValidationMethod: validator => {
				dataValue.Value = durationValue.Value.ToNewUnderlyingValue( i => (int)i.TotalSeconds );
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DurationControl ToDurationControl(
		this AbstractDataValue<decimal> dataValue, DurationControlSetup? setup = null, SpecifiedValue<decimal?>? value = null,
		Action<Validator>? additionalValidationMethod = null ) {
		var nullableValue = dataValue.CreateNewValue( v => (decimal?)v );
		return nullableValue.ToDurationControl(
			setup: setup,
			value: value,
			allowEmpty: false,
			additionalValidationMethod: validator => {
				dataValue.Value = nullableValue.Value.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}

	public static DurationControl ToDurationControl(
		this AbstractDataValue<decimal?> dataValue, DurationControlSetup? setup = null, SpecifiedValue<decimal?>? value = null, bool allowEmpty = true,
		Action<Validator>? additionalValidationMethod = null ) {
		var intValue = dataValue.CreateNewValue( v => (int?)v );
		return intValue.ToDurationControl(
			setup: setup,
			value: value is not null ? new SpecifiedValue<int?>( (int?)value.Value ) : null,
			allowEmpty: allowEmpty,
			additionalValidationMethod: validator => {
				dataValue.Value = intValue.Value;
				additionalValidationMethod?.Invoke( validator );
			} );
	}
}