using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems;
using Humanizer;
using Tewl.IO;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class WebMetaLogicStatics {
		internal static void Generate(
			TextWriter writer, string projectPath, string projectNamespace, string generatedCodeFolderPath, bool? staticFilesFolderIsInFramework,
			string staticFilesFolderPath, string staticFilesFolderUrlParentExpression ) {
			generateForFolder(
				writer,
				projectPath,
				projectNamespace,
				generatedCodeFolderPath,
				staticFilesFolderIsInFramework,
				staticFilesFolderPath,
				staticFilesFolderUrlParentExpression,
				"" );
		}

		private static void generateForFolder(
			TextWriter writer, string projectPath, string projectNamespace, string generatedCodeFolderPath, bool? staticFilesFolderIsInFramework,
			string staticFilesFolderPath, string staticFilesFolderUrlParentExpression, string folderPathRelativeToProject ) {
			if( folderPathRelativeToProject == generatedCodeFolderPath )
				return;

			if( folderPathRelativeToProject == staticFilesFolderPath ) {
				generateStaticFileLogic(
					writer,
					projectPath,
					projectNamespace,
					staticFilesFolderIsInFramework.Value,
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
				entitySetup = new EntitySetup( new WebItemGeneralData( projectPath, projectNamespace, filePathRelativeToProject, false ) );
				entitySetup.GenerateCode( writer );
			}

			// Generate code for files in the current folder.
			foreach( var fileName in IoMethods.GetFileNamesInFolder( folderPath ) ) {
				if( Path.GetExtension( fileName ).ToLowerInvariant() != ".cs" )
					continue;
				var generalData = new WebItemGeneralData( projectPath, projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, fileName ), false );
				if( !generalData.IsResource() && !generalData.IsPage() && !generalData.IsAutoCompleteService() )
					continue;
				new Page( generalData, entitySetup ).GenerateCode( writer );
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
					generatedCodeFolderPath,
					staticFilesFolderIsInFramework,
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
			generateStaticFileFolderSetup(
				writer,
				inFramework,
				!inVersionedFolder.HasValue,
				folderPathRelativeToProject,
				folderNamespace,
				folderSetupClassName,
				folderParentExpression );

			foreach( var fileName in IoMethods.GetFileNamesInFolder( folderPath ) )
				new StaticFile(
					new WebItemGeneralData( projectPath, projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, fileName ), true ),
					inFramework,
					inVersionedFolder == true,
					folderSetupClassName ).GenerateCode( writer );

			foreach( var subfolderName in IoMethods.GetFolderNamesInFolder( folderPath ) )
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
			string parentExpression ) {
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
				false );
			writer.WriteLine( "protected override IEnumerable<UrlPattern> getChildUrlPatterns() => null;" );
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