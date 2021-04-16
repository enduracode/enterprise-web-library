﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal static class FormItemStatics {
		internal static void WriteFormItemGetters( TextWriter writer, ModificationField field ) {
			// Some of these form item getters need modification methods to be executed to work properly. They return these methods, as out parameters. This allows
			// client code on a page to specify the order of modification methods, which is important because there may be both child modifications (like file
			// collections) and one-to-many modifications (like M+Vision references for an applicant) on the same page, and the main modification needs to execute
			// between these.
			writeTextFormItemGetters( writer, field );
			writeNumericFormItemGetters( writer, field );
			writeCheckboxFormItemGetters( writer, field );
			writeListFormItemGetters( writer, field );
			writeDateAndTimeFormItemGetters( writer, field );

			writeGenericGetter( writer, field );
		}

		private static void writeTextFormItemGetters( TextWriter writer, ModificationField field ) {
			if( field.TypeIs( typeof( string ) ) ) {
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
					dv => "{0}.ToTextControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
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
						"{0}.ToEmailAddressControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
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
						"{0}.ToTelephoneNumberControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
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
					dv => "{0}.ToUrlControl( allowEmpty, setup: controlSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
						.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
			}

			if( field.TypeIs( typeof( string ) ) )
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
						"{0}.ToNumericTextControl( allowEmpty, setup: controlSetup, value: value, minLength: minLength, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv, field.Size?.ToString() ?? "null" ) );
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
						"{0}.ToTextControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
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
						"{0}.ToTextControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv ) );

			if( field.TypeIs( typeof( string ) ) )
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
					dv => "{0}.ToHtmlEditor( allowEmpty, setup: editorSetup, value: value, maxLength: {1}, additionalValidationMethod: additionalValidationMethod )"
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
						"{0}.ToNumberControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod )"
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
						"{0}.ToNumberControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod )"
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
						"{0}.ToImpreciseNumberControl( minValue, maxValue, setup: controlSetup, value: value, valueStep: valueStep, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv ),
					preFormItemStatements: getNumberControlValueStepStatements( field ) );

			if( field.TypeIs( typeof( int ) ) )
				writeHtmlAndFileFormItemGetters( writer, field, "int?" );
			if( field.TypeIs( typeof( int? ) ) )
				writeHtmlAndFileFormItemGetters( writer, field, "int?" );

			if( field.TypeIs( typeof( decimal ) ) )
				writeHtmlAndFileFormItemGetters( writer, field, "decimal?" );
			if( field.TypeIs( typeof( decimal? ) ) )
				writeHtmlAndFileFormItemGetters( writer, field, "decimal?" );
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
				dv => "{0}.ToCheckbox( label, setup: checkboxSetup, value: {1}, additionalValidationMethod: {2} )".FormatWith(
					getDataValueExpression( dv ),
					getValueExpression( dv ),
					getAdditionalValidationMethodExpression( dv ) ),
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
				dv => "{0}.ToFlowCheckbox( label, setup: checkboxSetup, value: {1}, additionalValidationMethod: {2} )".FormatWith(
					getDataValueExpression( dv ),
					getValueExpression( dv ),
					getAdditionalValidationMethodExpression( dv ) ),
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
				dv => "{0}.ToRadioButton( group, label, setup: radioButtonSetup, value: {1}, additionalValidationMethod: {2} )".FormatWith(
					getDataValueExpression( dv ),
					getValueExpression( dv ),
					getAdditionalValidationMethodExpression( dv ) ),
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
				dv => "{0}.ToFlowRadioButton( group, label, setup: radioButtonSetup, value: {1}, additionalValidationMethod: {2} )".FormatWith(
					getDataValueExpression( dv ),
					getValueExpression( dv ),
					getAdditionalValidationMethodExpression( dv ) ),
				preFormItemStatements: preFormItemStatements );
		}

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
				dv => "new HtmlBlockEditor( (int?)( value != null ? value.Value : {0}.Value ), id => {0}.Value = id, out m, setup: editorSetup )".FormatWith( dv ),
				preFormItemStatements: "HtmlBlockEditorModification m = null;",
				postFormItemStatements: "mod = m;" );
			writeFormItemGetter(
				writer,
				field,
				"File",
				new CSharpParameter( "out System.Action", "modificationMethod" ).ToCollection(),
				false,
				new CSharpParameter( "BlobFileManagerSetup", "managerSetup", "null" ).ToCollection(),
				"SpecifiedValue<{0}>".FormatWith( valueParamTypeName ),
				new CSharpParameter( "bool", "requireUploadIfNoFile", "false" ).ToCollection(),
				false,
				dv =>
					"new BlobFileManager( (int?)( value != null ? value.Value : {0}.Value ), requireUploadIfNoFile, id => {0}.Value = id, out modificationMethod, setup: managerSetup )"
						.FormatWith( dv ) );
		}

		private static void writeListFormItemGetters( TextWriter writer, ModificationField field ) {
			if( field.TypeIs( typeof( bool ) ) || field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( decimal ) ) )
				writeFormItemGetter(
					writer,
					field,
					"RadioList",
					new CSharpParameter( "RadioListSetup<{0}>".FormatWith( field.NullableTypeName ), "controlSetup" ).ToCollection(),
					false,
					Enumerable.Empty<CSharpParameter>(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					Enumerable.Empty<CSharpParameter>(),
					true,
					dv => "{0}.ToRadioList( controlSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
			if( field.TypeIs( typeof( bool? ) ) || field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) || field.TypeIs( typeof( string ) ) ||
			    field.TypeIs( typeof( decimal? ) ) )
				writeFormItemGetter(
					writer,
					field,
					"RadioList",
					new CSharpParameter( "RadioListSetup<{0}>".FormatWith( field.NullableTypeName ), "controlSetup" ).ToCollection(),
					false,
					new CSharpParameter( "string", "defaultValueItemLabel", defaultValue: field.TypeIs( typeof( string ) ) ? "\"\"" : "\"None\"" ).ToCollection(),
					field.TypeIs( typeof( string ) ) ? field.NullableTypeName : "SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					Enumerable.Empty<CSharpParameter>(),
					true,
					dv =>
						"{0}.ToRadioList( controlSetup, defaultValueItemLabel: defaultValueItemLabel, value: value, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv ) );

			if( field.TypeIs( typeof( bool ) ) || field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( long ) ) || field.TypeIs( typeof( decimal ) ) )
				writeFormItemGetter(
					writer,
					field,
					"DropDown",
					new CSharpParameter( "DropDownSetup<{0}>".FormatWith( field.NullableTypeName ), "controlSetup" ).ToCollection(),
					false,
					Enumerable.Empty<CSharpParameter>(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					Enumerable.Empty<CSharpParameter>(),
					true,
					dv => "{0}.ToDropDown( controlSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
			if( field.TypeIs( typeof( bool? ) ) || field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( long? ) ) || field.TypeIs( typeof( string ) ) ||
			    field.TypeIs( typeof( decimal? ) ) )
				writeFormItemGetter(
					writer,
					field,
					"DropDown",
					new CSharpParameter( "DropDownSetup<{0}>".FormatWith( field.NullableTypeName ), "controlSetup" ).ToCollection()
						.Concat(
							field.TypeIs( typeof( string ) )
								? Enumerable.Empty<CSharpParameter>()
								: new CSharpParameter( "string", "defaultValueItemLabel" ).ToCollection() ),
					false,
					( field.TypeIs( typeof( string ) )
						  ? new CSharpParameter( "string", "defaultValueItemLabel", defaultValue: "\"\"" ).ToCollection()
						  : Enumerable.Empty<CSharpParameter>() ).Append(
						new CSharpParameter( "bool", "placeholderIsValid", field.TypeIs( typeof( string ) ) ? "false" : "true" ) ),
					field.TypeIs( typeof( string ) ) ? field.NullableTypeName : "SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					Enumerable.Empty<CSharpParameter>(),
					true,
					dv =>
						"{0}.ToDropDown( controlSetup, {1}defaultValueItemLabel, placeholderIsValid: placeholderIsValid, value: value, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv, field.TypeIs( typeof( string ) ) ? "defaultValueItemLabel: " : "" ) );

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
					dv => "{0}.ToCheckboxList( checkboxListSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
		}

		private static void writeDateAndTimeFormItemGetters( TextWriter writer, ModificationField field ) {
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
						"{0}.ToDateControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
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
						"{0}.ToDateControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv ) );

			if( field.TypeIs( typeof( DateTime ) ) )
				writeFormItemGetter(
					writer,
					field,
					"DateAndTimeControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "DateAndTimeControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					new CSharpParameter( "LocalDate?", "minValue", "null" ).ToCollection().Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
					true,
					dv =>
						"{0}.ToDateAndTimeControl( setup: controlSetup, value: value, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv ) );
			if( field.TypeIs( typeof( DateTime? ) ) )
				writeFormItemGetter(
					writer,
					field,
					"DateAndTimeControl",
					Enumerable.Empty<CSharpParameter>(),
					false,
					new CSharpParameter( "DateAndTimeControlSetup", "controlSetup", defaultValue: "null" ).ToCollection(),
					"SpecifiedValue<{0}>".FormatWith( field.NullableTypeName ),
					getAllowEmptyParameter( true )
						.ToCollection()
						.Append( new CSharpParameter( "LocalDate?", "minValue", "null" ) )
						.Append( new CSharpParameter( "LocalDate?", "maxValue", "null" ) ),
					true,
					dv =>
						"{0}.ToDateAndTimeControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, minValue: minValue, maxValue: maxValue, additionalValidationMethod: additionalValidationMethod )"
							.FormatWith( dv ) );

			if( field.TypeIs( typeof( int ) ) || field.TypeIs( typeof( int? ) ) || field.TypeIs( typeof( decimal ) ) || field.TypeIs( typeof( decimal? ) ) )
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
						      ? "{0}.ToDurationControl( setup: controlSetup, value: value, allowEmpty: allowEmpty, additionalValidationMethod: additionalValidationMethod )"
							      .FormatWith( dv )
						      : "{0}.ToDurationControl( setup: controlSetup, value: value, additionalValidationMethod: additionalValidationMethod )".FormatWith( dv ) );
		}

		private static void writeFormItemGetter(
			TextWriter writer, ModificationField field, string controlTypeForName, IEnumerable<CSharpParameter> requiredParams, bool controlIsLabeled,
			IEnumerable<CSharpParameter> preValueOptionalParams, string valueParamTypeName, IEnumerable<CSharpParameter> postValueOptionalParams,
			bool includeAdditionalValidationMethodParam, Func<string, string> formControlExpressionGetter, string preFormItemStatements = "",
			string postFormItemStatements = "", IEnumerable<string> additionalSummarySentences = null ) {
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
				parameters.Add(
					new CSharpParameter(
						"System.Action<Validator>",
						"additionalValidationMethod",
						defaultValue: "null",
						description: "A method that takes the form control’s validator and performs additional validation." ) );

			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				getFormItemGetterSummary( field, controlTypeForName, additionalSummarySentences ?? new string[ 0 ] ) );
			foreach( var i in parameters )
				CodeGenerationStatics.AddParamDocComment( writer, i.Name, i.Description );
			writer.WriteLine(
				"public FormItem " + EwlStatics.GetCSharpIdentifier( "Get" + field.PascalCasedName + controlTypeForName + "FormItem" ) + "( " +
				parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() + " ) {" );
			writer.WriteLine( "label = label ?? \"{0}\".ToComponents();".FormatWith( getDefaultLabel( field ) ) );
			writer.WriteLine(
				StringTools.ConcatenateWithDelimiter(
					Environment.NewLine,
					preFormItemStatements,
					"var formItem = {0}.ToFormItem( setup: formItemSetup, label: {1} );".FormatWith(
						formControlExpressionGetter( EwlStatics.GetCSharpIdentifier( field.PrivateFieldName ) ),
						controlIsLabeled ? "formItemLabel" : "label" ),
					postFormItemStatements,
					"return formItem;" ) );
			writer.WriteLine( "}" );
		}

		private static CSharpParameter getAllowEmptyParameter( bool isOptional ) {
			return new CSharpParameter( "bool", "allowEmpty", isOptional ? "true" : "" );
		}

		private static void writeGenericGetter( TextWriter writer, ModificationField field ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, getFormItemGetterSummary( field, "", new string[ 0 ] ) );

			var parameters = new List<CSharpParameter>();
			parameters.Add( new CSharpParameter( "System.Func<{0},IReadOnlyCollection<FlowComponent>>".FormatWith( field.NullableTypeName ), "contentGetter" ) );
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