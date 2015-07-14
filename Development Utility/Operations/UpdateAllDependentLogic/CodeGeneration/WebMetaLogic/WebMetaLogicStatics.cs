using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class WebMetaLogicStatics {
		internal static void Generate( TextWriter writer, string webProjectPath, WebProject configuration ) {
			generateCodeForWebItemsInFolder( writer, webProjectPath, "", configuration );
		}

		private static void generateCodeForWebItemsInFolder(
			TextWriter writer, string webProjectPath, string folderPathRelativeToProject, WebProject webProjectConfiguration ) {
			var folderPath = EwlStatics.CombinePaths( webProjectPath, folderPathRelativeToProject );

			// Generate code for the entity setup if one exists in this folder.
			var entitySetupFileName = "";
			foreach( var fileName in new[] { "EntitySetup.ascx", "EntitySetup.cs" } ) {
				if( File.Exists( EwlStatics.CombinePaths( folderPath, fileName ) ) ) {
					entitySetupFileName = fileName;
					break;
				}
			}
			EntitySetup entitySetup = null;
			if( entitySetupFileName.Length > 0 ) {
				var filePathRelativeToProject = Path.Combine( folderPathRelativeToProject, entitySetupFileName );
				entitySetup = new EntitySetup( new WebItemGeneralData( webProjectPath, filePathRelativeToProject, false, webProjectConfiguration ) );
				entitySetup.GenerateCode( writer );
			}

			// Generate code for files in the current folder.
			foreach( var fileName in IoMethods.GetFileNamesInFolder( folderPath ) ) {
				if( !folderPathRelativeToProject.Any() && ( fileName.Contains( ".csproj" ) || fileName == AppStatics.StandardLibraryFilesFileName ) )
					continue;
				var fileExtension = Path.GetExtension( fileName ).ToLowerInvariant();
				if( new[] { ".cs", ".asax", ".master", ".config", ".svc" }.Contains( fileExtension ) )
					continue;

				var filePathRelativeToProject = Path.Combine( folderPathRelativeToProject, fileName );
				if( fileExtension == ".aspx" )
					new Page( new WebItemGeneralData( webProjectPath, filePathRelativeToProject, false, webProjectConfiguration ), entitySetup ).GenerateCode( writer );
				else if( fileExtension == ".ascx" ) {
					if( fileName != entitySetupFileName )
						new UserControl( new WebItemGeneralData( webProjectPath, filePathRelativeToProject, false, webProjectConfiguration ) ).GenerateCode( writer );
				}
				else
					new StaticFile( new WebItemGeneralData( webProjectPath, filePathRelativeToProject, true, webProjectConfiguration ) ).GenerateCode( writer );
			}

			// Delve into sub folders.
			foreach( var subFolderName in IoMethods.GetFolderNamesInFolder( folderPath ) ) {
				var subFolderPath = Path.Combine( folderPathRelativeToProject, subFolderName );
				if( subFolderPath == "bin" || subFolderPath == "obj" )
					continue;
				generateCodeForWebItemsInFolder( writer, webProjectPath, subFolderPath, webProjectConfiguration );
			}
		}

		internal static void WriteCreateInfoFromQueryStringMethod(
			TextWriter writer, List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters, string methodNamePrefix,
			string infoConstructorArgPrefix ) {
			writer.WriteLine( methodNamePrefix + ( methodNamePrefix.Contains( "protected" ) ? "c" : "C" ) + "reateInfoFromQueryString() {" );

			writer.WriteLine( "try {" );
			var allParameters = requiredParameters.Concat( optionalParameters );

			// Create a local variable for all query parameters to hold their raw query value.
			foreach( var parameter in allParameters ) {
				// If a query parameter is not specified, Request.QueryString[it] returns null. If it is specified as blank (&it=), Request.QueryString[it] returns the empty string.
				writer.WriteLine(
					"var " + getLocalQueryValueVariableName( parameter ) + " = HttpContext.Current.Request.QueryString[ \"" + parameter.PropertyName + "\" ];" );
			}

			// Enforce specification of all required parameters.
			foreach( var parameter in requiredParameters ) {
				// Blow up if a required parameter was not specified.
				writer.WriteLine(
					"if( " + getLocalQueryValueVariableName( parameter ) + " == null ) throw new ApplicationException( \"Required parameter not included in query string: " +
					parameter.Name + "\" );" );
			}

			// Build up the call to the info constructor.
			var infoCtorArguments = new List<string> { infoConstructorArgPrefix };
			infoCtorArguments.AddRange( requiredParameters.Select( rp => getChangeTypeExpression( rp ) ) );

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
				"throw new ResourceNotAvailableException( \"Query parameter values or non URL elements of the request prevented the creation of the page or entity setup info object.\", e );" );
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

		internal static string GetParameterDeclarations( List<VariableSpecification> parameters ) {
			var text = "";
			foreach( var parameter in parameters )
				text = StringTools.ConcatenateWithDelimiter( ", ", text, parameter.TypeName + " " + parameter.Name );
			return text;
		}

		internal static void WriteCreateInfoFromNewParameterValuesMethod(
			TextWriter writer, List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters, string methodNamePrefix,
			string infoConstructorArgPrefix ) {
			writer.WriteLine( methodNamePrefix + ( methodNamePrefix.Contains( "protected" ) ? "c" : "C" ) + "reateInfoFromNewParameterValues() {" );
			writer.WriteLine(
				"return new Info( " +
				StringTools.ConcatenateWithDelimiter(
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