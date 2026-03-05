using System.Collections.Immutable;
using System.Text;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.WebFramework.WebItems;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.WebFramework;

internal static class WebFrameworkStatics {
	internal static void Generate(
		TextWriter writer, string projectPath, string projectNamespace, bool projectContainsFramework, IEnumerable<string> ignoredFolderPaths,
		string? staticFilesFolderPath, string? staticFilesFolderUrlParentExpression, out Action<string> resourceSerializationWriter ) {
		var allItems = new List<( WebItemGeneralData? entitySetup, WebItemGeneralData item )>();
		generateForFolder(
			writer,
			projectPath,
			projectNamespace,
			projectContainsFramework,
			ignoredFolderPaths.ToImmutableHashSet( StringComparer.Ordinal ),
			staticFilesFolderPath,
			staticFilesFolderUrlParentExpression,
			"",
			allItems );

		resourceSerializationWriter = interfaceName => {
			string getString( WebItemGeneralData item ) => "\"{0}.{1}\"".FormatWith( item.Namespace, item.ClassName ).Replace( "@", "", StringComparison.Ordinal );

			string getIdentifier( WebItemGeneralData item ) =>
				( item.Namespace.Replace( "_", "__", StringComparison.Ordinal ).Replace( ".", "_", StringComparison.Ordinal ) + "_" + item.ClassName ).Replace(
					"@",
					"",
					StringComparison.Ordinal );

			writer.WriteLine(
				"{0}( string name, string parameters )? {1}SerializeResource( ResourceParent item ) => item switch {{".FormatWith(
					interfaceName.Length > 0 ? "" : "public static ",
					interfaceName.AppendDelimiter( "." ) ) );
			foreach( var (_, item) in allItems )
				writer.WriteLine( "{0} i => ( {1}, serialize_{2}( i ) ),".FormatWith( item.FullClassName, getString( item ), getIdentifier( item ) ) );
			writer.WriteLine( "_ => null" );
			writer.WriteLine( "};" );
			writer.WriteLine();
			writer.WriteLine(
				"{0}ResourceParent? {1}DeserializeResource( string name, string parameters ) => name switch {{".FormatWith(
					interfaceName.Length > 0 ? "" : "public static ",
					interfaceName.AppendDelimiter( "." ) ) );
			foreach( var (_, item) in allItems )
				writer.WriteLine( "{0} => deserialize_{1}( parameters ),".FormatWith( getString( item ), getIdentifier( item ) ) );
			writer.WriteLine( "_ => null" );
			writer.WriteLine( "};" );

			var methodPrefix = interfaceName.Length > 0 ? "private" : "private static";
			foreach( var (entitySetup, item) in allItems ) {
				writer.WriteLine();
				writer.WriteLine( "{0} string serialize_{1}( {2} item ) {{".FormatWith( methodPrefix, getIdentifier( item ), item.FullClassName ) );

				string getMember( WebItemParameter parameter, string objectName ) =>
					"new JProperty( \"{0}\", {1}.{2} == null ? JValue.CreateNull() : JToken.FromObject( {1}.{2} ) )".FormatWith(
						parameter.Name,
						objectName,
						parameter.PropertyName );
				var members = StringTools.ConcatenateWithDelimiter(
					", ",
					( entitySetup != null ? entitySetup.RequiredParameters.Concat( entitySetup.OptionalParameters ) : [ ] ).Select( i => getMember( i, "item.Es" ) )
					.Concat( item.RequiredParameters.Concat( item.OptionalParameters ).Select( i => getMember( i, "item" ) ) ) );

				if( members.Length > 0 ) {
					writer.WriteLine( "#pragma warning disable CS0472, CS8073" );
					writer.WriteLine( "var jsonObject = new JObject( {0} );".FormatWith( members ) );
					writer.WriteLine( "#pragma warning restore CS0472, CS8073" );

					writer.WriteLine( "return jsonObject.ToString( Formatting.None );" );
				}
				else
					writer.WriteLine( "return \"\";" );
				writer.WriteLine( "}" );

				writer.WriteLine( "{0} ResourceParent deserialize_{1}( string parameters ) {{".FormatWith( methodPrefix, getIdentifier( item ) ) );

				string getParameter( WebItemParameter parameter ) =>
					"jsonObject[ \"{0}\" ]!.ToObject<{1}>(){2}".FormatWith( parameter.Name, parameter.TypeName, parameter.AllowsNull ? "" : "!" );
				var arguments = StringTools.ConcatenateWithDelimiter(
						", ",
						( entitySetup?.RequiredParameters ?? [ ] ).Concat( item.RequiredParameters )
						.Select( getParameter )
						.Append(
							StringTools.ConcatenateWithDelimiter(
									" ",
									( entitySetup?.OptionalParameters ?? [ ] ).Select( i => "s.{0} = {1};".FormatWith( i.PropertyName, getParameter( i ) ) ) )
								.Surround( "entitySetupOptionalParameterSetter: ( s, _ ) => { ", " }" ) )
						.Append(
							StringTools.ConcatenateWithDelimiter( " ", item.OptionalParameters.Select( i => "s.{0} = {1};".FormatWith( i.PropertyName, getParameter( i ) ) ) )
								.Surround( "optionalParameterSetter: ( s, {0} ) => {{ ".FormatWith( entitySetup != null ? "_, _" : "_" ), " }" ) ) )
					.Surround( " ", " " );

				if( arguments.Length > 0 )
					writer.WriteLine( "var jsonObject = JsonConvert.DeserializeObject<JObject>( parameters )!;" );
				writer.WriteLine(
					"return {0};".FormatWith(
						item.IsResource() ? "{0}.GetInfo({1})".FormatWith( item.FullClassName, arguments ) : "new {0}({1})".FormatWith( item.FullClassName, arguments ) ) );
				writer.WriteLine( "}" );
			}
		};
	}

