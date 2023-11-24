﻿namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

internal static class RetrievalStatics {
	internal static void WriteRowClasses(
		TextWriter writer, IReadOnlyCollection<Column> columns, IReadOnlyCollection<Column>? keyColumns, Action<TextWriter> transactionPropertyWriter,
		Action<TextWriter> toModificationMethodWriter ) {
		// BasicRow

		writer.WriteLine(
			"internal class BasicRow{0} {{".FormatWith( keyColumns is null ? "" : ": TableRetrievalRow<{0}>".FormatWith( GetColumnTupleTypeName( keyColumns ) ) ) );
		foreach( var column in columns.Where( i => !i.IsRowVersion ) )
			writer.WriteLine( "private readonly " + column.DataTypeName + " " + getMemberVariableName( column ) + ";" );

		writer.WriteLine( "internal BasicRow( DbDataReader reader ) {" );
		foreach( var column in columns.Where( i => !i.IsRowVersion ) )
			writer.WriteLine( "{0} = {1};".FormatWith( getMemberVariableName( column ), column.GetDataReaderValueExpression( "reader" ) ) );
		writer.WriteLine( "}" );

		foreach( var column in columns.Where( i => !i.IsRowVersion ) )
			writer.WriteLine(
				"public {0} {1} => {2};".FormatWith( column.DataTypeName, EwlStatics.GetCSharpIdentifier( column.PascalCasedName ), getMemberVariableName( column ) ) );

		if( keyColumns is not null )
			writer.WriteLine(
				"{0} TableRetrievalRow<{0}>.PrimaryKey => {1};".FormatWith(
					GetColumnTupleTypeName( keyColumns ),
					keyColumns.Count < 2
						? getMemberVariableName( keyColumns.Single() )
						: "( {0} )".FormatWith( StringTools.ConcatenateWithDelimiter( ", ", keyColumns.Select( getMemberVariableName ) ) ) ) );

		writer.WriteLine( "}" );


		// Row

		CodeGenerationStatics.AddSummaryDocComment( writer, "Holds data for a row of this result." );
		writer.WriteLine( "public partial class Row: System.IEquatable<Row> {" );
		writer.WriteLine( "private readonly BasicRow __basicRow;" );

		writer.WriteLine( "internal Row( BasicRow basicRow ) {" );
		writer.WriteLine( "__basicRow = basicRow;" );
		writer.WriteLine( "}" );

		foreach( var column in columns.Where( i => !i.IsRowVersion ) )
			writeColumnProperty( writer, column );

		// NOTE: Being smarter about the hash code could make searches of the collection faster.
		writer.WriteLine( "public override int GetHashCode() { " );
		// NOTE: Catch an exception generated by not having any uniquely identifying columns and rethrow it as a UserCorrectableException.
		writer.WriteLine(
			"return " + EwlStatics.GetCSharpIdentifier( columns.First( c => c.UseToUniquelyIdentifyRow ).PascalCasedNameExceptForOracle ) + ".GetHashCode();" );
		writer.WriteLine( "}" ); // Object override of GetHashCode

		writer.WriteLine(
			@"	public static bool operator == ( Row row1, Row row2 ) {
				return Equals( row1, row2 );
			}

			public static bool operator !=( Row row1, Row row2 ) {
				return !Equals( row1, row2 );
			}" );

		writer.WriteLine( "public override bool Equals( object obj ) {" );
		writer.WriteLine( "return Equals( obj as Row );" );
		writer.WriteLine( "}" ); // Object override of Equals

		writer.WriteLine( "public bool Equals( Row other ) {" );
		writer.WriteLine( "if( other == null ) return false;" );

		var condition = "";
		foreach( var column in columns.Where( c => c.UseToUniquelyIdentifyRow ) )
			condition = StringTools.ConcatenateWithDelimiter(
				" && ",
				condition,
				EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) + " == other." +
				EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) );
		writer.WriteLine( "return " + condition + ";" );
		writer.WriteLine( "}" ); // Equals method

		transactionPropertyWriter( writer );
		toModificationMethodWriter( writer );

		writer.WriteLine( "}" ); // class
	}

	private static string getMemberVariableName( Column column ) =>
		// A single underscore is a pretty common thing for other code generators and even some developers to use, so two is more unique and avoids problems.
		EwlStatics.GetCSharpIdentifier( "__" + column.CamelCasedName );

	private static void writeColumnProperty( TextWriter writer, Column column ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"This object will " + ( column.AllowsNull && !column.NullValueExpression.Any() ? "sometimes" : "never" ) + " be null." );
		writer.WriteLine(
			"public " + column.DataTypeName + " " + EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) + " { get { return __basicRow." +
			EwlStatics.GetCSharpIdentifier( column.PascalCasedName ) + "; } }" );
	}

	public static string GetColumnTupleTypeName( IReadOnlyCollection<Column> columns ) =>
		columns.Count < 2
			? columns.Single().DataTypeName
			: "( {0} )".FormatWith(
				StringTools.ConcatenateWithDelimiter(
					", ",
					columns.Select( i => "{0} {1}".FormatWith( i.DataTypeName, EwlStatics.GetCSharpIdentifier( i.CamelCasedName ) ) ) ) );
}