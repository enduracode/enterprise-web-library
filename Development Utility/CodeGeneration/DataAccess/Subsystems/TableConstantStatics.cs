using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;

internal static class TableConstantStatics {
	internal static void Generate( DatabaseConnection cn, TextWriter writer, string baseNamespace, Database database, IEnumerable<DatabaseTable> tables ) {
		foreach( var table in tables ) {
			writer.WriteLine(
				$$"""namespace {{baseNamespace}}.{{database.SecondaryDatabaseName}}TableConstants{{DataAccessStatics.GetSchemaNamespaceSuffix( database, table )}} {""" );

			CodeGenerationStatics.AddSummaryDocComment( writer, "This object represents the constants of the " + table.QualifiedName + " table." );
			writer.WriteLine( "public class " + EwlStatics.GetCSharpIdentifier( table.Name.TableNameToPascal( cn ) + "Table" ) + " {" );

			CodeGenerationStatics.AddSummaryDocComment( writer, "The name of this table." );
			writer.WriteLine( "public const string Name = \"" + table.QualifiedName + "\";" );

			foreach( var column in new TableColumns( cn, table, false ).AllColumnsExceptRowVersion ) {
				CodeGenerationStatics.AddSummaryDocComment( writer, "Contains schema information about this column." );
				writer.WriteLine( "public class " + EwlStatics.GetCSharpIdentifier( column.PascalCasedName + "Column" ) + " {" );

				CodeGenerationStatics.AddSummaryDocComment( writer, "The name of this column." );
				writer.WriteLine( "public const string Name = \"" + column.Name + "\";" );

				CodeGenerationStatics.AddSummaryDocComment(
					writer,
					"The size of this column. For varchars, this is the length of the biggest string that can be stored in this column." );
				writer.WriteLine( "public const int Size = " + column.Size + ";" );

				writer.WriteLine( "}" );
			}

			writer.WriteLine( "}" );

			writer.WriteLine( "}" );
		}
	}
}