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
				null,
				Enumerable.Empty<VariableSpecification>().Materialize(),
				Enumerable.Empty<VariableSpecification>().Materialize() );
			writer.WriteLine( "protected override StaticFileFolderSetup createParentFolderSetup() => {0};".FormatWith( isRootFolder ? "null" : parentExpression ) );
			if( !isRootFolder || parentExpression.Any() )
				writer.WriteLine( "protected override UrlHandler getUrlParent() => {0};".FormatWith( isRootFolder ? parentExpression : "parentFolderSetup.Value" ) );
			writer.WriteLine( "protected override UrlEncoder getUrlEncoder() => null;" );
			writer.WriteLine( "protected override IEnumerable<UrlPattern> getChildUrlPatterns() => null;" );
			writer.WriteLine( "protected override bool isFrameworkFolder => {0};".FormatWith( inFramework ? "true" : "false" ) );
			writer.WriteLine( "protected override string folderPath => @\"{0}\";".FormatWith( folderPathRelativeToProject ) );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		internal static void WriteCreateInfoFromQueryStringMethod(
			TextWriter writer, EntitySetup es, List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters ) {
			writer.WriteLine( "protected override void createInfoFromQueryString() {" );

			writer.WriteLine( "try {" );
			var allParameters = requiredParameters.Concat( optionalParameters );

			// Create a local variable for all query parameters to hold their raw query value.
			foreach( var parameter in ( es != null ? es.RequiredParameters.Concat( es.OptionalParameters ) : Enumerable.Empty<VariableSpecification>() ).Concat(
				allParameters ) ) {
				// If a query parameter is not specified, Request.QueryString[it] returns null. If it is specified as blank (&it=), Request.QueryString[it] returns the empty string.
				writer.WriteLine(
					"var " + getLocalQueryValueVariableName( parameter ) + " = HttpContext.Current.Request.QueryString[ \"" + parameter.PropertyName + "\" ];" );
			}

			// Enforce specification of all required parameters.
			foreach( var parameter in ( es != null ? es.RequiredParameters : Enumerable.Empty<VariableSpecification>() ).Concat( requiredParameters ) ) {
				// Blow up if a required parameter was not specified.
				writer.WriteLine(
					"if( " + getLocalQueryValueVariableName( parameter ) +
					" == null ) throw new ApplicationException( \"Required parameter not included in query string: " + parameter.Name + "\" );" );
			}

			if( es != null ) {
				var esCtorArguments = es.RequiredParameters.Select( getChangeTypeExpression ).ToList();
				if( es.OptionalParameters.Any() ) {
					esCtorArguments.Add( "optionalParameterPackage: esOptionalParameterPackage" );
					writer.WriteLine( "var esOptionalParameterPackage = new EntitySetup.OptionalParameterPackage();" );
					foreach( var parameter in es.OptionalParameters ) {
						// If the optional parameter was not specified, do not set its value (let it remain its default value).
						writer.WriteLine(
							"if( " + getLocalQueryValueVariableName( parameter ) + " != null ) esOptionalParameterPackage." + parameter.PropertyName + " = " +
							getChangeTypeExpression( parameter ) + ";" );
					}
				}
				writer.WriteLine( "var es = new EntitySetup( {0} );".FormatWith( StringTools.ConcatenateWithDelimiter( ", ", esCtorArguments ) ) );
			}

			// Build up the call to the info constructor.
			var infoCtorArguments = new List<string> { es != null ? "es" : "" };
			infoCtorArguments.AddRange( requiredParameters.Select( getChangeTypeExpression ) );

			// If there are optional parameters, build an optional paramater package, populate it, and include it in the call to the info constructor.
			if( optionalParameters.Count > 0 ) {
				infoCtorArguments.Add( "optionalParameterPackage: optionalParameterPackage" );
				writer.WriteLine( "var optionalParameterPackage = new OptionalParameterPackage();" );
				foreach( var parameter in optionalParameters ) {
					// If the optional parameter was not specified, do not set its value (let it remain its default value).
					writer.WriteLine(
						"if( " + getLocalQueryValueVariableName( parameter ) + " != null ) optionalParameterPackage." + parameter.PropertyName + " = " +
						getChangeTypeExpression( parameter ) + ";" );
				}
			}

			// Construct the info object.
			writer.WriteLine( "info = new Info( " + StringTools.ConcatenateWithDelimiter( ", ", infoCtorArguments.ToArray() ) + " );" );
			writer.WriteLine( "}" ); // Try block

			writer.WriteLine( "catch( Exception e ) {" );
			writer.WriteLine( "if( e is UserDisabledByPageException )" );
			writer.WriteLine( "throw;" );
			writer.WriteLine(
				"throw new ResourceNotAvailableException( \"Query parameter values or non URL elements of the request prevented the creation of the page info or entity setup object.\", e );" );
			writer.WriteLine( "}" ); // Catch block

			// Initialize the parameters modification object.
			if( allParameters.Any() ) {
				writer.WriteLine( "parametersModification = new ParametersModification();" );
				foreach( var parameter in allParameters )
					writer.WriteLine( "parametersModification." + parameter.PropertyName + " = info." + parameter.PropertyName + ";" );
			}

			writer.WriteLine( "}" );
		}

		private static string getLocalQueryValueVariableName( VariableSpecification parameter ) {
			return parameter.Name + "QueryValue";
		}

		private static string getChangeTypeExpression( VariableSpecification parameter ) {
			var parameterName = getLocalQueryValueVariableName( parameter );
			return parameter.GetUrlDeserializationExpression( parameterName );
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