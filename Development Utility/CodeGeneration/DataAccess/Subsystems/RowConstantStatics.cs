using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;

internal static class RowConstantStatics {
	private const string dictionaryName = "valuesAndNames";

	private record Row( string Value, string Name, string Identifier );

	internal static void Generate(
		DatabaseConnection cn, TextWriter writer, string baseNamespace, Database database,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		if( configuration.rowConstantTables == null )
			return;

		writer.WriteLine( "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "RowConstants {" );
		foreach( var table in configuration.rowConstantTables ) {
			Column valueColumn;
			var orderIsSpecified = !table.orderByColumn.IsNullOrWhiteSpace();
			var rows = new List<Row>();
			var duplicatesExist = false;
			try {
				var columns = new TableColumns( cn, table.tableName, false );
				valueColumn = columns.AllColumnsExceptRowVersion.Single( column => column.Name.ToLower() == table.valueColumn.ToLower() );
				var nameColumn = columns.AllColumnsExceptRowVersion.Single( column => column.Name.ToLower() == table.nameColumn.ToLower() );

				var cmd = new InlineSelect( [ "*" ], $"FROM {table.tableName}", false, orderByClause: orderIsSpecified ? $"ORDER BY {table.orderByColumn}" : "" );
				cmd.Execute(
					cn,
					reader => {
						while( reader.Read() ) {
							var identifierName = nameColumn.GetDataReaderValue( reader, forIdentifier: true );
							var row = new Row(
								valueColumn.GetDataReaderValue( reader ),
								nameColumn.GetDataReaderValue( reader ),
								EwlStatics.GetCSharpIdentifier( isPascalCase( identifierName ) ? identifierName : identifierName.EnglishToPascal() ) );

							if( rows.Any( i =>
								   i.Value.Equals( row.Value, StringComparison.Ordinal ) && i.Name.Equals( row.Name, StringComparison.Ordinal ) &&
								   i.Identifier.Equals( row.Identifier, StringComparison.Ordinal ) ) )
								duplicatesExist = true;
							else
								rows.Add( row );
						}
					} );
			}
			catch( Exception e ) {
				throw new UserCorrectableException(
					"Column or data retrieval failed for the " + table.tableName +
					" row constant table. Make sure the table and the value, name, and order by columns exist.",
					e );
			}

			var pascalTableName = table.tableName.TableNameToPascal( cn );
			string className;
			if( duplicatesExist ) {
				var singularTableName = pascalTableName.Singularize( inputIsKnownToBePlural: false );

				className = pascalTableName;
				var valueColumnName = valueColumn.PascalCasedName;
				for( var duplicateSubstringLength = valueColumnName.Length; duplicateSubstringLength > 0; duplicateSubstringLength -= 1 ) {
					if( !singularTableName.EndsWith( valueColumnName[ ..duplicateSubstringLength ], StringComparison.Ordinal ) )
						continue;

					className += valueColumnName[ duplicateSubstringLength.. ];
					break;
				}

				className = className.Pluralize( inputIsKnownToBeSingular: false );
			}
			else
				// Consider singularizing the table name here, too.
				className = pascalTableName + "Rows";

			CodeGenerationStatics.AddSummaryDocComment( writer, "Provides constants copied from the " + table.tableName + " table." );
			writer.WriteLine( "public class " + className + " {" );

			// constants
			foreach( var row in rows ) {
				// It’s important that row constants actually *be* constants when possible (instead of static readonly) so they can be used in switch statements.
				var prefix = row.Value.StartsWith( "new ", StringComparison.Ordinal ) ? "static readonly" : "const";

				CodeGenerationStatics.AddSummaryDocComment( writer, "Constant generated from row in database table." );
				writer.WriteLine( $"public {prefix} {valueColumn.DataTypeName} {row.Identifier} = {row.Value};" );
			}

			// one to one map
			var dictionaryType = "OneToOneMap<" + valueColumn.DataTypeName + ", string>";
			writer.WriteLine( "private static readonly " + dictionaryType + " " + dictionaryName + " = new " + dictionaryType + "();" );

			writeStaticConstructor( writer, className, rows );

			// methods
			writeGetNameFromValueMethod( writer, valueColumn.DataTypeName );
			writeGetValueFromNameMethod( writer, valueColumn.DataTypeName );
			if( orderIsSpecified ) {
				writeGetValuesToNamesMethod( writer, valueColumn.DataTypeName );
				writeFillListControlMethod( writer, valueColumn );
			}

			writer.WriteLine( "}" ); // class
		}
		writer.WriteLine( "}" ); // namespace
	}

	private static bool isPascalCase( string text ) =>
		text.Any( char.IsLower ) && text.RemoveNonAlphanumericCharacters( preserveWhiteSpace: false ).Equals( text, StringComparison.Ordinal );

	private static void writeStaticConstructor( TextWriter writer, string className, IEnumerable<Row> rows ) {
		writer.WriteLine( "static " + className + "() {" );

		foreach( var row in rows )
			writer.WriteLine( $"{dictionaryName}.Add( {row.Identifier}, {row.Name} );" );

		writer.WriteLine( "}" ); // constructor
	}

	private static void writeGetNameFromValueMethod( TextWriter writer, string valueTypeName ) {
		CodeGenerationStatics.AddSummaryDocComment( writer, "Returns the name of the constant given the constant's value." );
		const string parameterName = "constantValue";
		writer.WriteLine(
			"public static string GetNameFromValue( " + valueTypeName + " " + parameterName + " ) => " + dictionaryName + ".GetRightFromLeft( " + parameterName +
			" );" );
	}

	private static void writeGetValueFromNameMethod( TextWriter writer, string valueTypeName ) {
		CodeGenerationStatics.AddSummaryDocComment( writer, "Returns the value of the constant given the constant's name." );
		const string parameterName = "constantName";
		writer.WriteLine(
			"public static " + valueTypeName + " GetValueFromName( string " + parameterName + " ) => " + dictionaryName + ".GetLeftFromRight( " + parameterName +
			" );" );
	}

	private static void writeGetValuesToNamesMethod( TextWriter writer, string valueTypeName ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Returns a list of key value pairs where the key is the value of the row constant and the value is the name of the row constant." );
		writer.WriteLine( "public static ICollection<KeyValuePair<" + valueTypeName + ", string>> GetValuesToNames() => valuesAndNames.GetAllPairs();" );
	}

	private static void writeFillListControlMethod( TextWriter writer, Column valueColumn ) {
		writer.WriteLine(
			"public static IEnumerable<SelectListItem<" + valueColumn.NullableDataTypeName +
			">> GetListItems() => from i in valuesAndNames.GetAllPairs() select SelectListItem.Create<" + valueColumn.NullableDataTypeName + ">( i.Key, i.Value );" );
	}
}