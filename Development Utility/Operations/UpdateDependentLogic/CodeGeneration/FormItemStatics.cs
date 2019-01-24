using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal static class FormItemStatics {
		internal static void WriteFormItemGetters( TextWriter writer, ModificationField field ) {
			// Some of these form item getters need modification methods to be executed to work properly. They return these methods, as out parameters, instead of
			// just adding them to the data modification. This allows client code on a page to specify the order of modification methods, which is important because
			// there may be both child modifications (like file collections) and one-to-many modifications (like M+Vision references for an applicant) on the same
			// page, and the main modification needs to execute between these.
			writeStringFormItemGetters( writer, field );
			writer.WriteLine( "#pragma warning disable CS0618" ); // remove when EwfCheckBox and BlockCheckBox are gone
			writeNumericFormItemGetters( writer, field );
			writeCheckboxFormItemGetters( writer, field );
			writeBoolFormItemGetters( writer, field );
			writer.WriteLine( "#pragma warning restore CS0618" ); // remove when EwfCheckBox and BlockCheckBox are gone
			writeListFormItemGetters( writer, field );
			writeDateFormItemGetters( writer, field );
			writeGuidFormItemGetters( writer, field );

			writeGenericGetter( writer, field );
			writeGenericGetterWithoutValueParams( writer, field, null );
			writeGenericGetterWithoutValueParams( writer, field, false );
			writeGenericGetterWithoutValueParams( writer, field, true );
			writeGenericGetterWithValueParams( writer, field, null );
			writeGenericGetterWithValueParams( writer, field, false );
			writeGenericGetterWithValueParams( writer, field, true );
		}

		private static void writeStringFormItemGetters( TextWriter writer, ModificationField field ) {
			if( !field.TypeIs( typeof( string ) ) )
				return;

			writeFormItemGetter(
				writer,
				field,
				"TextControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "TextControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string",
				Enumerable.Empty<CSharpParameter>(),
				true,
				dv =>
					"{0}.ToTextControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			writeFormItemGetter(
				writer,
				field,
				"EmailAddressControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "EmailAddressControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string",
				Enumerable.Empty<CSharpParameter>(),
				true,
				dv =>
					"{0}.ToEmailAddressControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			writeFormItemGetter(
				writer,
				field,
				"TelephoneNumberControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "TelephoneNumberControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string",
				Enumerable.Empty<CSharpParameter>(),
				true,
				dv =>
					"{0}.ToTelephoneNumberControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			writeFormItemGetter(
				writer,
				field,
				"UrlControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "UrlControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string",
				Enumerable.Empty<CSharpParameter>(),
				true,
				dv =>
					"{0}.ToUrlControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			writeFormItemGetter(
				writer,
				field,
				"NumericTextControl",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "NumericTextControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
				"string",
				new CSharpParameter( "int?", "minLength", defaultValue: "null" ).ToCollection(),
				true,
				dv =>
					"{0}.ToNumericTextControl( allowEmpty, setup: controlSetup, value: value, minLength: minLength, maxLength: {1}, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			writeFormItemGetters(
				writer,
				field,
				"SelectList<string>",
				"RadioList",
				"string",
				"\"\"",
				new[] { new CSharpParameter( "IEnumerable<SelectListItem<string>>", "items" ) },
				new CSharpParameter[ 0 ],
				new[]
					{
						new CSharpParameter( "bool", "useHorizontalLayout", "false" ), new CSharpParameter( "string", "defaultValueItemLabel", "\"\"" ),
						new CSharpParameter( "bool", "disableSingleButtonDetection", "false" ), new CSharpParameter( "FormAction", "action", "null" ),
						new CSharpParameter( "bool", "autoPostBack", "false" )
					},
				new CSharpParameter[ 0 ],
				"SelectList.CreateRadioList( items, v, useHorizontalLayout: useHorizontalLayout, defaultValueItemLabel: defaultValueItemLabel, disableSingleButtonDetection: disableSingleButtonDetection, action: action, autoPostBack: autoPostBack )",
				"control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator )",
				"" );
			writeFormItemGetters(
				writer,
				field,
				"SelectList<string>",
				"DropDown",
				"string",
				"\"\"",
				new[] { new CSharpParameter( "IEnumerable<SelectListItem<string>>", "items" ) },
				new CSharpParameter[ 0 ],
				new[]
					{
						new CSharpParameter( "Unit?", "width", "null" ), new CSharpParameter( "string", "defaultValueItemLabel", "\"\"" ),
						new CSharpParameter( "bool", "placeholderIsValid", "false" ), new CSharpParameter( "string", "placeholderText", "\"Please select\"" ),
						new CSharpParameter( "FormAction", "action", "null" ), new CSharpParameter( "bool", "autoPostBack", "false" )
					},
				new CSharpParameter[ 0 ],
				"SelectList.CreateDropDown( items, v, width: width, defaultValueItemLabel: defaultValueItemLabel, placeholderIsValid: placeholderIsValid, placeholderText: placeholderText, action: action, autoPostBack: autoPostBack )",
				"control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator )",
				"" );
			writeFormItemGetter(
				writer,
				field,
				"Html",
				getAllowEmptyParameter( false ).ToCollection(),
				false,
				new CSharpParameter( "WysiwygHtmlEditorSetup", "editorSetup", "null" ).ToCollection(),
				"string",
				new CSharpParameter[ 0 ],
				true,
				dv =>
					"{0}.ToHtmlEditor( allowEmpty, setup: editorSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ),
				additionalSummarySentences: new[]
					{
						"WARNING: Do not use this form-item getter unless you know exactly what you're doing.",
						"If you want to store HTML, it is almost always better to use an HTML block instead of just a string field.",
						"HTML blocks have special handling for intra-site URIs and may include additional features in the future.",
						"They also cause all of your HTML to be stored in one place, which is usually a good practice."
					} );
		}

		private static void writeNumericFormItemGetters( TextWriter writer, ModificationField field ) {
			if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) )
				writeFormItemGetter(
					writer,
					field,
					"TextControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "NumericTextControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					new CSharpParameter( field.NullableTypeName, "minValue", "null" ).ToCollection()
						.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) ),
					true,
					dv =>
						"{0}.ToTextControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ) );
			if( field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) )
				writeFormItemGetter(
					writer,
					field,
					"TextControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "NumericTextControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					getAllowEmptyParameter( true )
						.ToCollection()
						.Append( new CSharpParameter( field.NullableTypeName, "minValue", "null" ) )
						.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) ),
					true,
					dv =>
						"{0}.ToTextControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ) );

			if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( short ) ) || field.TypeIs( typeof( byte ) ) ||
			    field.TypeIs( typeof( decimal ) ) )
				writeFormItemGetter(
					writer,
					field,
					"NumberControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "NumberControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					new CSharpParameter( field.NullableTypeName, "minValue", "null" ).ToCollection()
						.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) )
						.Append( new CSharpParameter( field.NullableTypeName, "valueStep", "null" ) ),
					true,
					dv =>
						"{0}.ToNumberControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ),
					preFormItemStatements: getNumberControlValueStepStatements( field ) );
			if( field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) || field.TypeIs( typeof( short? ) ) || field.TypeIs( typeof( byte? ) ) ||
			    field.TypeIs( typeof( decimal? ) ) )
				writeFormItemGetter(
					writer,
					field,
					"NumberControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "NumberControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					getAllowEmptyParameter( true )
						.ToCollection()
						.Append( new CSharpParameter( field.NullableTypeName, "minValue", "null" ) )
						.Append( new CSharpParameter( field.NullableTypeName, "maxValue", "null" ) )
						.Append( new CSharpParameter( field.NullableTypeName, "valueStep", "null" ) ),
					true,
					dv =>
						"{0}.ToNumberControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ),
					preFormItemStatements: getNumberControlValueStepStatements( field ) );

			if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( short ) ) || field.TypeIs( typeof( byte ) ) ||
			    field.TypeIs( typeof( decimal ) ) )
				writeFormItemGetter(
					writer,
					field,
					"ImpreciseNumberControl",
					new CSharpParameter( field.TypeName, "minValue" ).ToCollection().Append( new CSharpParameter( field.TypeName, "maxValue" ) ),
					false,
					new CSharpParameter( "ImpreciseNumberControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					field.NullableTypeName,
					new CSharpParameter( field.NullableTypeName, "valueStep", "null" ).ToCollection(),
					true,
					dv =>
						"{0}.ToImpreciseNumberControl( minValue, maxValue, setup: controlSetup, value: value, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ),
					preFormItemStatements: getNumberControlValueStepStatements( field ) );

			if( field.TypeIs( typeof( int ) ) ) {
				writeNumberAsSelectListFormItemGetters( writer, field );
				writeDurationFormItemGetter( writer, field );
				writeHtmlAndFileFormItemGetters( writer, field, "int?" );
				writeFileCollectionFormItemGetters( writer, field, "int" );
			}
			if( field.TypeIs( typeof( int? ) ) ) {
				writeNumberAsSelectListFormItemGetters( writer, field );
				writeDurationFormItemGetter( writer, field );
				writeHtmlAndFileFormItemGetters( writer, field, "int?" );
			}

			if( field.TypeIs( typeof( short ) ) )
				writeNumberAsSelectListFormItemGetters( writer, field );
			if( field.TypeIs( typeof( short? ) ) )
				writeNumberAsSelectListFormItemGetters( writer, field );

			if( field.TypeIs( typeof( byte ) ) )
				writeNumberAsSelectListFormItemGetters( writer, field );
			if( field.TypeIs( typeof( byte? ) ) )
				writeNumberAsSelectListFormItemGetters( writer, field );

			if( field.TypeIs( typeof( decimal ) ) ) {
				writeNumberAsSelectListFormItemGetters( writer, field );
				writeLegacyCheckBoxFormItemGetters( writer, field, "decimal" );
				writeDurationFormItemGetter( writer, field );
				writeHtmlAndFileFormItemGetters( writer, field, "decimal?" );
				writeFileCollectionFormItemGetters( writer, field, "decimal" );
			}
			if( field.TypeIs( typeof( decimal? ) ) ) {
				writeNumberAsSelectListFormItemGetters( writer, field );
				writeDurationFormItemGetter( writer, field );
				writeHtmlAndFileFormItemGetters( writer, field, "decimal?" );
			}
		}

		private static string getNumberControlValueStepStatements( ModificationField field ) {
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

		private static void writeCheckboxFormItemGetters( TextWriter writer, ModificationField field ) {
			if( !field.TypeIs( typeof( bool ) ) && !field.TypeIs( typeof( bool? ) ) && !field.TypeIs( typeof( decimal ) ) && !field.TypeIs( typeof( decimal? ) ) )
				return;

			var preFormItemStatements = field.TypeName == field.NullableTypeName
				                            ? "var nonNullableValue = new DataValue<{0}>();".FormatWith( field.TypeIs( typeof( decimal? ) ) ? "decimal" : "bool" )
				                            : "";
			string getDataValueExpression( string dv ) => field.TypeName == field.NullableTypeName ? "nonNullableValue" : dv;
			string getValueExpression( string dv ) => field.TypeName == field.NullableTypeName ? "value ?? {0}.Value.Value".FormatWith( dv ) : "value";

			string getAdditionalValidationMethodExpression( string dv ) =>
				field.TypeName == field.NullableTypeName
					? "validator => {{ {0}.Value = nonNullableValue.Value; additionalValidationMethod?.Invoke( validator ); }}".FormatWith( dv )
					: "additionalValidationMethod";

			// checkboxes
			writeFormItemGetter(
				writer,
				field,
				"Checkbox",
				new CSharpParameter[ 0 ],
				true,
				new[] { new CSharpParameter( "CheckboxSetup", "checkboxSetup", "null" ) },
				field.NullableTypeName,
				new CSharpParameter[ 0 ],
				true,
				dv =>
					"{0}.ToCheckbox( label, setup: checkboxSetup, value: {1}, additionalValidationMethod: {2} ).ToFormItem( setup: formItemSetup, label: formItemLabel )"
						.FormatWith( getDataValueExpression( dv ), getValueExpression( dv ), getAdditionalValidationMethodExpression( dv ) ),
				preFormItemStatements: preFormItemStatements );
			writeFormItemGetter(
				writer,
				field,
				"FlowCheckbox",
				new CSharpParameter[ 0 ],
				true,
				new[] { new CSharpParameter( "FlowCheckboxSetup", "checkboxSetup", "null" ) },
				field.NullableTypeName,
				new CSharpParameter[ 0 ],
				true,
				dv =>
					"{0}.ToFlowCheckbox( label, setup: checkboxSetup, value: {1}, additionalValidationMethod: {2} ).ToFormItem( setup: formItemSetup, label: formItemLabel )"
						.FormatWith( getDataValueExpression( dv ), getValueExpression( dv ), getAdditionalValidationMethodExpression( dv ) ),
				preFormItemStatements: preFormItemStatements );

			// radio buttons
			writeFormItemGetter(
				writer,
				field,
				"RadioButton",
				new CSharpParameter( "RadioButtonGroup", "group" ).ToCollection(),
				true,
				new[] { new CSharpParameter( "RadioButtonSetup", "radioButtonSetup", "null" ) },
				field.NullableTypeName,
				new CSharpParameter[ 0 ],
				true,
				dv =>
					"{0}.ToRadioButton( group, label, setup: radioButtonSetup, value: {1}, additionalValidationMethod: {2} ).ToFormItem( setup: formItemSetup, label: formItemLabel )"
						.FormatWith( getDataValueExpression( dv ), getValueExpression( dv ), getAdditionalValidationMethodExpression( dv ) ),
				preFormItemStatements: preFormItemStatements );
			writeFormItemGetter(
				writer,
				field,
				"FlowRadioButton",
				new CSharpParameter( "RadioButtonGroup", "group" ).ToCollection(),
				true,
				new[] { new CSharpParameter( "FlowRadioButtonSetup", "radioButtonSetup", "null" ) },
				field.NullableTypeName,
				new CSharpParameter[ 0 ],
				true,
				dv =>
					"{0}.ToFlowRadioButton( group, label, setup: radioButtonSetup, value: {1}, additionalValidationMethod: {2} ).ToFormItem( setup: formItemSetup, label: formItemLabel )"
						.FormatWith( getDataValueExpression( dv ), getValueExpression( dv ), getAdditionalValidationMethodExpression( dv ) ),
				preFormItemStatements: preFormItemStatements );
		}

		private static void writeFileCollectionFormItemGetters( TextWriter writer, ModificationField field, string valueParamTypeName ) {
			writeFormItemGetters(
				writer,
				field,
				"BlobFileCollectionManager",
				"FileCollection",
				valueParamTypeName,
				"-1",
				new CSharpParameter( "bool", "sortByName" ).ToCollection(),
				new CSharpParameter[ 0 ],
				new CSharpParameter[ 0 ],
				new CSharpParameter[ 0 ],
				"{ var control = new BlobFileCollectionManager( sortByName: sortByName ); control.LoadData( (int)v ); return control; }",
				"",
				"" );
		}

		private static void writeDurationFormItemGetter( TextWriter writer, ModificationField field ) =>
			writeFormItemGetter(
				writer,
				field,
				"DurationControl",
				Enumerable.Empty<CSharpParameter>(),
				false,
				new CSharpParameter( "DurationControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
				"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
				field.TypeName == field.NullableTypeName ? getAllowEmptyParameter( true ).ToCollection() : Enumerable.Empty<CSharpParameter>(),
				true,
				dv => field.TypeName == field.NullableTypeName
					      ? "{0}.ToDurationControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						      .FormatWith( dv )
					      : "{0}.ToDurationControl( setup: controlSetup, value: value, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
						      .FormatWith( dv ) );

		private static void writeHtmlAndFileFormItemGetters( TextWriter writer, ModificationField field, string valueParamTypeName ) {
			writeFormItemGetter(
				writer,
				field,
				"Html",
				new CSharpParameter( "out HtmlBlockEditorModification", "mod" ).ToCollection(),
				false,
				new CSharpParameter( "HtmlBlockEditorSetup", "editorSetup", "null" ).ToCollection(),
				"SpecifiedValue<{0}>".FormatWith( valueParamTypeName ),
				new CSharpParameter[ 0 ],
				false,
				dv =>
					"new HtmlBlockEditor( (int?)( value != null ? value.Value : {0}.Value ), id => {0}.Value = id, out m, setup: editorSetup ).ToFormItem( setup: formItemSetup, label: label )"
						.FormatWith( dv ),
				preFormItemStatements: "HtmlBlockEditorModification m = null;",
				postFormItemStatements: "mod = m;" );
			writeFormItemGetters(
				writer,
				field,
				"BlobFileManager",
				"File",
				valueParamTypeName,
				"null",
				new CSharpParameter( "out System.Action", "modificationMethod" ).ToCollection(),
				new CSharpParameter[ 0 ],
				new CSharpParameter[ 0 ],
				new CSharpParameter( "bool", "requireUploadIfNoFile", "false" ).ToCollection(),
				"{ var control = new BlobFileManager( (int?)v ); mm = () => " + field.Name + " = control.ModifyData(); return control; }",
				"control.ValidateFormValues( validator, subject, requireUploadIfNoFile )",
				"",
				preFormItemGetterStatements: "System.Action mm = null;",
				postFormItemGetterStatements: "modificationMethod = mm;" );
		}

		private static void writeBoolFormItemGetters( TextWriter writer, ModificationField field ) {
			if( field.TypeIs( typeof( bool ) ) ) {
				writeLegacyCheckBoxFormItemGetters( writer, field, "bool" );
				writeNumberAsSelectListFormItemGetters( writer, field );
			}
			if( field.TypeIs( typeof( bool? ) ) )
				writeNumberAsSelectListFormItemGetters( writer, field );
		}

		private static void writeLegacyCheckBoxFormItemGetters( TextWriter writer, ModificationField field, string valueParamTypeName ) {
			writeFormItemGetter(
				writer,
				field,
				"BlockCheckbox",
				new CSharpParameter[ 0 ],
				true,
				new[] { new CSharpParameter( "BlockCheckBoxSetup", "checkboxSetup", "null" ) },
				valueParamTypeName + "?",
				new CSharpParameter[ 0 ],
				true,
				dv =>
					"{0}.ToBlockCheckbox( label, setup: checkboxSetup, value: value, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: formItemLabel )"
						.FormatWith( dv ) );
		}

		private static void writeListFormItemGetters( TextWriter writer, ModificationField field ) {
			if( field.EnumerableElementTypeName.Any() )
				writeFormItemGetter(
					writer,
					field,
					"CheckboxList",
					new CSharpParameter( "CheckboxListSetup<{0}>".FormatWith( field.EnumerableElementTypeName ), "checkboxListSetup" ).ToCollection(),
					false,
					Enumerable.Empty<CSharpParameter>(),
					field.TypeName,
					Enumerable.Empty<CSharpParameter>(),
					true,
					dv =>
						"{0}.ToCheckboxList( checkboxListSetup, value: value, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ) );
		}

		private static void writeDateFormItemGetters( TextWriter writer, ModificationField field ) {
			if( field.TypeIs( typeof( DateTime ) ) )
				writeFormItemGetter(
					writer,
					field,
					"DateControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "DateControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					new CSharpParameter( "LocalDate?", "minValue", "null" ).ToCollection().Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
					true,
					dv =>
						"{0}.ToDateControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ) );
			if( field.TypeIs( typeof( DateTime? ) ) )
				writeFormItemGetter(
					writer,
					field,
					"DateControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "DateControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					getAllowEmptyParameter( true )
						.ToCollection()
						.Append( new CSharpParameter( "LocalDate?", "minValue", "null" ) )
						.Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
					true,
					dv =>
						"{0}.ToDateControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod ).ToFormItem( setup: formItemSetup, label: label )"
							.FormatWith( dv ) );
		}

		private static void writeGuidFormItemGetters( TextWriter writer, ModificationField field ) {
			if( !field.TypeIs( typeof( Guid ) ) )
				return;
			writeNumberAsSelectListFormItemGetters( writer, field );
		}

		private static void writeNumberAsSelectListFormItemGetters( TextWriter writer, ModificationField field ) {
			var nonNullableField = field.TypeName != field.NullableTypeName;
			writeFormItemGetters(
				writer,
				field,
				"SelectList<" + field.NullableTypeName + ">",
				"RadioList",
				field.NullableTypeName,
				"null",
				new[] { new CSharpParameter( "IEnumerable<SelectListItem<" + field.NullableTypeName + ">>", "items" ) },
				new CSharpParameter[ 0 ],
				new CSharpParameter( "bool", "useHorizontalLayout", "false" ).ToCollection()
					.Concat( nonNullableField ? new CSharpParameter[ 0 ] : new CSharpParameter( "string", "defaultValueItemLabel", "\"None\"" ).ToCollection() )
					.Concat(
						new[]
							{
								new CSharpParameter( "bool", "disableSingleButtonDetection", "false" ), new CSharpParameter( "FormAction", "action", "null" ),
								new CSharpParameter( "bool", "autoPostBack", "false" ),
								new CSharpParameter( "PageModificationValue<{0}>".FormatWith( field.NullableTypeName ), "itemIdPageModificationValue", "null" ),
								new CSharpParameter(
									"IEnumerable<ListItemMatchPageModificationSetup<{0}>>".FormatWith( field.NullableTypeName ),
									"itemMatchPageModificationSetups",
									"null" )
							} ),
				new CSharpParameter[ 0 ],
				"SelectList.CreateRadioList( items, v, useHorizontalLayout: useHorizontalLayout, defaultValueItemLabel: " +
				( nonNullableField ? "\"\"" : "defaultValueItemLabel" ) +
				", disableSingleButtonDetection: disableSingleButtonDetection, action: action, autoPostBack: autoPostBack, itemIdPageModificationValue: itemIdPageModificationValue, itemMatchPageModificationSetups: itemMatchPageModificationSetups )",
				"{ var selectedItemIdInPostBack = control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator ); return " +
				( nonNullableField
					  ? "selectedItemIdInPostBack.HasValue ? selectedItemIdInPostBack.Value : default( " + field.TypeName + " )"
					  : "selectedItemIdInPostBack" ) + "; }",
				"" );
			writeFormItemGetters(
				writer,
				field,
				"SelectList<" + field.NullableTypeName + ">",
				"DropDown",
				field.NullableTypeName,
				"null",
				new CSharpParameter( "IEnumerable<SelectListItem<" + field.NullableTypeName + ">>", "items" ).ToCollection()
					.Concat( nonNullableField ? new CSharpParameter[ 0 ] : new CSharpParameter( "string", "defaultValueItemLabel" ).ToCollection() ),
				new CSharpParameter[ 0 ],
				new CSharpParameter( "Unit?", "width", "null" ).ToCollection()
					.Concat( nonNullableField ? new CSharpParameter[ 0 ] : new CSharpParameter( "bool", "placeholderIsValid", "true" ).ToCollection() )
					.Concat(
						new[]
							{
								new CSharpParameter( "string", "placeholderText", "\"Please select\"" ), new CSharpParameter( "FormAction", "action", "null" ),
								new CSharpParameter( "bool", "autoPostBack", "false" ),
								new CSharpParameter( "PageModificationValue<{0}>".FormatWith( field.NullableTypeName ), "itemIdPageModificationValue", "null" ),
								new CSharpParameter(
									"IEnumerable<ListItemMatchPageModificationSetup<{0}>>".FormatWith( field.NullableTypeName ),
									"itemMatchPageModificationSetups",
									"null" )
							} ),
				new CSharpParameter[ 0 ],
				"SelectList.CreateDropDown( items, v, width: width, defaultValueItemLabel: " + ( nonNullableField ? "\"\"" : "defaultValueItemLabel" ) +
				", placeholderIsValid: " + ( nonNullableField ? "false" : "placeholderIsValid" ) +
				", placeholderText: placeholderText, action: action, autoPostBack: autoPostBack, itemIdPageModificationValue: itemIdPageModificationValue, itemMatchPageModificationSetups: itemMatchPageModificationSetups )",
				"{ var selectedItemIdInPostBack = control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator ); return " +
				( nonNullableField
					  ? "selectedItemIdInPostBack.HasValue ? selectedItemIdInPostBack.Value : default( " + field.TypeName + " )"
					  : "selectedItemIdInPostBack" ) + "; }",
				"" );
		}

		private static void writeFormItemGetter(
			TextWriter writer, ModificationField field, string controlTypeForName, IEnumerable<CSharpParameter> requiredParams, bool controlIsLabeled,
			IEnumerable<CSharpParameter> preValueOptionalParams, string valueParamTypeName, IEnumerable<CSharpParameter> postValueOptionalParams,
			bool includeAdditionalValidationMethodParam, Func<string, string> formItemExpressionGetter, string preFormItemStatements = "",
			string postFormItemStatements = "", IEnumerable<string> additionalSummarySentences = null ) {
			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				getFormItemGetterSummary( field, controlTypeForName, additionalSummarySentences ?? new string[ 0 ] ) );
			if( includeAdditionalValidationMethodParam )
				CodeGenerationStatics.AddParamDocComment(
					writer,
					"additionalValidationMethod",
					"A method that takes the form control’s validator and performs additional validation." );

			var parameters = new List<CSharpParameter>();
			parameters.AddRange( requiredParams );
			parameters.Add( new CSharpParameter( "FormItemSetup", "formItemSetup", "null" ) );
			parameters.Add( new CSharpParameter( "IReadOnlyCollection<PhrasingComponent>", "label", "null" ) );
			if( controlIsLabeled )
				parameters.Add( new CSharpParameter( "IReadOnlyCollection<PhrasingComponent>", "formItemLabel", "null" ) );
			parameters.AddRange( preValueOptionalParams );
			parameters.Add( new CSharpParameter( valueParamTypeName, "value", "null" ) );
			parameters.AddRange( postValueOptionalParams );
			if( includeAdditionalValidationMethodParam )
				parameters.Add( new CSharpParameter( "System.Action<Validator>", "additionalValidationMethod", "null" ) );

			writer.WriteLine(
				"public FormItem " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + controlTypeForName + "FormItem" ) + "( " +
				parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() + " ) {" );
			writer.WriteLine( "label = label ?? \"{0}\".ToComponents();".FormatWith( getDefaultLabel( field ) ) );
			writer.WriteLine(
				StringTools.ConcatenateWithDelimiter(
					Environment.NewLine,
					preFormItemStatements,
					"var formItem = {0};".FormatWith( formItemExpressionGetter( EwlStatics.GetCSharpIdentifier( field.PrivateFieldName ) ) ),
					postFormItemStatements,
					"return formItem;" ) );
			writer.WriteLine( "}" );
		}

		private static void writeFormItemGetters(
			TextWriter writer, ModificationField field, string controlType, string controlTypeForName, string valueParamTypeName, string valueParamDefaultValue,
			IEnumerable<CSharpParameter> requiredControlParams, IEnumerable<CSharpParameter> requiredValidationParams,
			IEnumerable<CSharpParameter> optionalControlParams, IEnumerable<CSharpParameter> optionalValidationParams, string controlGetterExpressionOrBlock,
			string validationMethodExpressionOrBlock, string labelOverrideNullCoalescingExpression, string preFormItemGetterStatements = "",
			string postFormItemGetterStatements = "", IEnumerable<string> additionalSummarySentences = null ) {
			writeFormItemGetterWithoutValueParams(
				writer,
				controlType,
				field,
				controlTypeForName,
				validationMethodExpressionOrBlock.Any(),
				requiredControlParams,
				requiredValidationParams,
				optionalControlParams,
				optionalValidationParams,
				additionalSummarySentences ?? new string[ 0 ] );
			writeFormItemGetterWithValueParams(
				writer,
				controlType,
				field,
				controlTypeForName,
				valueParamTypeName,
				valueParamDefaultValue,
				requiredControlParams,
				requiredValidationParams,
				optionalControlParams,
				optionalValidationParams,
				preFormItemGetterStatements,
				controlGetterExpressionOrBlock,
				validationMethodExpressionOrBlock,
				labelOverrideNullCoalescingExpression,
				postFormItemGetterStatements,
				additionalSummarySentences ?? new string[ 0 ] );
		}

		private static void writeFormItemGetterWithoutValueParams(
			TextWriter writer, string controlType, ModificationField field, string controlTypeForName, bool includeValidationParams,
			IEnumerable<CSharpParameter> requiredControlParams, IEnumerable<CSharpParameter> requiredValidationParams,
			IEnumerable<CSharpParameter> optionalControlParams, IEnumerable<CSharpParameter> optionalValidationParams,
			IEnumerable<string> additionalSummarySentences ) {
			// NOTE: The "out" parameter logic is a hack. We need to improve CSharpParameter.
			var body = "return " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + controlTypeForName + "FormItem" ) + "( false, " +
			           requiredControlParams.Concat( requiredValidationParams )
				           .Select( i => ( i.MethodSignatureDeclaration.StartsWith( "out " ) ? "out " : "" ) + i.Name )
				           .GetCommaDelimitedStringFromCollection()
				           .AppendDelimiter( ", " ) + "formItemSetup: formItemSetup, labelAndSubject: labelAndSubject, labelOverride: labelOverride" +
			           optionalControlParams.Select( i => i.Name + ": " + i.Name ).GetCommaDelimitedStringFromCollection().PrependDelimiter( ", " ) +
			           ( includeValidationParams ? ", validationPredicate: validationPredicate" : "" ) +
			           optionalValidationParams.Select( i => i.Name + ": " + i.Name ).GetCommaDelimitedStringFromCollection().PrependDelimiter( ", " ) +
			           ( includeValidationParams
				             ? ", validationErrorNotifier: validationErrorNotifier, additionalValidationMethod: additionalValidationMethod"
				             : "" ) + " );";

			writeFormItemGetter(
				writer,
				controlType,
				field,
				controlTypeForName,
				"",
				requiredControlParams,
				requiredValidationParams,
				"",
				optionalControlParams,
				includeValidationParams,
				optionalValidationParams,
				body,
				additionalSummarySentences );
		}

		private static void writeFormItemGetterWithValueParams(
			TextWriter writer, string controlType, ModificationField field, string controlTypeForName, string valueParamTypeName, string valueParamDefaultValue,
			IEnumerable<CSharpParameter> requiredControlParams, IEnumerable<CSharpParameter> requiredValidationParams,
			IEnumerable<CSharpParameter> optionalControlParams, IEnumerable<CSharpParameter> optionalValidationParams, string preFormItemGetterStatements,
			string controlGetterExpressionOrBlock, string validationMethodExpressionOrBlock, string labelOverrideNullCoalescingExpression,
			string postFormItemGetterStatements, IEnumerable<string> additionalSummarySentences ) {
			var validationMethod = "( control, postBackValues, subject, validator ) => " + validationMethodExpressionOrBlock;
			var formItemGetterStatement = "var formItem = " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + "FormItem" ) + "( useValueParameter, " +
			                              "( {0} ) => {1}".FormatWith( controlType.Any() ? "v, ls" : "v, ls, vs", controlGetterExpressionOrBlock ) +
			                              ( validationMethodExpressionOrBlock.Any() ? ", " + validationMethod : "" ) +
			                              ", setup: formItemSetup, labelAndSubject: labelAndSubject, labelOverride: labelOverride" +
			                              labelOverrideNullCoalescingExpression.PrependDelimiter( " ?? " ) + ", value: value" +
			                              ( validationMethodExpressionOrBlock.Any()
				                                ? ", validationPredicate: validationPredicate, validationErrorNotifier: validationErrorNotifier, additionalValidationMethod: additionalValidationMethod"
				                                : "" ) + " );";
			writeFormItemGetter(
				writer,
				controlType,
				field,
				controlTypeForName,
				valueParamTypeName,
				requiredControlParams,
				requiredValidationParams,
				valueParamDefaultValue,
				optionalControlParams,
				validationMethodExpressionOrBlock.Any(),
				optionalValidationParams,
				StringTools.ConcatenateWithDelimiter(
					Environment.NewLine,
					preFormItemGetterStatements,
					formItemGetterStatement,
					postFormItemGetterStatements,
					"return formItem;" ),
				additionalSummarySentences );
		}

		private static void writeFormItemGetter(
			TextWriter writer, string controlType, ModificationField field, string controlTypeForName, string valueParamTypeName,
			IEnumerable<CSharpParameter> requiredControlParams, IEnumerable<CSharpParameter> requiredValidationParams, string valueParamDefaultValue,
			IEnumerable<CSharpParameter> optionalControlParams, bool includeValidationParams, IEnumerable<CSharpParameter> optionalValidationParams, string body,
			IEnumerable<string> additionalSummarySentences ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, getFormItemGetterSummary( field, controlTypeForName, additionalSummarySentences ) );
			CodeGenerationStatics.AddParamDocComment( writer, "additionalValidationMethod", "Passes the labelAndSubject and a validator to the function." );

			var parameters = new List<CSharpParameter>();
			if( valueParamTypeName.Length > 0 )
				parameters.Add( new CSharpParameter( "bool", "useValueParameter" ) );
			parameters.AddRange( requiredControlParams );
			parameters.AddRange( requiredValidationParams );
			parameters.Add( new CSharpParameter( "FormItemSetup", "formItemSetup", "null" ) );
			parameters.Add( new CSharpParameter( "string", "labelAndSubject", "\"" + getDefaultLabel( field ) + "\"" ) );
			parameters.Add( new CSharpParameter( "FormItemLabel", "labelOverride", "null" ) );
			if( valueParamTypeName.Length > 0 )
				parameters.Add( new CSharpParameter( valueParamTypeName, "value", valueParamDefaultValue ) );
			parameters.AddRange( optionalControlParams );
			if( includeValidationParams )
				parameters.Add( new CSharpParameter( "System.Func<bool>", "validationPredicate", "null" ) );
			parameters.AddRange( optionalValidationParams );
			if( includeValidationParams ) {
				parameters.Add( new CSharpParameter( "System.Action", "validationErrorNotifier", "null" ) );
				parameters.Add( new CSharpParameter( "System.Action<string,Validator>", "additionalValidationMethod", "null" ) );
			}

			writer.WriteLine(
				"public FormItem" + controlType.Surround( "<", ">" ) + " " +
				EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + controlTypeForName + "FormItem" ) + "( " +
				parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() + " ) {" );
			writer.WriteLine( body );
			writer.WriteLine( "}" );
		}

		private static CSharpParameter getAllowEmptyParameter( bool isOptional ) {
			return new CSharpParameter( "bool", "allowEmpty", isOptional ? "true" : "" );
		}

		private static void writeGenericGetterWithoutValueParams( TextWriter writer, ModificationField field, bool? includeValidationMethodReturnValue ) {
			var controlGetter = "( v, ls ) => controlGetter( v" + ( field.TypeName != field.NullableTypeName ? ".Value" : "" ) + ", ls )";
			var body = "return " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + "FormItem" ) + "( false, " + controlGetter +
			           ( includeValidationMethodReturnValue.HasValue ? ", validationMethod" : "" ) +
			           ", setup: setup, labelAndSubject: labelAndSubject, labelOverride: labelOverride" +
			           ( includeValidationMethodReturnValue.HasValue
				             ? ", validationPredicate: validationPredicate, validationErrorNotifier: validationErrorNotifier, additionalValidationMethod: additionalValidationMethod"
				             : "" ) + " );";
			writeGenericGetter( writer, field, false, includeValidationMethodReturnValue, body );
		}

		private static void writeGenericGetterWithValueParams( TextWriter writer, ModificationField field, bool? includeValidationMethodReturnValue ) {
			var control = "useValueParameter ? controlGetter( value, labelAndSubject ) : controlGetter( " + EwlStatics.GetCSharpIdentifier( field.Name ) +
			              ", labelAndSubject )";
			var body = "return FormItem.Create( labelOverride ?? labelAndSubject, " + control + ", setup: setup" +
			           ( includeValidationMethodReturnValue.HasValue
				             ? ", validationGetter: " + getValidationGetter( field, includeValidationMethodReturnValue.Value )
				             : "" ) + " );";
			writeGenericGetter( writer, field, true, includeValidationMethodReturnValue, body );
		}

		private static string getValidationGetter( ModificationField field, bool includeValidationMethodReturnValue ) {
			var fieldPropertyName = EwlStatics.GetCSharpIdentifier( field.Name );
			var statements = new[]
				{
					"if( validationPredicate != null && !validationPredicate() ) return;",
					( includeValidationMethodReturnValue ? fieldPropertyName + " = " : "" ) +
					"validationMethod( control, postBackValues, labelAndSubject, validator );",
					"if( validator.ErrorsOccurred && validationErrorNotifier != null ) validationErrorNotifier();",
					"if( !validator.ErrorsOccurred && additionalValidationMethod != null ) additionalValidationMethod( labelAndSubject, validator );"
				};
			return "control => new EwfValidation( ( postBackValues, validator ) => { " + StringTools.ConcatenateWithDelimiter( " ", statements ) + " } )";
		}

		private static void writeGenericGetter( TextWriter writer, ModificationField field ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, getFormItemGetterSummary( field, "", new string[ 0 ] ) );

			var parameters = new List<CSharpParameter>();
			parameters.Add( new CSharpParameter( "System.Func<{0},IEnumerable<FlowComponent>>".FormatWith( field.NullableTypeName ), "contentGetter" ) );
			parameters.Add( new CSharpParameter( "FormItemSetup", "setup", "null" ) );
			parameters.Add( new CSharpParameter( "IReadOnlyCollection<PhrasingComponent>", "label", "null" ) );
			parameters.Add(
				new CSharpParameter(
					field.TypeIs( typeof( string ) ) ? field.NullableTypeName : "SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					"value",
					"null" ) );
			parameters.Add( new CSharpParameter( "System.Func<System.Action<{0}>,EwfValidation>".FormatWith( field.TypeName ), "validationGetter", "null" ) );

			writer.WriteLine(
				"public FormItem " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + "FormItem" ) + "( " +
				parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() + " ) {" );
			writer.WriteLine( "label = label ?? \"{0}\".ToComponents();".FormatWith( getDefaultLabel( field ) ) );
			writer.WriteLine(
				"return {0}.ToFormItem( setup: setup, label: label, validation: {1} );".FormatWith(
					"contentGetter( {0} )".FormatWith(
						field.TypeIs( typeof( string ) )
							? "value ?? {0}".FormatWith( EwlStatics.GetCSharpIdentifier( field.Name ) )
							: "value != null ? value.Value : {0}".FormatWith( EwlStatics.GetCSharpIdentifier( field.Name ) ) ),
					"validationGetter?.Invoke( v => {0} = v )".FormatWith( EwlStatics.GetCSharpIdentifier( field.Name ) ) ) );
			writer.WriteLine( "}" );
		}

		private static void writeGenericGetter(
			TextWriter writer, ModificationField field, bool includeValueParams, bool? includeValidationMethodReturnValue, string body ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, getFormItemGetterSummary( field, "", new string[ 0 ] ) );
			CodeGenerationStatics.AddParamDocComment( writer, "additionalValidationMethod", "Passes the labelAndSubject and a validator to the function." );

			var parameters = new List<CSharpParameter>();
			if( includeValueParams )
				parameters.Add( new CSharpParameter( "bool", "useValueParameter" ) );
			parameters.Add(
				new CSharpParameter( "System.Func<" + ( includeValueParams ? field.NullableTypeName : field.TypeName ) + ",string,ControlType>", "controlGetter" ) );
			if( includeValidationMethodReturnValue.HasValue )
				parameters.Add(
					includeValidationMethodReturnValue.Value
						? new CSharpParameter( "System.Func<ControlType,PostBackValueDictionary,string,Validator," + field.TypeName + ">", "validationMethod" )
						: new CSharpParameter( "System.Action<ControlType,PostBackValueDictionary,string,Validator>", "validationMethod" ) );
			parameters.Add( new CSharpParameter( "FormItemSetup", "setup", "null" ) );
			parameters.Add( new CSharpParameter( "string", "labelAndSubject", "\"" + getDefaultLabel( field ) + "\"" ) );
			parameters.Add( new CSharpParameter( "FormItemLabel", "labelOverride", "null" ) );
			if( includeValueParams )
				parameters.Add( new CSharpParameter( field.NullableTypeName, "value", field.TypeIs( typeof( string ) ) ? "\"\"" : "null" ) );
			if( includeValidationMethodReturnValue.HasValue ) {
				parameters.Add( new CSharpParameter( "System.Func<bool>", "validationPredicate", "null" ) );
				parameters.Add( new CSharpParameter( "System.Action", "validationErrorNotifier", "null" ) );
				parameters.Add( new CSharpParameter( "System.Action<string,Validator>", "additionalValidationMethod", "null" ) );
			}

			writer.WriteLine(
				"public FormItem<ControlType> " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + "FormItem" ) + "<ControlType>( " +
				parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() + " ) where ControlType: Control {" );
			writer.WriteLine( body );
			writer.WriteLine( "}" );
		}

		private static string getFormItemGetterSummary( ModificationField field, string controlType, IEnumerable<string> additionalSentences ) {
			var sentences = new[]
				{
					"Creates a " + field.Name + controlType.PrependDelimiter( " " ) + " form item, which includes a label, a page component, and a validation.",
					"The default label is “{0}”.".FormatWith( getDefaultLabel( field ) ),
					controlType.Any() ? "" : "This is a generic form-item getter; use it only if there is no specific getter for the control type that you need.",
					"You almost certainly should not call this method from a deferred block of code since this could cause validations to be added to the data modification in the wrong order."
				};
			return StringTools.ConcatenateWithDelimiter( " ", sentences.Concat( additionalSentences ).ToArray() );
		}

		private static string getDefaultLabel( ModificationField field ) {
			var result = field.PascalCasedName.CamelToEnglish();
			if( result.ToLower().EndsWith( " id" ) )
				result = result.Substring( 0, result.Length - 3 );
			return result;
		}
	}
}