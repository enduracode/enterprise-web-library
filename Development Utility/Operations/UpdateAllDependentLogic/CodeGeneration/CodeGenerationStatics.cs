using System;
using System.IO;
using System.Linq;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
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
			if( description.Length == 0 )
				return;
			writer.WriteLine( "/// <param name=\"" + name + "\">" + description + "</param>" );
		}

		internal static void AddGeneratedCodeUseOnlyComment( TextWriter writer ) {
			AddSummaryDocComment( writer, "Auto-generated code use only." );
		}

		internal static string GetCSharpSafeClassName( string desiredClassName ) {
			desiredClassName = desiredClassName.Replace( ' ', '_' );
			if( Char.IsDigit( desiredClassName.First() ) )
				return "_" + desiredClassName;
			return desiredClassName;
		}
	}
}