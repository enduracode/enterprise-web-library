using NodaTime;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration;

internal class ModificationFormItemMethodWriter {
	private readonly ModificationField field;

	private readonly List<string> controls = new();
	private readonly Dictionary<string, Action<TextWriter, bool, IReadOnlyCollection<( string, Func<string, string> )>>> writersByControl = new();

	private string mainControl = "";
	private readonly Dictionary<string, Func<string, string>> inclusionInstructionsByExcludedControl = new( StringComparer.Ordinal );

	public ModificationFormItemMethodWriter( ModificationField field ) {
		this.field = field;

		// Some of these form item getters need modification methods to be executed to work properly. They return these methods, as out parameters. This allows
		// client code on a page to specify the order of modification methods, which is important because there may be both child modifications (like file
		// collections) and one-to-many modifications (like M+Vision references for an applicant) on the same page, and the main modification needs to execute
		// between these.
		addTextControls();
		addNumericControls();
		addCheckboxControls();
		addListControls();
		addDateAndTimeControls();

		if( field.TypeIs( typeof( string ) ) ) {
			addExclusion( "EmailAddressControl", fieldSource => $"suffix the {fieldSource} name with “Email”" );
			addExclusion( "TelephoneNumberControl", fieldSource => $"suffix the {fieldSource} name with “Phone”" );
			addExclusion( "UrlControl", fieldSource => $"suffix the {fieldSource} name with “Url”" );

			if( field.HasSuffix( "Email" ) ) {
				mainControl = "EmailAddressControl";
				removeExclusion( "EmailAddressControl" );
			}
			else if( field.HasSuffix( "Phone" ) ) {
				mainControl = "TelephoneNumberControl";
				removeExclusion( "TelephoneNumberControl" );
			}
			else if( field.HasSuffix( "Url" ) ) {
				mainControl = "UrlControl";
				removeExclusion( "UrlControl" );
			}
			else
				mainControl = "TextControl";
		}
		else if( field.TypeIs( typeof( decimal ) ) || field.TypeIs( typeof( decimal? ) ) )
			mainControl = "NumberControl";
		else if( field.TypeIs( typeof( LocalDate ) ) || field.TypeIs( typeof( LocalDate? ) ) )
			mainControl = "DateControl";
	}

	private void addTextControls() {
		if( field.TypeIs( typeof( string ) ) ) {
			addControl(
				"TextControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "TextControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string?",
				new CSharpParameter( "int?", "minLength", defaultValue: "null" ).ToCollection(),
				true,
				dv =>
					"{0}.ToTextControl( allowEmpty, setup: controlSetup, value: value, minLength: minLength, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			addControl(
				"EmailAddressControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "EmailAddressControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string?",
					[ ],
				true,
				dv =>
					"{0}.ToEmailAddressControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			addControl(
				"TelephoneNumberControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "TelephoneNumberControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string?",
					[ ],
				true,
				dv =>
					"{0}.ToTelephoneNumberControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			addControl(
				"UrlControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "UrlControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string?",
					[ ],
				true,
				dv => "{0}.ToUrlControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
					.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
		}

		if( field.TypeIs( typeof( string ) ) )
			addControl(
				"NumericTextControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "NumericTextControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string?",
				new CSharpParameter( "int?", "minLength", defaultValue: "null" ).ToCollection(),
				true,
				dv =>
					"{0}.ToNumericTextControl( allowEmpty, setup: controlSetup, value: value, minLength: minLength, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
		if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) )
			addControl(
				"TextControl",
					[ ],
				false,
				new CSharpParameter( "NumericTextControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				new CSharpParameter( field.NullableTypeName, "minValue", "null" ).ToCollection()
					.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) ),
				true,
				dv =>
					"{0}.ToTextControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ) );
		if( field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) )
			addControl(
				"TextControl",
					[ ],
				false,
				new CSharpParameter( "NumericTextControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				getAllowEmptyParameter( true )
					.ToCollection()
					.Append( new CSharpParameter( field.NullableTypeName, "minValue", "null" ) )
					.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) ),
				true,
				dv =>
					"{0}.ToTextControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ) );

