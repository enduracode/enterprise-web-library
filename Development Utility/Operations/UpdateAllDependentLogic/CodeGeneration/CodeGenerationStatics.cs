using System;
using System.IO;
using RedStapler.StandardLibrary;

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
			// GMS: There may be a better way to see if something starts with a number, or to more general ask if something is an acceptable class name.
			int dummyInt;
			if( Int32.TryParse( desiredClassName.Truncate( 1 ), out dummyInt ) )
				return "_" + desiredClassName;

			return desiredClassName;
		}
	}
}
