using System.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration {
	internal static class CodeGenerationStatics {
		internal static void AddSummaryDocComment( TextWriter writer, string text ) {
			if( text.Length == 0 )
				return;
			text = text.Replace( writer.NewLine, writer.NewLine + "/// " );
			writer.WriteLine( "/// <summary>" );
			writer.WriteLine( "/// " + text );
			writer.WriteLine( "/// </summary>" );
		}

		internal static void AddParamDocComment( TextWriter writer, string name, string description ) {
			writer.WriteLine( "/// <param name=\"" + name + "\">" + description + "</param>" );
		}

		internal static void AddGeneratedCodeUseOnlyComment( TextWriter writer ) {
			AddSummaryDocComment( writer, "Auto-generated code use only." );
		}
	}
}