		if( field.TypeIs( typeof( string ) ) )
			addControl(
				"Html",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "WysiwygHtmlEditorSetup?", "editorSetup", "null" ).ToCollection(),
				"string?",
					[ ],
				true,
				dv => "{0}.ToHtmlEditor( allowEmpty, setup: editorSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
					.FormatWith( dv, field.Size?.ToString() ?? "null" ),
				additionalSummarySentences:
					[
						"WARNING: Do not use this form-item getter unless you know exactly what you're doing.",
						"If you want to store HTML, it is almost always better to use an HTML block instead of just a string field.",
						"HTML blocks have special handling for intra-site URIs and may include additional features in the future.",
						"They also cause all of your HTML to be stored in one place, which is usually a good practice."
					] );
	}

	private void addNumericControls() {
		if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( short ) ) || field.TypeIs( typeof( byte ) ) ||
		    field.TypeIs( typeof( decimal ) ) )
			addControl(
				"NumberControl",
					[ ],
				false,
				new CSharpParameter( "NumberControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				new CSharpParameter( field.NullableTypeName, "minValue", "null" ).ToCollection()
					.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) )
					.Append( new CSharpParameter( field.NullableTypeName, "valueStep", "null" ) ),
				true,
				dv =>
					"{0}.ToNumberControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ),
				preFormItemStatements: getNumberControlValueStepStatements() );
		if( field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) || field.TypeIs( typeof( short? ) ) || field.TypeIs( typeof( byte? ) ) ||
		    field.TypeIs( typeof( decimal? ) ) )
			addControl(
				"NumberControl",
					[ ],
				false,
				new CSharpParameter( "NumberControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				getAllowEmptyParameter( true )
					.ToCollection()
					.Append( new CSharpParameter( field.NullableTypeName, "minValue", "null" ) )
					.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) )
					.Append( new CSharpParameter( field.NullableTypeName, "valueStep", "null" ) ),
				true,
				dv =>
					"{0}.ToNumberControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ),
				preFormItemStatements: getNumberControlValueStepStatements() );

		if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( short ) ) || field.TypeIs( typeof( byte ) ) ||
		    field.TypeIs( typeof( decimal ) ) )
			addControl(
				"ImpreciseNumberControl",
				new CSharpParameter( field.TypeName, "minValue" ).ToCollection().Append( new CSharpParameter( field.TypeName, "maxValue" ) ),
				false,
				new CSharpParameter( "ImpreciseNumberControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				field.NullableTypeName,
				new CSharpParameter( field.NullableTypeName, "valueStep", "null" ).ToCollection(),
				true,
				dv =>
					"{0}.ToImpreciseNumberControl( minValue, maxValue, setup: controlSetup, value: value, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ),
				preFormItemStatements: getNumberControlValueStepStatements() );

		if( field.TypeIs( typeof( int ) ) )
			writeHtmlAndFileFormItemGetters( "int?" );
		if( field.TypeIs( typeof( int? ) ) )
			writeHtmlAndFileFormItemGetters( "int?" );

		if( field.TypeIs( typeof( decimal ) ) )
			writeHtmlAndFileFormItemGetters( "decimal?" );
		if( field.TypeIs( typeof( decimal? ) ) )
			writeHtmlAndFileFormItemGetters( "decimal?" );
	}

	private string getNumberControlValueStepStatements() {
		if( ( !field.TypeIs( typeof( decimal ) ) && !field.TypeIs( typeof( decimal? ) ) ) || !field.NumericScale.HasValue )
			return "";
		var minStep = field.NumericScale.Value == 0
			              ? "1"
			              : ".{0}1m".FormatWith(
				              string.Concat( Enumerable.Repeat( '0', Math.Min( field.NumericScale.Value, (short)28 /* max scale for decimal */ ) - 1 ) ) );
		return StringTools.ConcatenateWithDelimiter(
			Environment.NewLine,
			"if( !valueStep.HasValue ) valueStep = {0};".FormatWith( minStep ),
			"else if( valueStep.Value % {0} != 0 ) throw new System.ApplicationException( \"The specified step is not a multiple of the field’s minimum step.\" );"
				.FormatWith( minStep ) );
	}

	private void addCheckboxControls() {
		if( !field.TypeIs( typeof( bool ) ) && !field.TypeIs( typeof( bool? ) ) && !field.TypeIs( typeof( decimal ) ) && !field.TypeIs( typeof( decimal? ) ) )
			return;

		var preFormItemStatements = field.TypeName == field.NullableTypeName
			                            ? "var nonNullableValue = value.HasValue ? new DataValue<{0}>( {1}.DataExists ) : {1}.CreateNewValue( v => v!.Value );"
				                            .FormatWith( field.TypeIs( typeof( decimal? ) ) ? "decimal" : "bool", getDataValueMember() )
			                            : "";
		string getDataValueExpression( string dv ) => field.TypeName == field.NullableTypeName ? "nonNullableValue" : dv;

		string getAdditionalValidationMethodExpression( string dv ) =>
			field.TypeName == field.NullableTypeName
				? "validator => {{ {0}.Value = nonNullableValue.Value; additionalValidationMethod?.Invoke( validator ); }}".FormatWith( dv )
				: "additionalValidationMethod";

		// checkboxes
		addControl(
			"Checkbox",
				[ ],
			true,
				[ new CSharpParameter( "CheckboxSetup?", "checkboxSetup", "null" ) ],
			field.NullableTypeName,
				[ ],
			true,
			dv => "{0}.ToCheckbox( label, setup: checkboxSetup, value: value, additionalValidationMethod: {1} )".FormatWith(
				getDataValueExpression( dv ),
				getAdditionalValidationMethodExpression( dv ) ),
			preFormItemStatements: preFormItemStatements );
		addControl(
			"FlowCheckbox",
				[ ],
			true,
				[ new CSharpParameter( "FlowCheckboxSetup?", "checkboxSetup", "null" ) ],
			field.NullableTypeName,
				[ ],
			true,
			dv => "{0}.ToFlowCheckbox( label, setup: checkboxSetup, value: value, additionalValidationMethod: {1} )".FormatWith(
				getDataValueExpression( dv ),
				getAdditionalValidationMethodExpression( dv ) ),
			preFormItemStatements: preFormItemStatements );

		// radio buttons
		addControl(
			"RadioButton",
			new CSharpParameter( "RadioButtonGroup", "group" ).ToCollection(),
			true,
				[ new CSharpParameter( "RadioButtonSetup?", "radioButtonSetup", "null" ) ],
			field.NullableTypeName,
				[ ],
			true,
			dv => "{0}.ToRadioButton( group, label, setup: radioButtonSetup, value: value, additionalValidationMethod: {1} )".FormatWith(
				getDataValueExpression( dv ),
				getAdditionalValidationMethodExpression( dv ) ),
			preFormItemStatements: preFormItemStatements );
		addControl(
			"FlowRadioButton",
			new CSharpParameter( "RadioButtonGroup", "group" ).ToCollection(),
			true,
				[ new CSharpParameter( "FlowRadioButtonSetup?", "radioButtonSetup", "null" ) ],
			field.NullableTypeName,
				[ ],
			true,
			dv => "{0}.ToFlowRadioButton( group, label, setup: radioButtonSetup, value: value, additionalValidationMethod: {1} )".FormatWith(
				getDataValueExpression( dv ),
				getAdditionalValidationMethodExpression( dv ) ),
			preFormItemStatements: preFormItemStatements );
	}

	private void writeHtmlAndFileFormItemGetters( string valueParamTypeName ) {
		addControl(
			"Html",
			new CSharpParameter( "out HtmlBlockEditorModification", "mod" ).ToCollection(),
			false,
			new CSharpParameter( "HtmlBlockEditorSetup?", "editorSetup", "null" ).ToCollection(),
			"SpecifiedValue<{0}>?".FormatWith( valueParamTypeName ),
				[ ],
			false,
			dv => "new HtmlBlockEditor( (int?)( value != null ? value.Value : {0}.Value ), id => {0}.Value = id, out mod, setup: editorSetup )".FormatWith( dv ) );
		addControl(
			"File",
			new CSharpParameter( "out System.Action", "modificationMethod" ).ToCollection(),
			false,
			new CSharpParameter( "BlobFileManagerSetup?", "managerSetup", "null" ).ToCollection(),
			"SpecifiedValue<{0}>?".FormatWith( valueParamTypeName ),
			new CSharpParameter( "bool", "requireUploadIfNoFile", "false" ).ToCollection(),
			false,
			dv =>
				"new BlobFileManager( (int?)( value != null ? value.Value : {0}.Value ), requireUploadIfNoFile, id => {0}.Value = id, out modificationMethod, setup: managerSetup )"
					.FormatWith( dv ) );
	}

	private void addListControls() {
		if( field.TypeIs( typeof( bool ) ) || field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( decimal ) ) )
			addControl(
				"RadioList",
				new CSharpParameter( "RadioListSetup<{0}>".FormatWith( field.NullableTypeName ), "controlSetup" ).ToCollection(),
				false,
					[ ],
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
					[ ],
				true,
				dv => "{0}.ToRadioList( controlSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
		if( field.TypeIs( typeof( bool? ) ) || field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) || field.TypeIs( typeof( string ) ) ||
		    field.TypeIs( typeof( decimal? ) ) )
			addControl(
				"RadioList",
				new CSharpParameter( "RadioListSetup<{0}>".FormatWith( field.TypeName ), "controlSetup" ).ToCollection(),
				false,
				new CSharpParameter( "string", "defaultValueItemLabel", defaultValue: field.TypeIs( typeof( string ) ) ? "\"\"" : "\"None\"" ).ToCollection(),
				field.TypeIs( typeof( string ) ) ? field.NullableTypeName + "?" : "SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
					[ ],
				true,
				dv =>
					"{0}.ToRadioList( controlSetup, defaultValueItemLabel: defaultValueItemLabel, value: value, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ) );

		if( field.TypeIs( typeof( bool ) ) || field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( decimal ) ) )
			addControl(
				"DropDown",
				new CSharpParameter( "DropDownSetup<{0}>".FormatWith( field.NullableTypeName ), "controlSetup" ).ToCollection(),
				false,
					[ ],
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
					[ ],
				true,
				dv => "{0}.ToDropDown( controlSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
		if( field.TypeIs( typeof( bool? ) ) || field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) || field.TypeIs( typeof( string ) ) ||
		    field.TypeIs( typeof( decimal? ) ) )
			addControl(
				"DropDown",
				new CSharpParameter( "DropDownSetup<{0}>".FormatWith( field.TypeName ), "controlSetup" ).ToCollection()
					.Concat( field.TypeIs( typeof( string ) ) ? [ ] : new CSharpParameter( "string", "defaultValueItemLabel" ).ToCollection() ),
				false,
				( field.TypeIs( typeof( string ) ) ? new CSharpParameter( "string", "defaultValueItemLabel", defaultValue: "\"\"" ).ToCollection() : [ ] ).Append(
					new CSharpParameter( "bool", "placeholderIsValid", field.TypeIs( typeof( string ) ) ? "false" : "true" ) ),
				field.TypeIs( typeof( string ) ) ? field.NullableTypeName + "?" : "SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
					[ ],
				true,
				dv =>
					"{0}.ToDropDown( controlSetup, {1}defaultValueItemLabel, placeholderIsValid: placeholderIsValid, value: value, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv, field.TypeIs( typeof( string ) ) ? "defaultValueItemLabel: " : "" ) );

		if( field.EnumerableElementTypeName.Any() )
			addControl(
				"CheckboxList",
				new CSharpParameter( "CheckboxListSetup<{0}>".FormatWith( field.EnumerableElementTypeName ), "checkboxListSetup" ).ToCollection(),
				false,
					[ ],
				field.NullableTypeName,
					[ ],
				true,
				dv => "{0}.ToCheckboxList( checkboxListSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
	}

	private void addDateAndTimeControls() {
		if( field.TypeIs( typeof( LocalDate ) ) || field.TypeIs( typeof( DateTime ) ) )
			addControl(
				"DateControl",
					[ ],
				false,
				new CSharpParameter( "DateControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				new CSharpParameter( "LocalDate?", "minValue", "null" ).ToCollection().Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
				true,
				dv =>
					"{0}.ToDateControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ) );
		if( field.TypeIs( typeof( LocalDate? ) ) || field.TypeIs( typeof( DateTime? ) ) )
			addControl(
				"DateControl",
					[ ],
				false,
				new CSharpParameter( "DateControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				getAllowEmptyParameter( true )
					.ToCollection()
					.Append( new CSharpParameter( "LocalDate?", "minValue", "null" ) )
					.Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
				true,
				dv =>
					"{0}.ToDateControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ) );

		if( field.TypeIs( typeof( TimeSpan ) ) || field.TypeIs( typeof( TimeSpan? ) ) )
			addControl(
				"TimeControl",
					[ ],
				false,
				new CSharpParameter( "TimeControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				( field.TypeName == field.NullableTypeName ? getAllowEmptyParameter( true ).ToCollection() : [ ] )
				.Append( new CSharpParameter( "LocalTime?", "minValue", "null" ) )
				.Append( new CSharpParameter( "LocalTime?", "maxValue", "null" ) )
				.Append( new CSharpParameter( "int", "minuteInterval", "15" ) ),
				true,
				dv => field.TypeName == field.NullableTypeName
					      ? "{0}.ToTimeControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, minuteInterval: minuteInterval, additionalValidationMethod: additionalValidationMethod )"
						      .FormatWith( dv )
					      : "{0}.ToTimeControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, minuteInterval: minuteInterval, additionalValidationMethod: additionalValidationMethod )"
						      .FormatWith( dv ) );

		if( field.TypeIs( typeof( DateTime ) ) )
			addControl(
				"DateAndTimeControl",
					[ ],
				false,
				new CSharpParameter( "DateAndTimeControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				new CSharpParameter( "LocalDate?", "minValue", "null" ).ToCollection().Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
				true,
				dv =>
					"{0}.ToDateAndTimeControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ) );
		if( field.TypeIs( typeof( DateTime? ) ) )
			addControl(
				"DateAndTimeControl",
					[ ],
				false,
				new CSharpParameter( "DateAndTimeControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				getAllowEmptyParameter( true )
					.ToCollection()
					.Append( new CSharpParameter( "LocalDate?", "minValue", "null" ) )
					.Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
				true,
				dv =>
					"{0}.ToDateAndTimeControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv ) );

		if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( decimal ) ) || field.TypeIs( typeof( decimal? ) ) )
			addControl(
				"DurationControl",
					[ ],
				false,
				new CSharpParameter( "DurationControlSetup?", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				field.TypeName == field.NullableTypeName ? getAllowEmptyParameter( true ).ToCollection() : [ ],
				true,
				dv => field.TypeName == field.NullableTypeName
					      ? "{0}.ToDurationControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, additionalValidationMethod: additionalValidationMethod )"
						      .FormatWith( dv )
					      : "{0}.ToDurationControl( setup: controlSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
	}

	private void addControl(
		string control, IEnumerable<CSharpParameter> requiredParams, bool controlIsLabeled, IEnumerable<CSharpParameter> preValueOptionalParams,
		string valueParamTypeName, IEnumerable<CSharpParameter> postValueOptionalParams, bool includeAdditionalValidationMethodParam,
		Func<string, string> formControlExpressionGetter, string preFormItemStatements = "", string postFormItemStatements = "",
		IEnumerable<string>? additionalSummarySentences = null ) {
		controls.Add( control );
		writersByControl.Add(
			control,
			( writer, omitControlFromMethodName, excludedControls ) => {
				var parameters = new List<CSharpParameter>();
				parameters.AddRange( requiredParams );
				parameters.Add( new CSharpParameter( "FormItemSetup?", "formItemSetup", "null" ) );
				parameters.Add( new CSharpParameter( "IReadOnlyCollection<PhrasingComponent>?", "label", "null" ) );
				if( controlIsLabeled )
					parameters.Add( new CSharpParameter( "IReadOnlyCollection<PhrasingComponent>?", "formItemLabel", "null" ) );
				parameters.AddRange( preValueOptionalParams );
				parameters.Add( new CSharpParameter( valueParamTypeName, "value", "null" ) );
				parameters.AddRange( postValueOptionalParams );
				if( includeAdditionalValidationMethodParam )
					parameters.Add(
						new CSharpParameter(
							"System.Action<Validator>?",
							"additionalValidationMethod",
							defaultValue: "null",
							description: "A method that takes the form control’s validator and performs additional validation." ) );

				CodeGenerationStatics.AddSummaryDocComment(
					writer,
					getFormItemGetterSummary(
						control,
						( additionalSummarySentences ?? [ ] ).Concat(
							excludedControls.Any()
								? $"<para>{StringTools.ConcatenateWithDelimiter( "<br/>", excludedControls.Select( i => $"If you need a {i.Item1} form item, {i.Item2( field.Source )}." ) )}</para>"
									.ToCollection()
								: [ ] ) ) );
				foreach( var i in parameters )
					CodeGenerationStatics.AddParamDocComment( writer, i.Name, i.Description );
				var name = EwlStatics.GetCSharpIdentifier( $"Get{field.PascalCasedName}{( omitControlFromMethodName ? "" : control )}FormItem" );
				writer.WriteLine( $"public FormItem {name}( {parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection()} ) {{" );
				writer.WriteLine( "label = label ?? \"{0}\".ToComponents();".FormatWith( getDefaultLabel() ) );
				writer.WriteLine(
					StringTools.ConcatenateWithDelimiter(
						Environment.NewLine,
						preFormItemStatements,
						"var formItem = {0}.ToFormItem( setup: formItemSetup, label: {1} );".FormatWith(
							formControlExpressionGetter( getDataValueMember() ),
							controlIsLabeled ? "formItemLabel" : "label" ),
						postFormItemStatements,
						"return formItem;" ) );
				writer.WriteLine( "}" );
			} );
	}

	private CSharpParameter getAllowEmptyParameter( bool isOptional ) => new( "bool", "allowEmpty", isOptional ? "true" : "" );

	private void addExclusion( string control, Func<string, string> inclusionInstructions ) {
		inclusionInstructionsByExcludedControl.Add( control, inclusionInstructions );
	}

	private void removeExclusion( string control ) {
		inclusionInstructionsByExcludedControl.Remove( control );
	}

	internal void WriteFormItemGetters( TextWriter writer ) {
		if( mainControl.Length > 0 && inclusionInstructionsByExcludedControl.ContainsKey( mainControl ) )
			throw new ArgumentException( $"A control specified as {nameof(mainControl)} cannot also be excluded." );

		var excludedControls = inclusionInstructionsByExcludedControl.Select( i => ( i.Key, i.Value ) ).OrderBy( i => i.Key ).Materialize();

		if( mainControl.Length > 0 )
			writersByControl[ mainControl ]( writer, true, excludedControls );

		foreach( var control in controls ) {
			if( string.Equals( control, mainControl, StringComparison.Ordinal ) )
				continue;
			if( inclusionInstructionsByExcludedControl.ContainsKey( control ) )
				continue;
			writersByControl[ control ]( writer, false, excludedControls );
		}

		writeComponentGetter( writer );
	}

	private void writeComponentGetter( TextWriter writer ) {
		CodeGenerationStatics.AddSummaryDocComment( writer, getFormItemGetterSummary( "", [ ] ) );

		var parameters = new List<CSharpParameter>();
		parameters.Add( new CSharpParameter( "System.Func<{0},IReadOnlyCollection<FlowComponent>>".FormatWith( field.NullableTypeName ), "contentGetter" ) );
		parameters.Add( new CSharpParameter( "FormItemSetup?", "setup", "null" ) );
		parameters.Add( new CSharpParameter( "IReadOnlyCollection<PhrasingComponent>?", "label", "null" ) );
		parameters.Add(
			new CSharpParameter(
				field.TypeIs( typeof( string ) ) ? field.NullableTypeName + "?" :
				field.EnumerableElementTypeName.Length > 0 ? field.NullableTypeName : "SpecifiedValue<{0}>?".FormatWith( field.NullableTypeName ),
				"value",
				"null" ) );
		parameters.Add( new CSharpParameter( "System.Func<System.Action<{0}>,EwfValidation>?".FormatWith( field.TypeName ), "validationGetter", "null" ) );

		writer.WriteLine(
			"public FormItem " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + "ComponentFormItem" ) + "( " +
			parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() + " ) {" );
		writer.WriteLine( "label = label ?? \"{0}\".ToComponents();".FormatWith( getDefaultLabel() ) );
		writer.WriteLine(
			"return {0}.ToFormItem( setup: setup, label: label, validation: {1} );".FormatWith(
				"contentGetter( {0} )".FormatWith(
					field.TypeIs( typeof( string ) ) || field.EnumerableElementTypeName.Length > 0
						? $"value ?? {getDataValueMember()}.Value"
						: $"value is not null ? value.Value : {getDataValueMember()}.Value" ),
				"validationGetter?.Invoke( v => {0} = v )".FormatWith( EwlStatics.GetCSharpIdentifier( field.PascalCasedName ) ) ) );
		writer.WriteLine( "}" );
	}

	private string getFormItemGetterSummary( string control, IEnumerable<string> additionalSentences ) {
		var sentences = new[]
			{
				"Creates a " + field.Name + control.PrependDelimiter( " " ) + " form item, which includes a label, a page component, and a validation.",
				"The default label is “{0}”.".FormatWith( getDefaultLabel() ),
				control.Length > 0
					? ""
					: "This method creates the form item from components; if you instead want to create it with a type of form control for which there is no automatically generated method, write your own custom method within the modification class so you can access the private data-value object.",
				"You almost certainly should not call this method from a deferred block of code since this could cause validations to be added to the data modification in the wrong order."
			};
		return StringTools.ConcatenateWithDelimiter( " ", sentences.Concat( additionalSentences ) );
	}

	private string getDefaultLabel() {
		var result = field.PascalCasedName.CamelToEnglish().ToLowerInvariant().Capitalize();
		if( result.EndsWith( " id", StringComparison.Ordinal ) )
			result = result[ ..^3 ];
		return result;
	}

	private string getDataValueMember() => $"this.{EwlStatics.GetCSharpIdentifier( field.CamelCasedName )}";
}