	private static void generateForFolder(
		TextWriter writer, string projectPath, string projectNamespace, bool projectContainsFramework, ImmutableHashSet<string> ignoredFolderPaths,
		string? staticFilesFolderPath, string? staticFilesFolderUrlParentExpression, string folderPathRelativeToProject,
		List<( WebItemGeneralData? entitySetup, WebItemGeneralData item )> allItems ) {
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
				allItems );
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
		EntitySetup? entitySetup = null;
		if( entitySetupFileName.Length > 0 ) {
			var filePathRelativeToProject = Path.Combine( folderPathRelativeToProject, entitySetupFileName );
			var generalData = new WebItemGeneralData( projectPath, projectNamespace, filePathRelativeToProject, false );
			entitySetup = new EntitySetup( projectContainsFramework, generalData );
			entitySetup.GenerateCode( writer );
			allItems.Add( ( null, generalData ) );
		}

		// Generate code for files in the current folder.
		foreach( var fileName in IoMethods.GetFileNamesInFolder( folderPath ).OrderBy( i => i ) ) {
			if( Path.GetExtension( fileName ).ToLowerInvariant() != ".cs" )
				continue;
			var generalData = new WebItemGeneralData( projectPath, projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, fileName ), false );
			if( !generalData.IsResource() )
				continue;
			new Resource( generalData, entitySetup ).GenerateCode( writer );
			allItems.Add( ( entitySetup?.GeneralData, generalData ) );
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
				allItems );
		}
	}

	private static void generateStaticFileLogic(
		TextWriter writer, string projectPath, string projectNamespace, bool inFramework, bool? inVersionedFolder, string folderPathRelativeToProject,
		string? folderParentExpression, List<( WebItemGeneralData? entitySetup, WebItemGeneralData item )> allItems ) {
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
					subfolderNames.Select( subfolderName => "{0}.{1}.UrlPatterns.Literal( \"{2}\" )".FormatWith(
						WebItemGeneralData.GetNamespaceFromPath( projectNamespace, EwlStatics.CombinePaths( folderPathRelativeToProject, subfolderName ), false )
							.Separate( ".", false )
							.Last(),
						folderSetupClassName,
						subfolderName ) ) )
				.Materialize() );

		foreach( var file in files ) {
			new WebItems.StaticFile( file, inFramework, inVersionedFolder == true, folderSetupClassName ).GenerateCode( writer );
			allItems.Add( ( null, file ) );
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
				allItems );
	}

	private static void generateStaticFileFolderSetup(
		TextWriter writer, bool inFramework, bool isRootFolder, string folderPathRelativeToProject, string folderNamespace, string className,
		string? parentExpression, IReadOnlyCollection<string> childPatterns ) {
		writer.WriteLine( "namespace {0} {{".FormatWith( folderNamespace ) );
		writer.WriteLine( "public sealed partial class {0}: StaticFileFolderSetup {{".FormatWith( className ) );

		UrlStatics.GenerateUrlClasses(
			writer,
			className,
			null,
			Enumerable.Empty<WebItemParameter>().Materialize(),
			Enumerable.Empty<WebItemParameter>().Materialize(),
			false );
		writer.WriteLine( "protected override ResourceParent? createParent() => {0};".FormatWith( isRootFolder ? "null" : parentExpression ) );
		if( !isRootFolder || parentExpression!.Any() )
			writer.WriteLine( "protected override UrlHandler? getUrlParent() => {0};".FormatWith( isRootFolder ? parentExpression : "base.getUrlParent()" ) );
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
						    : EnterpriseWebFramework.StaticFile.AppStaticFilesFolderName ) + ( isRootFolder ? "" : Path.DirectorySeparatorChar.ToString() ) ).Length ) ) );

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
		TextWriter writer, IReadOnlyCollection<WebItemParameter> requiredParameters, string methodNamePrefix, string className, string infoConstructorArgPrefix ) {
		writer.WriteLine( methodNamePrefix + ( methodNamePrefix.Contains( "protected" ) ? "r" : "R" ) + "eCreateFromNewParameterValues() {" );
		writer.WriteLine(
			"return new {0}( ".FormatWith( className ) + StringTools.ConcatenateWithDelimiter(
				", ",
				infoConstructorArgPrefix,
				InfoStatics.GetInfoConstructorArgumentsForRequiredParameters(
					requiredParameters,
					parameter => "parametersModification!." + parameter.GetReCreationExpression( parameter.PropertyName ) ) ) + " );" );
		writer.WriteLine( "}" );
	}
}