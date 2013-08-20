using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal static class TypedCssClassStatics {
		internal static void Generate( string rootPath, string nameSpace, TextWriter writer ) {
			var cssClasses = new HashSet<string>();
			foreach( var fileInfo in new DirectoryInfo( rootPath ).GetFiles( "*.css", SearchOption.AllDirectories ) ) {
				new FileReader( fileInfo.FullName ).ExecuteInStreamReader( delegate( StreamReader reader ) {
					// Remove comments and styles.
					// NOTE: We need to find a way to also throw out media query expressions.
					var text = reader.ReadToEnd().RemoveTextBetweenStrings( "/*", "*/" ).RemoveTextBetweenStrings( "{", "}" );

					foreach( Match match in Regex.Matches( text, @"\.(\w+)" ) )
						cssClasses.Add( match.Groups[ 1 ].Value );
				} );
			}

			if( cssClasses.Any() ) {
				writer.WriteLine( "namespace " + nameSpace + " {" );

				CodeGenerationStatics.AddSummaryDocComment( writer, "This class provides typesafe access to css classes present in *.css files." );
				writer.WriteLine( "public class CssClasses {" );
				foreach( var cssClass in cssClasses )
					writer.WriteLine( "public const string " + StandardLibraryMethods.GetCSharpIdentifierSimple( cssClass ).CapitalizeString() + " = \"" + cssClass + "\";" );

				writer.WriteLine( "}" ); // class
				writer.WriteLine( "}" ); // namespace
			}
		}
	}
}