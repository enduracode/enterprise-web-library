using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;

static class CommandConditionStatics {
	internal static void Generate( DatabaseConnection cn, TextWriter writer, string baseNamespace, Database database, IEnumerable<DatabaseTable> tables ) {
		foreach( var table in tables ) {
			writer.WriteLine(
				$$"""namespace {{baseNamespace}}.{{database.SecondaryDatabaseName}}CommandConditions{{DataAccessStatics.GetSchemaNamespaceSuffix( database, table )}} {""" );

			// Write the interface for all of the table's conditions.
			writer.WriteLine( "public interface " + GetTableConditionInterfaceName( cn, table.Name ) + ": TableCondition {}" );

			writeEqualityConditionClasses( writer, cn, database, table );
			writeInequalityConditionClasses( writer, cn, database, table );
			writeInConditionClasses( writer, cn, database, table );
			writeLikeConditionClasses( writer, cn, database, table );

			writer.WriteLine( "}" ); // namespace
		}
	}

	private static void writeEqualityConditionClasses( TextWriter writer, DatabaseConnection cn, Database database, DatabaseTable table ) {
		writer.WriteLine( "public static class " + GetTableEqualityConditionsClassName( cn, table.Name ) + " {" );
		foreach( var column in new TableColumns( cn, database, table, false ).AllColumnsExceptRowVersion ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "A condition that narrows the scope of a command." );
			writer.WriteLine( "public class " + GetConditionClassName( column ) + ": " + GetTableConditionInterfaceName( cn, table.Name ) + " {" );
			writer.WriteLine( "private readonly " + column.DataTypeName + " value;" );

			CodeGenerationStatics.AddSummaryDocComment( writer, "Creates a condition to narrow the scope of a command." );
			writer.WriteLine( "public " + GetConditionClassName( column ) + "( " + column.DataTypeName + " value ) {" );
			writer.WriteLine( "this.value = value;" );
			writer.WriteLine( "}" );

			writer.WriteLine( "internal " + column.DataTypeName + " Value { get { return value; } }" );
			var colVal = column.GetCommandColumnValueExpression( "value" );
			writer.WriteLine( "InlineDbCommandCondition TableCondition.CommandCondition { get { return new EqualityCondition( " + colVal + " ); } }" );
			writer.WriteLine( "}" );
		}
		writer.WriteLine( "}" ); // class
	}

	internal static string GetTableEqualityConditionsClassName( DatabaseConnection cn, string table ) {
		return EwlStatics.GetCSharpIdentifier( table.TableNameToPascal( cn ) + "TableEqualityConditions" );
	}

	private static void writeInequalityConditionClasses( TextWriter writer, DatabaseConnection cn, Database database, DatabaseTable table ) {
		// NOTE: This kind of sucks. It seems like we could use generics to not have to write N of these methods into ISU.cs.
		writer.WriteLine( "public static class " + EwlStatics.GetCSharpIdentifier( table.Name.TableNameToPascal( cn ) + "TableInequalityConditions" ) + " {" );
		foreach( var column in new TableColumns( cn, database, table, false ).AllColumnsExceptRowVersion ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "A condition that narrows the scope of a command." );
			writer.WriteLine( "public class " + GetConditionClassName( column ) + ": " + GetTableConditionInterfaceName( cn, table.Name ) + " {" );
			writer.WriteLine( "private readonly InequalityCondition.Operator op; " );
			writer.WriteLine( "private readonly " + column.DataTypeName + " value;" );

			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Creates a condition to narrow the scope of a command. Expression will read 'valueInDatabase op yourValue'. So new InequalityCondition( Operator.GreaterThan, value ) will turn into 'columnName > @value'." );
			writer.WriteLine( "public " + GetConditionClassName( column ) + "( InequalityCondition.Operator op, " + column.DataTypeName + " value ) {" );
			writer.WriteLine( "this.op = op;" );
			writer.WriteLine( "this.value = value;" );
			writer.WriteLine( "}" );

			var colVal = column.GetCommandColumnValueExpression( "value" );
			writer.WriteLine( "InlineDbCommandCondition TableCondition.CommandCondition { get { return new InequalityCondition( op, " + colVal + " ); } }" );
			writer.WriteLine( "}" );
		}
		writer.WriteLine( "}" ); // class
	}

	private static void writeInConditionClasses( TextWriter writer, DatabaseConnection cn, Database database, DatabaseTable table ) {
		writer.WriteLine( "public static class " + EwlStatics.GetCSharpIdentifier( table.Name.TableNameToPascal( cn ) + "TableInConditions" ) + " {" );
		foreach( var column in new TableColumns( cn, database, table, false ).AllColumnsExceptRowVersion ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "A condition that narrows the scope of a command." );
			writer.WriteLine( "public class " + GetConditionClassName( column ) + ": " + GetTableConditionInterfaceName( cn, table.Name ) + " {" );
			writer.WriteLine( "private readonly string subQuery;" );

			CodeGenerationStatics.AddSummaryDocComment( writer, "Creates a condition to narrow the scope of a command." );
			writer.WriteLine( "public " + GetConditionClassName( column ) + "( string subQuery ) {" );
			writer.WriteLine( "this.subQuery = subQuery;" );
			writer.WriteLine( "}" );

			writer.WriteLine( "InlineDbCommandCondition TableCondition.CommandCondition { get { return new InCondition( \"" + column.Name + "\", subQuery ); } }" );

			writer.WriteLine( "}" );
		}
		writer.WriteLine( "}" );
	}

	private static void writeLikeConditionClasses( TextWriter writer, DatabaseConnection cn, Database database, DatabaseTable table ) {
		writer.WriteLine( "public static class " + EwlStatics.GetCSharpIdentifier( table.Name.TableNameToPascal( cn ) + "TableLikeConditions" ) + " {" );
		foreach( var column in new TableColumns( cn, database, table, false ).AllColumnsExceptRowVersion ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "A condition that narrows the scope of a command." );
			writer.WriteLine( "public class " + GetConditionClassName( column ) + ": " + GetTableConditionInterfaceName( cn, table.Name ) + " {" );
			writer.WriteLine( "private readonly LikeCondition.Behavior behavior; " );
			writer.WriteLine( "private readonly string value;" );

			CodeGenerationStatics.AddSummaryDocComment( writer, "Creates a condition to narrow the scope of a command." );
			writer.WriteLine( "public " + GetConditionClassName( column ) + "( LikeCondition.Behavior behavior, string value ) {" );
			writer.WriteLine( "this.behavior = behavior;" );
			writer.WriteLine( "this.value = value;" );
			writer.WriteLine( "}" );

			writer.WriteLine(
				"InlineDbCommandCondition TableCondition.CommandCondition { get { return new LikeCondition( behavior, \"" + column.Name + "\", value ); } }" );

			writer.WriteLine( "}" ); // class
		}
		writer.WriteLine( "}" ); // class
	}

	internal static string GetTableConditionInterfaceName( DatabaseConnection cn, string tableName ) => tableName.TableNameToPascal( cn ) + "TableCondition";

	internal static string GetConditionClassName( Column column ) =>
		EwlStatics.GetCSharpIdentifier( column.PascalCasedName == "Value" ? "_Value" : column.PascalCasedName );
}