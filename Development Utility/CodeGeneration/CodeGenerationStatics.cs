using Microsoft.CodeAnalysis.CSharp;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration;

internal static class CodeGenerationStatics {
	public static void AddSummaryDocComment( TextWriter writer, string text ) {
		if( text.Length == 0 )
			return;
		text = text.Replace( writer.NewLine, writer.NewLine + "/// " );
		writer.WriteLine( "/// <summary>" );
		writer.WriteLine( "/// " + text );
		writer.WriteLine( "/// </summary>" );
	}

	public static void AddParamDocComment( TextWriter writer, string name, string description ) {
		writer.WriteLine( "/// <param name=\"" + name + "\">" + description + "</param>" );
	}

	public static void AddGeneratedCodeUseOnlyComment( TextWriter writer ) {
		AddSummaryDocComment( writer, "Auto-generated code use only." );
	}

	public static string EscapeForLiteral( this string value ) => SymbolDisplay.FormatLiteral( value, true )[ 1..^1 ];
}