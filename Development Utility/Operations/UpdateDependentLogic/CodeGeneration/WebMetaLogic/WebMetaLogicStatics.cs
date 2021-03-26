using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems;
using Humanizer;
using Tewl.IO;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class WebMetaLogicStatics {
		internal static void Generate( TextWriter writer, string projectPath, string projectNamespace ) {
			generateCodeForWebItemsInFolder( writer, projectPath, projectNamespace, "" );
		}

		private static void generateCodeForWebItemsInFolder( TextWriter writer, string projectPath, string projectNamespace, string folderPathRelativeToProject ) {
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
				if( !folderPathRelativeToProject.Any() && fileName.Contains( ".csproj" ) )
					continue;
				var fileExtension = Path.GetExtension( fileName ).ToLowerInvariant();
				if( new[] { ".cs", ".ascx", ".asax", ".master", ".config", ".svc" }.Contains( fileExtension ) )
					continue;

				var filePathRelativeToProject = Path.Combine( folderPathRelativeToProject, fileName );
				if( fileExtension == ".aspx" )
					new Page( new WebItemGeneralData( projectPath, projectNamespace, filePathRelativeToProject, false ), entitySetup ).GenerateCode( writer );
				else
					new StaticFile( new WebItemGeneralData( projectPath, projectNamespace, filePathRelativeToProject, true ) ).GenerateCode( writer );
			}

			// Delve into sub folders.
			foreach( var subFolderName in IoMethods.GetFolderNamesInFolder( folderPath ) ) {
				var subFolderPath = Path.Combine( folderPathRelativeToProject, subFolderName );
				if( subFolderPath == "bin" || subFolderPath == "obj" )
					continue;
				generateCodeForWebItemsInFolder( writer, projectPath, projectNamespace, subFolderPath );
			}
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
			TextWriter writer, IReadOnlyCollection<VariableSpecification> requiredParameters, IReadOnlyCollection<VariableSpecification> optionalParameters,
			string methodNamePrefix, string className, string infoConstructorArgPrefix ) {
			writer.WriteLine( methodNamePrefix + ( methodNamePrefix.Contains( "protected" ) ? "r" : "R" ) + "eCreateFromNewParameterValues() {" );
			writer.WriteLine(
				"return new {0}( ".FormatWith( className ) + StringTools.ConcatenateWithDelimiter(
					", ",
					infoConstructorArgPrefix,
					InfoStatics.GetInfoConstructorArguments(
						requiredParameters,
						optionalParameters,
						parameter => "parametersModification." + parameter.PropertyName,
						parameter => "parametersModification." + parameter.PropertyName ) ) + " );" );
			writer.WriteLine( "}" );
		}
	}
}