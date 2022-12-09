using System.Collections.Immutable;
using System.Text;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework.WebItems;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework {
	internal static class WebFrameworkStatics {
		internal static void Generate(
			TextWriter writer, string projectPath, string projectNamespace, bool projectContainsFramework, IEnumerable<string> ignoredFolderPaths,
			string staticFilesFolderPath, string staticFilesFolderUrlParentExpression, out Action<string> resourceSerializationWriter ) {
			var allResources = new List<( WebItemGeneralData entitySetup, WebItemGeneralData resource )>();
			generateForFolder(
				writer,
				projectPath,
				projectNamespace,
				projectContainsFramework,
				ignoredFolderPaths.ToImmutableHashSet( StringComparer.Ordinal ),
				staticFilesFolderPath,
				staticFilesFolderUrlParentExpression,
				"",
				allResources );

			resourceSerializationWriter = interfaceName => {
				string getString( WebItemGeneralData resource ) =>
					"\"{0}.{1}\"".FormatWith( resource.Namespace, resource.ClassName ).Replace( "@", "", StringComparison.Ordinal );

				string getIdentifier( WebItemGeneralData resource ) =>
					( resource.Namespace.Replace( "_", "__", StringComparison.Ordinal ).Replace( ".", "_", StringComparison.Ordinal ) + "_" + resource.ClassName )
					.Replace( "@", "", StringComparison.Ordinal );

				writer.WriteLine(
					"{0}( string name, string parameters )? {1}SerializeResource( ResourceBase resource ) => resource switch {{".FormatWith(
						interfaceName.Length > 0 ? "" : "public static ",
						interfaceName.AppendDelimiter( "." ) ) );
				foreach( var (_, resource) in allResources )
					writer.WriteLine( "{0} r => ( {1}, serialize_{2}( r ) ),".FormatWith( resource.FullClassName, getString( resource ), getIdentifier( resource ) ) );
				writer.WriteLine( "_ => null" );
				writer.WriteLine( "};" );
				writer.WriteLine();
				writer.WriteLine(
					"{0}ResourceBase {1}DeserializeResource( string name, string parameters ) => name switch {{".FormatWith(
						interfaceName.Length > 0 ? "" : "public static ",
						interfaceName.AppendDelimiter( "." ) ) );
				foreach( var (_, resource) in allResources )
					writer.WriteLine( "{0} => deserialize_{1}( parameters ),".FormatWith( getString( resource ), getIdentifier( resource ) ) );
				writer.WriteLine( "_ => null" );
				writer.WriteLine( "};" );

				var methodPrefix = interfaceName.Length > 0 ? "private" : "private static";
				foreach( var (entitySetup, resource) in allResources ) {
					writer.WriteLine();
					writer.WriteLine( "{0} string serialize_{1}( {2} resource ) {{".FormatWith( methodPrefix, getIdentifier( resource ), resource.FullClassName ) );

					string getMember( WebItemParameter parameter, string objectName ) =>
						"new JProperty( \"{0}\", {1}.{2} == null ? JValue.CreateNull() : JToken.FromObject( {1}.{2} ) )".FormatWith(
							parameter.Name,
							objectName,
							parameter.PropertyName );
					var members = StringTools.ConcatenateWithDelimiter(
						", ",
						( entitySetup != null ? entitySetup.RequiredParameters.Concat( entitySetup.OptionalParameters ) : Enumerable.Empty<WebItemParameter>() )
						.Select( i => getMember( i, "resource.Es" ) )
						.Concat( resource.RequiredParameters.Concat( resource.OptionalParameters ).Select( i => getMember( i, "resource" ) ) ) );

					if( members.Length > 0 ) {
						writer.WriteLine( "#pragma warning disable CS0472" );
						writer.WriteLine( "var jsonObject = new JObject( {0} );".FormatWith( members ) );
						writer.WriteLine( "#pragma warning restore CS0472" );

						writer.WriteLine( "return jsonObject.ToString( Formatting.None );" );
					}
					else
						writer.WriteLine( "return \"\";" );
					writer.WriteLine( "}" );

					writer.WriteLine( "{0} ResourceBase deserialize_{1}( string parameters ) {{".FormatWith( methodPrefix, getIdentifier( resource ) ) );

					string getParameter( WebItemParameter parameter ) => "jsonObject[ \"{0}\" ].ToObject<{1}>()".FormatWith( parameter.Name, parameter.TypeName );
					var arguments = StringTools.ConcatenateWithDelimiter(
							", ",
							( entitySetup != null ? entitySetup.RequiredParameters : Enumerable.Empty<WebItemParameter>() ).Concat( resource.RequiredParameters )
							.Select( getParameter )
							.Append(
								StringTools.ConcatenateWithDelimiter(
										" ",
										( entitySetup != null ? entitySetup.OptionalParameters : Enumerable.Empty<WebItemParameter>() ).Select(
											i => "s.{0} = {1};".FormatWith( i.PropertyName, getParameter( i ) ) ) )
									.Surround( "entitySetupOptionalParameterSetter: ( s, _ ) => { ", " }" ) )
							.Append(
								StringTools.ConcatenateWithDelimiter(
										" ",
										resource.OptionalParameters.Select( i => "s.{0} = {1};".FormatWith( i.PropertyName, getParameter( i ) ) ) )
									.Surround( "optionalParameterSetter: ( s, {0} ) => {{ ".FormatWith( entitySetup != null ? "_, _" : "_" ), " }" ) ) )
						.Surround( " ", " " );

					if( arguments.Length > 0 )
						writer.WriteLine( "var jsonObject = JsonConvert.DeserializeObject<JObject>( parameters );" );
					writer.WriteLine(
						"return {0};".FormatWith(
							resource.IsResource()
								? "{0}.GetInfo({1})".FormatWith( resource.FullClassName, arguments )
								: "new {0}({1})".FormatWith( resource.FullClassName, arguments ) ) );
					writer.WriteLine( "}" );
				}
			};
		}

		private static void generateForFolder(
			TextWriter writer, string projectPath, string projectNamespace, bool projectContainsFramework, ImmutableHashSet<string> ignoredFolderPaths,
			string staticFilesFolderPath, string staticFilesFolderUrlParentExpression, string folderPathRelativeToProject,
			List<( WebItemGeneralData entitySetup, WebItemGeneralData resource )> allResources ) {
			if( ignoredFolderPaths.Contains( folderPathRelativeToProject ) )
				return;

			if( folderPathRelativeToProject == staticFilesFolderPath ) {
				generateStaticFileLogic(
					writer,
					projectPath,
					projectNamespace,
					projectContainsFramework,
					null,
					folderPathRelativeToProject,
					staticFilesFolderUrlParentExpression,
					allResources );
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

			// Generate code for files in the current folder.
			foreach( var fileName in IoMethods.GetFileNamesInFolder( folderPath ).OrderBy( i => i ) ) {
				if( Path.GetExtension( fileName ).ToLowerInvariant() != ".cs" )
					continue;
				var generalData = new WebItemGeneralData( projectPath, projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, fileName ), false );
				if( !generalData.IsResource() )
					continue;
				new Resource( projectContainsFramework, generalData, entitySetup ).GenerateCode( writer );
				allResources.Add( ( entitySetup?.GeneralData, generalData ) );
			}

			// Delve into sub folders.
			foreach( var subFolderName in IoMethods.GetFolderNamesInFolder( folderPath ).OrderBy( i => i ) ) {
				var subFolderPath = Path.Combine( folderPathRelativeToProject, subFolderName );
				if( subFolderPath == "bin" || subFolderPath == "obj" )
					continue;
				generateForFolder(
					writer,
					projectPath,
					projectNamespace,
					projectContainsFramework,
					ignoredFolderPaths,
					staticFilesFolderPath,
					staticFilesFolderUrlParentExpression,
					subFolderPath,
					allResources );
			}
		}

		private static void generateStaticFileLogic(
			TextWriter writer, string projectPath, string projectNamespace, bool inFramework, bool? inVersionedFolder, string folderPathRelativeToProject,
			string folderParentExpression, List<( WebItemGeneralData entitySetup, WebItemGeneralData resource )> allResources ) {
			var isRootFolder = !inVersionedFolder.HasValue;
			var folderPath = EwlStatics.CombinePaths( projectPath, folderPathRelativeToProject );

			var folderNamespace = WebItemGeneralData.GetNamespaceFromPath( projectNamespace, folderPathRelativeToProject, false );
			const string folderSetupClassName = "FolderSetup";
			var files = IoMethods.GetFileNamesInFolder( folderPath )
				.OrderBy( i => i )
				.Select( i => new WebItemGeneralData( projectPath, projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, i ), true ) )
				.Materialize();
			var subfolderNames = IoMethods.GetFolderNamesInFolder( folderPath )
				.Where( i => !isRootFolder || i != AppStatics.StaticFileLogicFolderName )
				.OrderBy( i => i )
				.Materialize();
			generateStaticFileFolderSetup(
				writer,
				inFramework,
				isRootFolder,
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

			foreach( var file in files ) {
				new StaticFile( file, inFramework, inVersionedFolder == true, folderSetupClassName ).GenerateCode( writer );
				allResources.Add( ( null, file ) );
			}

			var staticFilesFolderPath = inFramework
				                            ? EnterpriseWebFramework.StaticFile.FrameworkStaticFilesSourceFolderPath
				                            : EnterpriseWebFramework.StaticFile.AppStaticFilesFolderName;
			var logicFolderPath = EwlStatics.CombinePaths(
				projectPath,
				staticFilesFolderPath,
				AppStatics.StaticFileLogicFolderName,
				folderPathRelativeToProject.Substring( ( staticFilesFolderPath + ( isRootFolder ? "" : Path.DirectorySeparatorChar.ToString() ) ).Length ) );
			Directory.CreateDirectory( logicFolderPath );
			createStaticFileLogicTemplate( logicFolderPath, folderNamespace, folderSetupClassName );
			foreach( var i in files )
				createStaticFileLogicTemplate( logicFolderPath, i.Namespace, i.ClassName );

			foreach( var subfolderName in subfolderNames )
				generateStaticFileLogic(
					writer,
					projectPath,
					projectNamespace,
					inFramework,
					inVersionedFolder ?? subfolderName == "versioned",
					EwlStatics.CombinePaths( folderPathRelativeToProject, subfolderName ),
					"new {0}.{1}()".FormatWith( folderNamespace.Separate( ".", false ).Last(), folderSetupClassName ),
					allResources );
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
				Enumerable.Empty<WebItemParameter>().Materialize(),
				Enumerable.Empty<WebItemParameter>().Materialize(),
				false );
			writer.WriteLine( "protected override StaticFileFolderSetup createParentFolderSetup() => {0};".FormatWith( isRootFolder ? "null" : parentExpression ) );
			if( !isRootFolder || parentExpression.Any() )
				writer.WriteLine( "protected override UrlHandler getUrlParent() => {0};".FormatWith( isRootFolder ? parentExpression : "parentFolderSetup.Value" ) );
			UrlStatics.GenerateGetEncoderMethod(
				writer,
				"",
				Enumerable.Empty<WebItemParameter>().Materialize(),
				Enumerable.Empty<WebItemParameter>().Materialize(),
				_ => "true",
				false );
			writer.WriteLine(
				"protected override IEnumerable<UrlPattern> getChildUrlPatterns() => {0};".FormatWith(
					childPatterns.Any() ? "new[] {{ {0} }}".FormatWith( StringTools.ConcatenateWithDelimiter( ", ", childPatterns ) ) : "base.getChildUrlPatterns()" ) );
			writer.WriteLine( "protected override bool isFrameworkFolder => {0};".FormatWith( inFramework ? "true" : "false" ) );
			writer.WriteLine(
				"protected override string folderPath => @\"{0}\";".FormatWith(
					folderPathRelativeToProject.Substring(
						( ( inFramework
							    ? EnterpriseWebFramework.StaticFile.FrameworkStaticFilesSourceFolderPath
							    : EnterpriseWebFramework.StaticFile.AppStaticFilesFolderName ) + ( isRootFolder ? "" : Path.DirectorySeparatorChar.ToString() ) )
						.Length ) ) );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private static void createStaticFileLogicTemplate( string folderPath, string itemNamespace, string className ) {
			var templateFilePath = EwlStatics.CombinePaths( folderPath, className + DataAccess.DataAccessStatics.CSharpTemplateFileExtension );
			IoMethods.DeleteFile( templateFilePath );

			// If a real file exists, don’t create a template.
			if( File.Exists( EwlStatics.CombinePaths( folderPath, className + ".cs" ) ) )
				return;

			using var writer = new StreamWriter( templateFilePath, false, Encoding.UTF8 );
			writer.WriteLine( "namespace {0} {{".FormatWith( itemNamespace ) );
			writer.WriteLine( "	partial class {0} {{".FormatWith( className ) );
			writer.WriteLine(
				"		// IMPORTANT: Change extension from \"{0}\" to \".cs\" before including in project and editing.".FormatWith(
					DataAccess.DataAccessStatics.CSharpTemplateFileExtension ) );
			writer.WriteLine( "	}" );
			writer.WriteLine( "}" );
		}

		internal static string GetParameterDeclarations( IReadOnlyCollection<WebItemParameter> parameters ) {
			var text = "";
			foreach( var parameter in parameters )
				text = StringTools.ConcatenateWithDelimiter( ", ", text, parameter.TypeName + " " + parameter.Name );
			return text;
		}

		internal static void WriteReCreateFromNewParameterValuesMethod(
			TextWriter writer, IReadOnlyCollection<WebItemParameter> requiredParameters, string methodNamePrefix, string className,
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