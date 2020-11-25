using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Humanizer;
using Tewl.IO;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration {
	internal static class TypedCssClassStatics {
		internal static void Generate( string rootPath, string nameSpace, TextWriter writer ) {
			var cssClasses = new HashSet<string>();
			foreach( var fileInfo in new DirectoryInfo( rootPath ).GetFiles( "*.css", SearchOption.AllDirectories ) ) {
				new FileReader( fileInfo.FullName ).ExecuteInStreamReader(
					delegate( StreamReader reader ) {
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
				writer.WriteLine( "public class ElementClasses {" );

				var identifiers = new HashSet<string>();
				foreach( var elementClass in cssClasses ) {
					writer.WriteLine( "/// <summary>" );
					writer.WriteLine( "/// Constant for the '{0}' class.".FormatWith( elementClass ) );
					writer.WriteLine( "/// </summary>" );
					var identifier = EwlStatics.GetCSharpIdentifier( elementClass.CapitalizeString() );
					if( identifiers.Contains( identifier ) ) {
						var uniqueIdentifier = identifier;
						var i = 0;
						while( identifiers.Contains( uniqueIdentifier ) )
							uniqueIdentifier = identifier + i++;
						identifier = uniqueIdentifier;
					}
					identifiers.Add( identifier );
					writer.WriteLine( "public static readonly ElementClass {0} = new ElementClass( \"{1}\" );".FormatWith( identifier, elementClass ) );
				}

				writer.WriteLine( "}" ); // class
				writer.WriteLine( "}" ); // namespace
			}
		}
	}
}