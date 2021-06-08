using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework.WebItems;
using Humanizer;
using Tewl.IO;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework {
	internal static class WebFrameworkStatics {
		private static StringBuilder legacyUrlStatics;
		private static List<string> aspxFilePaths;

		internal static void Generate(
			TextWriter writer, string projectPath, string projectNamespace, bool projectContainsFramework, string generatedCodeFolderPath,
			string staticFilesFolderPath, string staticFilesFolderUrlParentExpression ) {
			var legacyUrlStaticsFilePath = EwlStatics.CombinePaths( projectPath, "LegacyUrlStatics.cs" );
			if( File.Exists( legacyUrlStaticsFilePath ) && File.ReadAllText( legacyUrlStaticsFilePath ).Length == 0 ) {
				legacyUrlStatics = new StringBuilder();
				aspxFilePaths = new List<string>();
			}

			generateForFolder(
				writer,
				projectPath,
				projectNamespace,
				projectContainsFramework,
				generatedCodeFolderPath,
				staticFilesFolderPath,
				staticFilesFolderUrlParentExpression,
				"" );

			if( legacyUrlStatics != null ) {
				File.WriteAllText( legacyUrlStaticsFilePath, legacyUrlStatics.ToString(), Encoding.UTF8 );
				foreach( var i in aspxFilePaths )
					File.Delete( i );
			}
		}

		private static void generateForFolder(
			TextWriter writer, string projectPath, string projectNamespace, bool projectContainsFramework, string generatedCodeFolderPath,
			string staticFilesFolderPath, string staticFilesFolderUrlParentExpression, string folderPathRelativeToProject ) {
			if( folderPathRelativeToProject == generatedCodeFolderPath )
				return;

			if( folderPathRelativeToProject == staticFilesFolderPath ) {
				generateStaticFileLogic(
					writer,
					projectPath,
					projectNamespace,
					projectContainsFramework,
					null,
					folderPathRelativeToProject,
					staticFilesFolderUrlParentExpression );
				return;
			}

			var folderPath = EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject );

			// Generate code for the entity setup if one exists in this folder.
			var entitySetupFileName = "";
			foreach( var fileName in new[] { "EntitySetup.cs" } )
				if( File.Exists( EwlStatics.CombinePaths( folderPath, fileName ) ) ) {
					entitySetupFileName = fileName;
					break;
				}
			EntitySetup entitySetup = null;
			if( entitySetupFileName.Length > 0 ) {
				var filePathRelativeToProject = Path.Combine( folderPathRelativeToProject, entitySetupFileName );
				entitySetup = new EntitySetup( projectContainsFramework, new WebItemGeneralData( projectPath, projectNamespace, filePathRelativeToProject, false ) );
				entitySetup.GenerateCode( writer );
			}

			if( legacyUrlStatics != null ) {
				var files = new List<WebItemGeneralData>();
				foreach( var fileName in IoMethods.GetFileNamesInFolder( folderPath, searchPattern: "*.aspx" ).OrderBy( i => i ) ) {
					var filePath = EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject, fileName );

					var aspxLines = File.ReadAllLines( filePath );
					if( aspxLines.Length != 1 || !Regex.IsMatch( aspxLines[ 0 ], "^<%@ .+ %>$" ) )
						throw new Exception( "Invalid ASPX file: \"{0}\"".FormatWith( EwlStatics.CombinePaths( folderPathRelativeToProject, fileName ) ) );

					var newCsLines = new List<string>();
					var pageNeeded = true;
					foreach( var line in File.ReadAllLines( EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject, fileName + ".cs" ) ) ) {
						if( pageNeeded && !line.StartsWith( "using " ) ) {
							newCsLines.Add( "" );
							newCsLines.Add( "// EwlPage" );
							pageNeeded = false;
						}
						newCsLines.Add( line.Replace( ": EwfPage {", " {" ).Replace( ": EwfPage,", ":" ) );
					}
					var newCsFilePath = EwlStatics.CombinePaths( folderPathRelativeToProject, Path.GetFileNameWithoutExtension( fileName ) + ".cs" );
					File.WriteAllText(
						EwlStatics.CombinePaths( projectPath, newCsFilePath ),
						newCsLines.Aggregate( ( text, line ) => text + Environment.NewLine + line ),
						Encoding.UTF8 );
					files.Add( new WebItemGeneralData( projectPath, projectNamespace, newCsFilePath, false ) );

					aspxFilePaths.Add( filePath );
					aspxFilePaths.Add( filePath + ".cs" );
					aspxFilePaths.Add( filePath + ".designer.cs" );
				}

				const string folderSetupClassName = "LegacyUrlFolderSetup";
				var childPatterns = files.Select(
						file =>
							"new UrlPattern( encoder => encoder is {0}.UrlEncoder ? EncodingUrlSegment.Create( {1} ) : null, url => string.Equals( url.Segment, {1}, StringComparison.OrdinalIgnoreCase ) ? new {0}.UrlDecoder() : null )"
								.FormatWith( file.ClassName, "\"{0}.aspx\"".FormatWith( Path.GetFileNameWithoutExtension( file.FileName ) ) ) )
					.Concat(
						IoMethods.GetFolderNamesInFolder( folderPath )
							.Where(
								subfolderName => {
									var subfolderPath = EwlStatics.CombinePaths( folderPathRelativeToProject, subfolderName );
									if( subfolderPath == "bin" || subfolderPath == "obj" )
										return false;

									bool folderContainsAspxFiles( string path ) =>
										IoMethods.GetFileNamesInFolder( path, searchPattern: "*.aspx" ).Any() || IoMethods.GetFolderNamesInFolder( path )
											.Any( i => folderContainsAspxFiles( EwlStatics.CombinePaths( path, i ) ) );
									return folderContainsAspxFiles( EwlStatics.CombinePaths( projectPath, subfolderPath ) );
								} )
							.Select(
								subfolderName => "{0}.{1}.UrlPatterns.Literal( \"{2}\" )".FormatWith(
									WebItemGeneralData.GetNamespaceFromPath( projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, subfolderName ), false )
										.Separate( ".", false )
										.Last(),
									folderSetupClassName,
									subfolderName ) ) )
					.Materialize();

				if( folderPathRelativeToProject.Length == 0 ) {
					legacyUrlStatics.AppendLine( "using System;" );
					legacyUrlStatics.AppendLine( "using System.Collections.Generic;" );
					legacyUrlStatics.AppendLine( "using EnterpriseWebLibrary.EnterpriseWebFramework;" );
					legacyUrlStatics.AppendLine();
					legacyUrlStatics.AppendLine( "namespace {0} {{".FormatWith( projectNamespace ) );
					legacyUrlStatics.AppendLine( "internal static class LegacyUrlStatics {" );
					legacyUrlStatics.AppendLine( "public static IReadOnlyCollection<UrlPattern> GetPatterns() {" );
					legacyUrlStatics.AppendLine( "var patterns = new List<UrlPattern>();" );
					foreach( var i in childPatterns )
						legacyUrlStatics.AppendLine( "patterns.Add( {0} );".FormatWith( i ) );
					legacyUrlStatics.AppendLine( "return patterns;" );
					legacyUrlStatics.AppendLine( "}" );
					legacyUrlStatics.AppendLine( "public static UrlHandler GetParent() => new YourRootHandler();" );
					legacyUrlStatics.AppendLine( "}" );
					legacyUrlStatics.Append( "}" );
				}
				else if( childPatterns.Any() ) {
					var folderSetup = new StringBuilder();
					folderSetup.AppendLine( "using System;" );
					folderSetup.AppendLine( "using System.Collections.Generic;" );
					folderSetup.AppendLine( "using EnterpriseWebLibrary.EnterpriseWebFramework;" );
					folderSetup.AppendLine();
					folderSetup.AppendLine( "// EwlResource" );
					folderSetup.AppendLine();
					var folderNamespace = WebItemGeneralData.GetNamespaceFromPath( projectNamespace, folderPathRelativeToProject, false );
					folderSetup.AppendLine( "namespace {0} {{".FormatWith( folderNamespace ) );
					folderSetup.AppendLine( "partial class {0} {{".FormatWith( folderSetupClassName ) );

					var namespaces = folderNamespace.Substring( projectNamespace.Length + ".".Length ).Separate( ".", false );
					folderSetup.AppendLine(
						"protected override UrlHandler getUrlParent() => {0};".FormatWith(
							namespaces.Count == 1
								? "LegacyUrlStatics.GetParent()"
								: "new {0}.{1}()".FormatWith( namespaces[ namespaces.Count - 2 ], folderSetupClassName ) ) );

					folderSetup.AppendLine( "protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;" );

					folderSetup.AppendLine( "protected override IEnumerable<UrlPattern> getChildUrlPatterns() {" );
					folderSetup.AppendLine( "var patterns = new List<UrlPattern>();" );
					foreach( var i in childPatterns )
						folderSetup.AppendLine( "patterns.Add( {0} );".FormatWith( i ) );
					folderSetup.AppendLine( "return patterns;" );
					folderSetup.AppendLine( "}" );

					folderSetup.AppendLine( "}" );
					folderSetup.Append( "}" );
					Directory.CreateDirectory( EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject, "Legacy URLs" ) );
					File.WriteAllText(
						EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject, "Legacy URLs", "{0}.cs".FormatWith( folderSetupClassName ) ),
						folderSetup.ToString(),
						Encoding.UTF8 );
				}

				foreach( var file in files ) {
					var parentCode = new StringBuilder();
					parentCode.AppendLine();
					parentCode.AppendLine();
					parentCode.AppendLine( "namespace {0} {{".FormatWith( file.Namespace ) );
					parentCode.AppendLine( "partial class {0} {{".FormatWith( file.ClassName ) );
					parentCode.AppendLine(
						"protected override UrlHandler getUrlParent() => {0};".FormatWith(
							folderPathRelativeToProject.Length == 0 ? "LegacyUrlStatics.GetParent()" : "new {0}()".FormatWith( folderSetupClassName ) ) );
					parentCode.AppendLine( "}" );
					parentCode.Append( "}" );
					File.AppendAllText( EwlStatics.CombinePaths( projectPath, file.PathRelativeToProject ), parentCode.ToString(), Encoding.UTF8 );
				}
			}

			// Generate code for files in the current folder.
			foreach( var fileName in IoMethods.GetFileNamesInFolder( folderPath ) ) {
				if( legacyUrlStatics != null &&
				    aspxFilePaths.Any( i => i.EqualsIgnoreCase( EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject, fileName ) ) ) )
					continue;

				if( Path.GetExtension( fileName ).ToLowerInvariant() != ".cs" )
					continue;
				var generalData = new WebItemGeneralData( projectPath, projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, fileName ), false );
				if( !generalData.IsResource() && !generalData.IsPage() && !generalData.IsAutoCompleteService() )
					continue;
				new Resource( projectContainsFramework, generalData, entitySetup ).GenerateCode( writer );
			}

			// Delve into sub folders.
			foreach( var subFolderName in IoMethods.GetFolderNamesInFolder( folderPath ) ) {
				var subFolderPath = Path.Combine( folderPathRelativeToProject, subFolderName );
				if( subFolderPath == "bin" || subFolderPath == "obj" )
					continue;
				generateForFolder(
					writer,
					projectPath,
					projectNamespace,
					projectContainsFramework,
					generatedCodeFolderPath,
					staticFilesFolderPath,
					staticFilesFolderUrlParentExpression,
					subFolderPath );
			}
		}

		private static void generateStaticFileLogic(
			TextWriter writer, string projectPath, string projectNamespace, bool inFramework, bool? inVersionedFolder, string folderPathRelativeToProject,
			string folderParentExpression ) {
			var folderPath = EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject );

			var folderNamespace = WebItemGeneralData.GetNamespaceFromPath( projectNamespace, folderPathRelativeToProject, false );
			const string folderSetupClassName = "FolderSetup";
			var files = IoMethods.GetFileNamesInFolder( folderPath )
				.Select( i => new WebItemGeneralData( projectPath, projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, i ), true ) )
				.Materialize();
			var subfolderNames = IoMethods.GetFolderNamesInFolder( folderPath ).Materialize();
			generateStaticFileFolderSetup(
				writer,
				inFramework,
				!inVersionedFolder.HasValue,
				folderPathRelativeToProject,
				folderNamespace,
				folderSetupClassName,
				folderParentExpression,
				files.Select( i => "{0}.UrlPatterns.Literal( \"{1}\" )".FormatWith( i.ClassName, i.FileName ) )
					.Concat(
						subfolderNames.Select(
							subfolderName => "{0}.{1}.UrlPatterns.Literal( \"{2}\" )".FormatWith(
								WebItemGeneralData.GetNamespaceFromPath( projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, subfolderName ), false )
									.Separate( ".", false )
									.Last(),
								folderSetupClassName,
								subfolderName ) ) )
					.Materialize() );

			foreach( var file in files )
				new StaticFile( file, inFramework, inVersionedFolder == true, folderSetupClassName ).GenerateCode( writer );

			foreach( var subfolderName in subfolderNames )
				generateStaticFileLogic(
					writer,
					projectPath,
					projectNamespace,
					inFramework,
					inVersionedFolder ?? subfolderName == "versioned",
					EwlStatics.CombinePaths( folderPathRelativeToProject, subfolderName ),
					"new {0}.{1}()".FormatWith( folderNamespace.Separate( ".", false ).Last(), folderSetupClassName ) );
		}

		private static void generateStaticFileFolderSetup(
			TextWriter writer, bool inFramework, bool isRootFolder, string folderPathRelativeToProject, string folderNamespace, string className,
			string parentExpression, IReadOnlyCollection<string> childPatterns ) {
			writer.WriteLine( "namespace {0} {{".FormatWith( folderNamespace ) );
			writer.WriteLine( "public sealed partial class {0}: StaticFileFolderSetup {{".FormatWith( className ) );

			UrlStatics.GenerateUrlClasses(
				writer,
				className,
				null,
				Enumerable.Empty<VariableSpecification>().Materialize(),
				Enumerable.Empty<VariableSpecification>().Materialize(),
				false );
			writer.WriteLine( "protected override StaticFileFolderSetup createParentFolderSetup() => {0};".FormatWith( isRootFolder ? "null" : parentExpression ) );
			if( !isRootFolder || parentExpression.Any() )
				writer.WriteLine( "protected override UrlHandler getUrlParent() => {0};".FormatWith( isRootFolder ? parentExpression : "parentFolderSetup.Value" ) );
			UrlStatics.GenerateGetEncoderMethod(
				writer,
				"",
				Enumerable.Empty<VariableSpecification>().Materialize(),
				Enumerable.Empty<VariableSpecification>().Materialize(),
				p => "true",
				false );
			writer.WriteLine(
				"protected override IEnumerable<UrlPattern> getChildUrlPatterns() => {0};".FormatWith(
					childPatterns.Any() ? "new[] {{ {0} }}".FormatWith( StringTools.ConcatenateWithDelimiter( ", ", childPatterns ) ) : "base.getChildUrlPatterns()" ) );
			writer.WriteLine( "protected override bool isFrameworkFolder => {0};".FormatWith( inFramework ? "true" : "false" ) );
			writer.WriteLine( "protected override string folderPath => @\"{0}\";".FormatWith( folderPathRelativeToProject ) );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		internal static string GetParameterDeclarations( IReadOnlyCollection<VariableSpecification> parameters ) {
			var text = "";
			foreach( var parameter in parameters )
				text = StringTools.ConcatenateWithDelimiter( ", ", text, parameter.TypeName + " " + parameter.Name );
			return text;
		}

		internal static void WriteReCreateFromNewParameterValuesMethod(
			TextWriter writer, IReadOnlyCollection<VariableSpecification> requiredParameters, string methodNamePrefix, string className,
			string infoConstructorArgPrefix ) {
			writer.WriteLine( methodNamePrefix + ( methodNamePrefix.Contains( "protected" ) ? "r" : "R" ) + "eCreateFromNewParameterValues() {" );
			writer.WriteLine(
				"return new {0}( ".FormatWith( className ) + StringTools.ConcatenateWithDelimiter(
					", ",
					infoConstructorArgPrefix,
					InfoStatics.GetInfoConstructorArgumentsForRequiredParameters(
						requiredParameters,
						parameter => "parametersModification." + parameter.PropertyName ) ) + " );" );
			writer.WriteLine( "}" );
		}
	}
}