using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
    internal static class FormItemStatics {
        private const string validationListParamDocComment =
            "The DataModification or BasicValidationList to which this form item's validation should be added. Pass null to use the page's data update.";

        internal static void WriteFormItemGetters( TextWriter writer, ModificationField field ) {
            // Some of these form item getters need modification methods to be executed to work properly. They return these methods, as out parameters, instead of
            // just adding them to the data modification. This allows client code on a page to specify the order of modification methods, which is important because
            // there may be both child modifications (like file collections) and one-to-many modifications (like M+Vision references for an applicant) on the same
            // page, and the main modification needs to execute between these.
            writeStringFormItemGetters( writer, field );
            writeNumericFormItemGetters( writer, field );
            writeBoolFormItemGetters( writer, field );
            writeDateFormItemGetters( writer, field );
            writeEnumerableFormItemGetters( writer, field );
            writeGuidFormItemGetters( writer, field );

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

            writeFormItemGetters( writer,
                                  field,
                                  "EwfTextBox",
                                  "Text",
                                  "string",
                                  "\"\"",
                                  new CSharpParameter[ 0 ],
                                  getAllowEmptyParameter( false ).ToSingleElementArray(),
                                  new[]
                                      {
                                          new CSharpParameter( "int", "textBoxRows", "1" ), new CSharpParameter( "bool", "masksCharacters", "false" ),
                                          new CSharpParameter( "bool", "readOnly", "false" ), new CSharpParameter( "bool?", "suggestSpellCheck", "null" ),
                                          new CSharpParameter( "PostBack", "postBack", "null" ), new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "new EwfTextBox( v, rows: textBoxRows, masksCharacters: masksCharacters, " +
                                  ( field.Size.HasValue ? "maxLength: " + field.Size.Value + ", " : "" ) +
                                  "readOnly: readOnly, suggestSpellCheck: suggestSpellCheck, postBack: postBack, autoPostBack: autoPostBack )",
                                  "validator.GetString( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty" +
                                  ( field.Size.HasValue ? ", " + field.Size.Value : "" ) + " )",
                                  "true" );
            writeFormItemGetters( writer,
                                  field,
                                  "EwfTextBox",
                                  "EmailAddress",
                                  "string",
                                  "\"\"",
                                  new CSharpParameter[ 0 ],
                                  getAllowEmptyParameter( false ).ToSingleElementArray(),
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "readOnly", "false" ), new CSharpParameter( "PostBack", "postBack", "null" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "new EwfTextBox( v" + ( field.Size.HasValue ? ", maxLength: " + field.Size.Value : "" ) +
                                  ", readOnly: readOnly, postBack: postBack, autoPostBack: autoPostBack )",
                                  "validator.GetEmailAddress( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty" +
                                  ( field.Size.HasValue ? ", maxLength: " + field.Size.Value : "" ) + " )",
                                  "true" );
            writeFormItemGetters( writer,
                                  field,
                                  "EwfTextBox",
                                  "PhoneNumber",
                                  "string",
                                  "\"\"",
                                  new CSharpParameter[ 0 ],
                                  getAllowEmptyParameter( false ).ToSingleElementArray(),
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "readOnly", "false" ), new CSharpParameter( "PostBack", "postBack", "null" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "allowExtension", "true" ),
                                          new CSharpParameter( "bool", "allowSurroundingGarbage", "false" )
                                      },
                                  "new EwfTextBox( v" + ( field.Size.HasValue ? ", maxLength: " + field.Size.Value : "" ) +
                                  ", readOnly: readOnly, postBack: postBack, autoPostBack: autoPostBack )",
                                  "validator.GetPhoneNumber( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowExtension, allowEmpty, allowSurroundingGarbage )",
                                  "true" );
            writeFormItemGetters( writer,
                                  field,
                                  "EwfTextBox",
                                  "Uri",
                                  "string",
                                  "\"\"",
                                  new CSharpParameter[ 0 ],
                                  getAllowEmptyParameter( false ).ToSingleElementArray(),
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "readOnly", "false" ), new CSharpParameter( "PostBack", "postBack", "null" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "new EwfTextBox( v" + ( field.Size.HasValue ? ", maxLength: " + field.Size.Value : "" ) +
                                  ", readOnly: readOnly, postBack: postBack, autoPostBack: autoPostBack )",
                                  "validator.GetUrl( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty" +
                                  ( field.Size.HasValue ? ", " + field.Size.Value : "" ) + " )",
                                  "true" );
            writeFormItemGetters( writer,
                                  field,
                                  "SelectList<string>",
                                  "RadioList",
                                  "string",
                                  "\"\"",
                                  new[] { new CSharpParameter( "IEnumerable<EwfListItem<string>>", "items" ) },
                                  new CSharpParameter[ 0 ],
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "useHorizontalLayout", "false" ),
                                          new CSharpParameter( "string", "defaultValueItemLabel", "\"\"" ),
                                          new CSharpParameter( "bool", "disableSingleButtonDetection", "false" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "SelectList.CreateRadioList( items, v, useHorizontalLayout: useHorizontalLayout, defaultValueItemLabel: defaultValueItemLabel, disableSingleButtonDetection: disableSingleButtonDetection, autoPostBack: autoPostBack )",
                                  "control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator )",
                                  "true" );
            writeFormItemGetters( writer,
                                  field,
                                  "SelectList<string>",
                                  "DropDown",
                                  "string",
                                  "\"\"",
                                  new[] { new CSharpParameter( "IEnumerable<EwfListItem<string>>", "items" ) },
                                  new CSharpParameter[ 0 ],
                                  new[]
                                      {
                                          new CSharpParameter( "Unit?", "width", "null" ), new CSharpParameter( "string", "defaultValueItemLabel", "\"\"" ),
                                          new CSharpParameter( "bool", "placeholderIsValid", "false" ),
                                          new CSharpParameter( "string", "placeholderText", "\"Please select\"" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "SelectList.CreateDropDown( items, v, width: width, defaultValueItemLabel: defaultValueItemLabel, placeholderIsValid: placeholderIsValid, placeholderText: placeholderText, autoPostBack: autoPostBack )",
                                  "control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator )",
                                  "true" );
            writeFormItemGetters( writer,
                                  field,
                                  "WysiwygHtmlEditor",
                                  "Html",
                                  "string",
                                  "\"\"",
                                  new CSharpParameter[ 0 ],
                                  getAllowEmptyParameter( false ).ToSingleElementArray(),
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  "new WysiwygHtmlEditor( v )",
                                  "validator.GetString( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty" +
                                  ( field.Size.HasValue ? ", " + field.Size.Value : "" ) + " )",
                                  "true",
                                  additionalSummarySentences:
                                      new[]
                                          {
                                              "WARNING: Do not use this form-item getter unless you know exactly what you're doing.",
                                              "If you want to store HTML, it is almost always better to use an HTML block instead of just a string field.",
                                              "HTML blocks have special handling for intra-site URIs and may include additional features in the future.",
                                              "They also cause all of your HTML to be stored in one place, which is usually a good practice."
                                          } );
        }

        private static void writeNumericFormItemGetters( TextWriter writer, ModificationField field ) {
            if( field.TypeIs( typeof( int ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "int?",
                                                  new[]
                                                      {
                                                          new CSharpParameter( "int", "min", "int.MinValue" ), new CSharpParameter( "int", "max", "int.MaxValue" )
                                                      },
                                                  "validator.GetInt( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), min, max )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
                writeHtmlAndFileFormItemGetters( writer, field, "int?" );
                writeFileCollectionFormItemGetters( writer, field, "int" );
                writeDurationFormItemGetters( writer, field );
            }
            if( field.TypeIs( typeof( int? ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "int?",
                                                  getAllowEmptyParameter( true ).ToSingleElementArray(),
                                                  "validator.GetNullableInt( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
                writeHtmlAndFileFormItemGetters( writer, field, "int?" );
            }

            if( field.TypeIs( typeof( short ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "short?",
                                                  new[]
                                                      {
                                                          new CSharpParameter( "short", "min", "short.MinValue" ),
                                                          new CSharpParameter( "short", "max", "short.MaxValue" )
                                                      },
                                                  "validator.GetShort( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), min, max )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
            }
            if( field.TypeIs( typeof( short? ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "short?",
                                                  new[]
                                                      {
                                                          getAllowEmptyParameter( true ), new CSharpParameter( "short", "min", "short.MinValue" ),
                                                          new CSharpParameter( "short", "max", "short.MaxValue" )
                                                      },
                                                  "validator.GetNullableShort( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty, min, max )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
            }

            if( field.TypeIs( typeof( byte ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "byte?",
                                                  new[]
                                                      {
                                                          new CSharpParameter( "byte", "min", "byte.MinValue" ),
                                                          new CSharpParameter( "byte", "max", "byte.MaxValue" )
                                                      },
                                                  "validator.GetByte( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), min, max )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
            }
            if( field.TypeIs( typeof( byte? ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "byte?",
                                                  getAllowEmptyParameter( true ).ToSingleElementArray(),
                                                  "validator.GetNullableByte( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
            }

            if( field.TypeIs( typeof( decimal ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "decimal?",
                                                  new[]
                                                      {
                                                          new CSharpParameter( "decimal", "min", "Validator.SqlDecimalDefaultMin" ),
                                                          new CSharpParameter( "decimal", "max", "Validator.SqlDecimalDefaultMax" )
                                                      },
                                                  "validator.GetDecimal( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), min, max )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
                writeCheckBoxFormItemGetters( writer, field, "decimal" );
                writeHtmlAndFileFormItemGetters( writer, field, "decimal?" );
                writeFileCollectionFormItemGetters( writer, field, "decimal" );
                writeDurationFormItemGetters( writer, field );
            }
            if( field.TypeIs( typeof( decimal? ) ) ) {
                writeNumberAsTextFormItemGetters( writer,
                                                  field,
                                                  "decimal?",
                                                  new[]
                                                      {
                                                          getAllowEmptyParameter( true ),
                                                          new CSharpParameter( "decimal", "min", "Validator.SqlDecimalDefaultMin" ),
                                                          new CSharpParameter( "decimal", "max", "Validator.SqlDecimalDefaultMax" )
                                                      },
                                                  "validator.GetNullableDecimal( new ValidationErrorHandler( subject ), control.GetPostBackValue( postBackValues ), allowEmpty, min, max )" );
                writeNumberAsSelectListFormItemGetters( writer, field );
                writeHtmlAndFileFormItemGetters( writer, field, "decimal?" );
            }
        }

        private static void writeNumberAsTextFormItemGetters( TextWriter writer, ModificationField field, string valueParamTypeName,
                                                              IEnumerable<CSharpParameter> optionalValidationParams, string validationMethodExpressionOrBlock ) {
            writeFormItemGetters( writer,
                                  field,
                                  "EwfTextBox",
                                  "Text",
                                  valueParamTypeName,
                                  "null",
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "readOnly", "false" ), new CSharpParameter( "PostBack", "postBack", "null" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  optionalValidationParams,
                                  "new EwfTextBox( v.ObjectToString( true ), readOnly: readOnly, postBack: postBack, autoPostBack: autoPostBack )",
                                  validationMethodExpressionOrBlock,
                                  "true" );
        }

        private static void writeFileCollectionFormItemGetters( TextWriter writer, ModificationField field, string valueParamTypeName ) {
            writeFormItemGetters( writer,
                                  field,
                                  "BlobFileCollectionManager",
                                  "FileCollection",
                                  valueParamTypeName,
                                  "-1",
                                  new CSharpParameter( "bool", "sortByName" ).ToSingleElementArray(),
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  "{ var control = new BlobFileCollectionManager( sortByName: sortByName ); control.LoadData( (int)v ); return control; }",
                                  "",
                                  "true" );
        }

        private static void writeDurationFormItemGetters( TextWriter writer, ModificationField field ) {
            writeFormItemGetters( writer,
                                  field,
                                  "DurationPicker",
                                  "Duration",
                                  field.TypeName,
                                  "0",
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  "new DurationPicker( System.TimeSpan.FromSeconds( (int)v ) )",
                                  "(" + field.TypeName +
                                  ")control.ValidateAndGetPostBackDuration( postBackValues, validator, new ValidationErrorHandler( subject ) ).TotalSeconds",
                                  "true" );
        }

        private static void writeHtmlAndFileFormItemGetters( TextWriter writer, ModificationField field, string valueParamTypeName ) {
            writeFormItemGetters( writer,
                                  field,
                                  "HtmlBlockEditor",
                                  "Html",
                                  valueParamTypeName,
                                  "null",
                                  new CSharpParameter( "out HtmlBlockEditorModification", "mod" ).ToSingleElementArray(),
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  "new HtmlBlockEditor( (int?)v, id => " + field.PropertyName + " = id, out m )",
                                  "control.Validate( postBackValues, validator, new ValidationErrorHandler( subject ) )",
                                  "true",
                                  preFormItemGetterStatements: "HtmlBlockEditorModification m = null;",
                                  postFormItemGetterStatements: "mod = m;" );
            writeFormItemGetters( writer,
                                  field,
                                  "BlobFileManager",
                                  "File",
                                  valueParamTypeName,
                                  "null",
                                  new CSharpParameter( "out System.Action", "modificationMethod" ).ToSingleElementArray(),
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter( "bool", "requireUploadIfNoFile", "false" ).ToSingleElementArray(),
                                  "{ var control = new BlobFileManager(); if( v.HasValue ) control.LoadData( (int)v.Value ); mm = () => " + field.PropertyName +
                                  " = control.ModifyData(); return control; }",
                                  "control.ValidateFormValues( validator, subject, requireUploadIfNoFile )",
                                  "true",
                                  preFormItemGetterStatements: "System.Action mm = null;",
                                  postFormItemGetterStatements: "modificationMethod = mm;" );
        }

        private static void writeBoolFormItemGetters( TextWriter writer, ModificationField field ) {
            if( field.TypeIs( typeof( bool ) ) ) {
                writeCheckBoxFormItemGetters( writer, field, "bool" );
                writeNumberAsSelectListFormItemGetters( writer, field );
            }
            if( field.TypeIs( typeof( bool? ) ) )
                writeNumberAsSelectListFormItemGetters( writer, field );
        }

        private static void writeCheckBoxFormItemGetters( TextWriter writer, ModificationField field, string valueParamTypeName ) {
            var fieldIsDecimal = valueParamTypeName == "decimal";
            var valueParamDefaultValue = fieldIsDecimal ? false.BooleanToDecimal().ToString() : "false";
            var toBoolSuffix = fieldIsDecimal ? ".DecimalToBoolean()" : "";
            var fromBoolSuffix = fieldIsDecimal ? ".BooleanToDecimal()" : "";

            writeFormItemGetters( writer,
                                  field,
                                  "EwfCheckBox",
                                  "CheckBox",
                                  valueParamTypeName,
                                  valueParamDefaultValue,
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter[ 0 ],
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "putLabelOnCheckBox", "true" ), new CSharpParameter( "PostBack", "postBack", "null" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "new EwfCheckBox( v.Value" + toBoolSuffix +
                                  ", label: putLabelOnCheckBox ? ls : \"\", postBack: postBack ) { AutoPostBack = autoPostBack }",
                                  "control.IsCheckedInPostBack( postBackValues )" + fromBoolSuffix,
                                  "!putLabelOnCheckBox" );
            writeFormItemGetters( writer,
                                  field,
                                  "BlockCheckBox",
                                  "BlockCheckBox",
                                  valueParamTypeName,
                                  valueParamDefaultValue,
                                  new CSharpParameter( "IEnumerable<Control>", "nestedControls" ).ToSingleElementArray(),
                                  new CSharpParameter[ 0 ],
                                  new[]
                                      {
                                          new CSharpParameter( "bool", "putLabelOnCheckBox", "true" ), new CSharpParameter( "PostBack", "postBack", "null" ),
                                          new CSharpParameter( "bool", "autoPostBack", "false" ),
                                          new CSharpParameter( "bool", "nestedControlsAlwaysVisible", "false" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "{ " +
                                  StringTools.ConcatenateWithDelimiter( " ",
                                                                        "var c = new BlockCheckBox( v.Value" + toBoolSuffix +
                                                                        ", label: putLabelOnCheckBox ? ls : \"\", postBack: postBack ) { AutoPostBack = autoPostBack, NestedControlsAlwaysVisible = nestedControlsAlwaysVisible };",
                                                                        "c.NestedControls.AddRange( nestedControls );",
                                                                        "return c;" ) + " }",
                                  "control.IsCheckedInPostBack( postBackValues )" + fromBoolSuffix,
                                  "!putLabelOnCheckBox" );
        }

        private static void writeDateFormItemGetters( TextWriter writer, ModificationField field ) {
            if( field.TypeIs( typeof( DateTime? ) ) ) {
                writeFormItemGetters( writer,
                                      field,
                                      "DatePicker",
                                      "Date",
                                      "System.DateTime?",
                                      "null",
                                      new CSharpParameter[ 0 ],
                                      new CSharpParameter[ 0 ],
                                      new[]
                                          {
                                              new CSharpParameter( "System.DateTime?", "minDate", "null" ),
                                              new CSharpParameter( "System.DateTime?", "maxDate", "null" ),
                                              new CSharpParameter( "bool", "constrainToSqlSmallDateTimeRange", "true" )
                                          },
                                      getAllowEmptyParameter( true ).ToSingleElementArray(),
                                      "{ " +
                                      StringTools.ConcatenateWithDelimiter( " ",
                                                                            "var c = new DatePicker( v ) { ConstrainToSqlSmallDateTimeRange = constrainToSqlSmallDateTimeRange };",
                                                                            "if( minDate.HasValue ) c.MinDate = minDate.Value;",
                                                                            "if( maxDate.HasValue ) c.MaxDate = maxDate.Value;",
                                                                            "return c;" ) + " }",
                                      "control.ValidateAndGetNullablePostBackDate( postBackValues, validator, new ValidationErrorHandler( subject ), allowEmpty )",
                                      "true" );
            }
            if( field.TypeIs( typeof( DateTime ) ) ) {
                writeFormItemGetters( writer,
                                      field,
                                      "DatePicker",
                                      "Date",
                                      "System.DateTime?",
                                      "null",
                                      new CSharpParameter[ 0 ],
                                      new CSharpParameter[ 0 ],
                                      new[]
                                          {
                                              new CSharpParameter( "System.DateTime?", "minDate", "null" ),
                                              new CSharpParameter( "System.DateTime?", "maxDate", "null" ),
                                              new CSharpParameter( "bool", "constrainToSqlSmallDateTimeRange", "true" )
                                          },
                                      new CSharpParameter[ 0 ],
                                      "{ " +
                                      StringTools.ConcatenateWithDelimiter( " ",
                                                                            "var c = new DatePicker( v ) { ConstrainToSqlSmallDateTimeRange = constrainToSqlSmallDateTimeRange };",
                                                                            "if( minDate.HasValue ) c.MinDate = minDate.Value;",
                                                                            "if( maxDate.HasValue ) c.MaxDate = maxDate.Value;",
                                                                            "return c;" ) + " }",
                                      "control.ValidateAndGetPostBackDate( postBackValues, validator, new ValidationErrorHandler( subject ) )",
                                      "true" );
            }
        }

        private static void writeEnumerableFormItemGetters( TextWriter writer, ModificationField field ) {
            if( !field.EnumerableElementTypeName.Any() )
                return;
            writeFormItemGetters( writer,
                                  field,
                                  "EwfCheckBoxList<" + field.EnumerableElementTypeName + ">",
                                  "CheckBoxList",
                                  field.TypeName,
                                  "null",
                                  new CSharpParameter( "IEnumerable<EwfListItem<" + field.EnumerableElementTypeName + ">>", "items" ).ToSingleElementArray(),
                                  new CSharpParameter[ 0 ],
                                  new[]
                                      {
                                          new CSharpParameter( "string", "caption", "\"\"" ),
                                          new CSharpParameter( "bool", "includeSelectAndDeselectAllButtons", "false" ),
                                          new CSharpParameter( "byte", "numberOfColumns", "1" )
                                      },
                                  new CSharpParameter[ 0 ],
                                  "new EwfCheckBoxList<" + field.EnumerableElementTypeName + ">( items, v ?? new " + field.EnumerableElementTypeName +
                                  "[ 0 ], caption: caption, includeSelectAndDeselectAllButtons: includeSelectAndDeselectAllButtons, numberOfColumns: numberOfColumns )",
                                  "control.GetSelectedItemIdsInPostBack( postBackValues )",
                                  "true" );
        }

        private static void writeGuidFormItemGetters( TextWriter writer, ModificationField field ) {
            if( !field.TypeIs( typeof( Guid ) ) )
                return;
            writeNumberAsSelectListFormItemGetters( writer, field );
        }

        private static void writeNumberAsSelectListFormItemGetters( TextWriter writer, ModificationField field ) {
            var nonNullableField = field.TypeName != field.NullableTypeName;
            writeFormItemGetters( writer,
                                  field,
                                  "SelectList<" + field.NullableTypeName + ">",
                                  "RadioList",
                                  field.NullableTypeName,
                                  "null",
                                  new[] { new CSharpParameter( "IEnumerable<EwfListItem<" + field.NullableTypeName + ">>", "items" ) },
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter( "bool", "useHorizontalLayout", "false" ).ToSingleElementArray()
                                                                                               .Concat( nonNullableField
                                                                                                            ? new CSharpParameter[ 0 ]
                                                                                                            : new CSharpParameter( "string",
                                                                                                                                   "defaultValueItemLabel",
                                                                                                                                   "\"None\"" )
                                                                                                                  .ToSingleElementArray() )
                                                                                               .Concat( new[]
                                                                                                   {
                                                                                                       new CSharpParameter( "bool",
                                                                                                                            "disableSingleButtonDetection",
                                                                                                                            "false" ),
                                                                                                       new CSharpParameter( "PostBack", "postBack", "null" ),
                                                                                                       new CSharpParameter( "bool", "autoPostBack", "false" )
                                                                                                   } ),
                                  new CSharpParameter[ 0 ],
                                  "SelectList.CreateRadioList( items, v, useHorizontalLayout: useHorizontalLayout, defaultValueItemLabel: " +
                                  ( nonNullableField ? "\"\"" : "defaultValueItemLabel" ) +
                                  ", disableSingleButtonDetection: disableSingleButtonDetection, postBack: postBack, autoPostBack: autoPostBack )",
                                  "{ var selectedItemIdInPostBack = control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator ); return " +
                                  ( nonNullableField
                                        ? "selectedItemIdInPostBack.HasValue ? selectedItemIdInPostBack.Value : default( " + field.TypeName + " )"
                                        : "selectedItemIdInPostBack" ) + "; }",
                                  "true" );
            writeFormItemGetters( writer,
                                  field,
                                  "SelectList<" + field.NullableTypeName + ">",
                                  "DropDown",
                                  field.NullableTypeName,
                                  "null",
                                  new CSharpParameter( "IEnumerable<EwfListItem<" + field.NullableTypeName + ">>", "items" ).ToSingleElementArray()
                                                                                                                            .Concat( nonNullableField
                                                                                                                                         ? new CSharpParameter[
                                                                                                                                               0 ]
                                                                                                                                         : new CSharpParameter(
                                                                                                                                               "string",
                                                                                                                                               "defaultValueItemLabel" )
                                                                                                                                               .ToSingleElementArray
                                                                                                                                               () ),
                                  new CSharpParameter[ 0 ],
                                  new CSharpParameter( "Unit?", "width", "null" ).ToSingleElementArray()
                                                                                 .Concat( ( nonNullableField
                                                                                                ? new CSharpParameter[ 0 ]
                                                                                                : new CSharpParameter( "bool", "placeholderIsValid", "true" )
                                                                                                      .ToSingleElementArray() ) )
                                                                                 .Concat( new[]
                                                                                     {
                                                                                         new CSharpParameter( "string", "placeholderText", "\"Please select\"" ),
                                                                                         new CSharpParameter( "PostBack", "postBack", "null" ),
                                                                                         new CSharpParameter( "bool", "autoPostBack", "false" )
                                                                                     } ),
                                  new CSharpParameter[ 0 ],
                                  "SelectList.CreateDropDown( items, v, width: width, defaultValueItemLabel: " +
                                  ( nonNullableField ? "\"\"" : "defaultValueItemLabel" ) + ", placeholderIsValid: " +
                                  ( nonNullableField ? "false" : "placeholderIsValid" ) +
                                  ", placeholderText: placeholderText, postBack: postBack, autoPostBack: autoPostBack )",
                                  "{ var selectedItemIdInPostBack = control.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator ); return " +
                                  ( nonNullableField
                                        ? "selectedItemIdInPostBack.HasValue ? selectedItemIdInPostBack.Value : default( " + field.TypeName + " )"
                                        : "selectedItemIdInPostBack" ) + "; }",
                                  "true" );
        }

        private static void writeFormItemGetters( TextWriter writer, ModificationField field, string controlType, string controlTypeForName,
                                                  string valueParamTypeName, string valueParamDefaultValue, IEnumerable<CSharpParameter> requiredControlParams,
                                                  IEnumerable<CSharpParameter> requiredValidationParams, IEnumerable<CSharpParameter> optionalControlParams,
                                                  IEnumerable<CSharpParameter> optionalValidationParams, string controlGetterExpressionOrBlock,
                                                  string validationMethodExpressionOrBlock, string putLabelOnFormItemExpression,
                                                  string preFormItemGetterStatements = "", string postFormItemGetterStatements = "",
                                                  IEnumerable<string> additionalSummarySentences = null ) {
            writeFormItemGetterWithoutValueParams( writer,
                                                   controlType,
                                                   field,
                                                   controlTypeForName,
                                                   validationMethodExpressionOrBlock.Any(),
                                                   requiredControlParams,
                                                   requiredValidationParams,
                                                   optionalControlParams,
                                                   optionalValidationParams,
                                                   additionalSummarySentences ?? new string[ 0 ] );
            writeFormItemGetterWithValueParams( writer,
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
                                                putLabelOnFormItemExpression,
                                                postFormItemGetterStatements,
                                                additionalSummarySentences ?? new string[ 0 ] );
        }

        private static void writeFormItemGetterWithoutValueParams( TextWriter writer, string controlType, ModificationField field, string controlTypeForName,
                                                                   bool includeValidationParams, IEnumerable<CSharpParameter> requiredControlParams,
                                                                   IEnumerable<CSharpParameter> requiredValidationParams,
                                                                   IEnumerable<CSharpParameter> optionalControlParams,
                                                                   IEnumerable<CSharpParameter> optionalValidationParams,
                                                                   IEnumerable<string> additionalSummarySentences ) {
            // NOTE: The "out" parameter logic is a hack. We need to improve CSharpParameter.
            var body = "return " + StandardLibraryMethods.GetCSharpIdentifierSimple( "Get" + field.PascalCasedName + controlTypeForName + "FormItem" ) +
                       "( false, " +
                       requiredControlParams.Concat( requiredValidationParams )
                                            .Select( i => ( i.MethodSignatureDeclaration.StartsWith( "out " ) ? "out " : "" ) + i.Name )
                                            .GetCommaDelimitedStringFromCollection()
                                            .AppendDelimiter( ", " ) + "labelAndSubject: labelAndSubject, " +
                       optionalControlParams.Select( i => i.Name + ": " + i.Name ).GetCommaDelimitedStringFromCollection().AppendDelimiter( ", " ) +
                       "cellSpan: cellSpan, textAlignment: textAlignment" + ( includeValidationParams ? ", validationPredicate: validationPredicate" : "" ) +
                       optionalValidationParams.Select( i => i.Name + ": " + i.Name ).GetCommaDelimitedStringFromCollection().PrependDelimiter( ", " ) +
                       ( includeValidationParams
                             ? ", validationErrorNotifier: validationErrorNotifier, additionalValidationMethod: additionalValidationMethod, validationList: validationList"
                             : "" ) + " );";

            writeFormItemGetter( writer,
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

        private static void writeFormItemGetterWithValueParams( TextWriter writer, string controlType, ModificationField field, string controlTypeForName,
                                                                string valueParamTypeName, string valueParamDefaultValue,
                                                                IEnumerable<CSharpParameter> requiredControlParams,
                                                                IEnumerable<CSharpParameter> requiredValidationParams,
                                                                IEnumerable<CSharpParameter> optionalControlParams,
                                                                IEnumerable<CSharpParameter> optionalValidationParams, string preFormItemGetterStatements,
                                                                string controlGetterExpressionOrBlock, string validationMethodExpressionOrBlock,
                                                                string putLabelOnFormItemExpression, string postFormItemGetterStatements,
                                                                IEnumerable<string> additionalSummarySentences ) {
            var validationMethod = "( control, postBackValues, subject, validator ) => " + validationMethodExpressionOrBlock;
            var formItemGetterStatement = "var formItem = " + StandardLibraryMethods.GetCSharpIdentifierSimple( "Get" + field.PascalCasedName + "FormItem" ) +
                                          "( useValueParameter, ( v, ls ) => " + controlGetterExpressionOrBlock +
                                          ( validationMethodExpressionOrBlock.Any() ? ", " + validationMethod : "" ) +
                                          ", labelAndSubject: labelAndSubject, putLabelOnFormItem: " + putLabelOnFormItemExpression +
                                          ", value: value, cellSpan: cellSpan, textAlignment: textAlignment" +
                                          ( validationMethodExpressionOrBlock.Any()
                                                ? ", validationPredicate: validationPredicate, validationErrorNotifier: validationErrorNotifier, additionalValidationMethod: additionalValidationMethod, validationList: validationList"
                                                : "" ) + " );";
            writeFormItemGetter( writer,
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
                                 StringTools.ConcatenateWithDelimiter( Environment.NewLine,
                                                                       preFormItemGetterStatements,
                                                                       formItemGetterStatement,
                                                                       postFormItemGetterStatements,
                                                                       "return formItem;" ),
                                 additionalSummarySentences );
        }

        private static void writeFormItemGetter( TextWriter writer, string controlType, ModificationField field, string controlTypeForName,
                                                 string valueParamTypeName, IEnumerable<CSharpParameter> requiredControlParams,
                                                 IEnumerable<CSharpParameter> requiredValidationParams, string valueParamDefaultValue,
                                                 IEnumerable<CSharpParameter> optionalControlParams, bool includeValidationParams,
                                                 IEnumerable<CSharpParameter> optionalValidationParams, string body,
                                                 IEnumerable<string> additionalSummarySentences ) {
            CodeGenerationStatics.AddSummaryDocComment( writer, getFormItemGetterSummary( field, controlTypeForName, additionalSummarySentences ) );
            CodeGenerationStatics.AddParamDocComment( writer, "additionalValidationMethod", "Passes the labelAndSubject and a validator to the function." );
            CodeGenerationStatics.AddParamDocComment( writer, "validationList", validationListParamDocComment );

            var parameters = new List<CSharpParameter>();
            if( valueParamTypeName.Length > 0 )
                parameters.Add( new CSharpParameter( "bool", "useValueParameter" ) );
            parameters.AddRange( requiredControlParams );
            parameters.AddRange( requiredValidationParams );
            parameters.Add( new CSharpParameter( "string", "labelAndSubject", "\"" + getDefaultLabelAndSubject( field ) + "\"" ) );
            if( valueParamTypeName.Length > 0 )
                parameters.Add( new CSharpParameter( valueParamTypeName, "value", valueParamDefaultValue ) );
            parameters.AddRange( optionalControlParams );
            parameters.Add( new CSharpParameter( "int?", "cellSpan", "null" ) );
            parameters.Add( new CSharpParameter( "TextAlignment", "textAlignment", "TextAlignment.NotSpecified" ) );
            if( includeValidationParams )
                parameters.Add( new CSharpParameter( "System.Func<bool>", "validationPredicate", "null" ) );
            parameters.AddRange( optionalValidationParams );
            if( includeValidationParams ) {
                parameters.Add( new CSharpParameter( "System.Action", "validationErrorNotifier", "null" ) );
                parameters.Add( new CSharpParameter( "System.Action<string,Validator>", "additionalValidationMethod", "null" ) );
                parameters.Add( new CSharpParameter( "ValidationList", "validationList", "null" ) );
            }

            writer.WriteLine( "public FormItem<" + controlType + "> " +
                              StandardLibraryMethods.GetCSharpIdentifierSimple( "Get" + field.PascalCasedName + controlTypeForName + "FormItem" ) + "( " +
                              parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() + " ) {" );
            writer.WriteLine( body );
            writer.WriteLine( "}" );
        }

        private static CSharpParameter getAllowEmptyParameter( bool isOptional ) {
            return new CSharpParameter( "bool", "allowEmpty", isOptional ? "true" : "" );
        }

        private static void writeGenericGetterWithoutValueParams( TextWriter writer, ModificationField field, bool? includeValidationMethodReturnValue ) {
            var controlGetter = "( v, ls ) => controlGetter( v" + ( field.TypeName != field.NullableTypeName ? ".Value" : "" ) + ", ls )";
            var body = "return " + StandardLibraryMethods.GetCSharpIdentifierSimple( "Get" + field.PascalCasedName + "FormItem" ) + "( false, " + controlGetter +
                       ( includeValidationMethodReturnValue.HasValue ? ", validationMethod" : "" ) +
                       ", labelAndSubject: labelAndSubject, putLabelOnFormItem: putLabelOnFormItem, cellSpan: cellSpan, textAlignment: textAlignment" +
                       ( includeValidationMethodReturnValue.HasValue
                             ? ", validationPredicate: validationPredicate, validationErrorNotifier: validationErrorNotifier, additionalValidationMethod: additionalValidationMethod, validationList: validationList"
                             : "" ) + " );";
            writeGenericGetter( writer, field, false, includeValidationMethodReturnValue, body );
        }

        private static void writeGenericGetterWithValueParams( TextWriter writer, ModificationField field, bool? includeValidationMethodReturnValue ) {
            var control = "useValueParameter ? controlGetter( value, labelAndSubject ) : controlGetter( " +
                          StandardLibraryMethods.GetCSharpIdentifierSimple( field.PropertyName ) + ", labelAndSubject )";
            var body = "return FormItem.Create( putLabelOnFormItem ? labelAndSubject : \"\", " + control + ", cellSpan: cellSpan, textAlignment: textAlignment" +
                       ( includeValidationMethodReturnValue.HasValue
                             ? ", validationGetter: " + getValidationGetter( field, includeValidationMethodReturnValue.Value )
                             : "" ) + " );";
            writeGenericGetter( writer, field, true, includeValidationMethodReturnValue, body );
        }

        private static string getValidationGetter( ModificationField field, bool includeValidationMethodReturnValue ) {
            var fieldPropertyName = StandardLibraryMethods.GetCSharpIdentifierSimple( field.PropertyName );
            var statements = new[]
                {
                    "if( validationPredicate != null && !validationPredicate() ) return;",
                    ( includeValidationMethodReturnValue ? fieldPropertyName + " = " : "" ) +
                    "validationMethod( control, postBackValues, labelAndSubject, validator );",
                    "if( validator.ErrorsOccurred && validationErrorNotifier != null ) validationErrorNotifier();",
                    "if( !validator.ErrorsOccurred && additionalValidationMethod != null ) additionalValidationMethod( labelAndSubject, validator );"
                };
            return "control => new Validation( ( postBackValues, validator ) => { " + StringTools.ConcatenateWithDelimiter( " ", statements ) +
                   " }, validationList ?? EwfPage.Instance.DataUpdate )";
        }

        private static void writeGenericGetter( TextWriter writer, ModificationField field, bool includeValueParams, bool? includeValidationMethodReturnValue,
                                                string body ) {
            CodeGenerationStatics.AddSummaryDocComment( writer, getFormItemGetterSummary( field, "", new string[ 0 ] ) );
            CodeGenerationStatics.AddParamDocComment( writer, "additionalValidationMethod", "Passes the labelAndSubject and a validator to the function." );
            CodeGenerationStatics.AddParamDocComment( writer, "validationList", validationListParamDocComment );

            var parameters = new List<CSharpParameter>();
            if( includeValueParams )
                parameters.Add( new CSharpParameter( "bool", "useValueParameter" ) );
            parameters.Add( new CSharpParameter( "System.Func<" + ( includeValueParams ? field.NullableTypeName : field.TypeName ) + ",string,ControlType>",
                                                 "controlGetter" ) );
            if( includeValidationMethodReturnValue.HasValue ) {
                parameters.Add( includeValidationMethodReturnValue.Value
                                    ? new CSharpParameter( "System.Func<ControlType,PostBackValueDictionary,string,Validator," + field.TypeName + ">",
                                                           "validationMethod" )
                                    : new CSharpParameter( "System.Action<ControlType,PostBackValueDictionary,string,Validator>", "validationMethod" ) );
            }
            parameters.Add( new CSharpParameter( "string", "labelAndSubject", "\"" + getDefaultLabelAndSubject( field ) + "\"" ) );
            parameters.Add( new CSharpParameter( "bool", "putLabelOnFormItem", "true" ) );
            if( includeValueParams )
                parameters.Add( new CSharpParameter( field.NullableTypeName, "value", field.TypeIs( typeof( string ) ) ? "\"\"" : "null" ) );
            parameters.Add( new CSharpParameter( "int?", "cellSpan", "null" ) );
            parameters.Add( new CSharpParameter( "TextAlignment", "textAlignment", "TextAlignment.NotSpecified" ) );
            if( includeValidationMethodReturnValue.HasValue ) {
                parameters.Add( new CSharpParameter( "System.Func<bool>", "validationPredicate", "null" ) );
                parameters.Add( new CSharpParameter( "System.Action", "validationErrorNotifier", "null" ) );
                parameters.Add( new CSharpParameter( "System.Action<string,Validator>", "additionalValidationMethod", "null" ) );
                parameters.Add( new CSharpParameter( "ValidationList", "validationList", "null" ) );
            }

            writer.WriteLine( "public FormItem<ControlType> " + StandardLibraryMethods.GetCSharpIdentifierSimple( "Get" + field.PascalCasedName + "FormItem" ) +
                              "<ControlType>( " + parameters.Select( i => i.MethodSignatureDeclaration ).GetCommaDelimitedStringFromCollection() +
                              " ) where ControlType: Control {" );
            writer.WriteLine( body );
            writer.WriteLine( "}" );
        }

        private static string getFormItemGetterSummary( ModificationField field, string controlType, IEnumerable<string> additionalSentences ) {
            var sentences = new[]
                {
                    "Creates a " + field.PropertyName + controlType.PrependDelimiter( " " ) + " form item, which includes a label, a control, and a validation.",
                    controlType.Any()
                        ? ""
                        : "This is a generic form-item getter; use it only if there is no specific getter for the control type that you need.",
                    "You almost certainly should not call this method from a deferred block of code since this could cause validations to be added to the data modification in the wrong order."
                };
            return StringTools.ConcatenateWithDelimiter( " ", sentences.Concat( additionalSentences ).ToArray() );
        }

        private static string getDefaultLabelAndSubject( ModificationField field ) {
            var result = field.PascalCasedName.CamelToEnglish();
            if( result.ToLower().EndsWith( " id" ) )
                result = result.Substring( 0, result.Length - 3 );
            return result;
        }
    }
}