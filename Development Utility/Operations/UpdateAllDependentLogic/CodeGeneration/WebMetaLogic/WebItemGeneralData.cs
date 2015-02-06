using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal class WebItemGeneralData {
		private readonly string pathRelativeToProject;
		private readonly string urlRelativeToProject;
		private readonly string itemNamespace;
		private readonly string className;
		private readonly string path;
		private readonly string code;
		private readonly WebProject webProjectConfiguration;

		internal WebItemGeneralData( string webProjectPath, string pathRelativeToProject, bool includeFileExtensionInClassName, WebProject webProjectConfiguration ) {
			this.pathRelativeToProject = pathRelativeToProject;

			// Get the URL for this item. "Plain old class" entity setups do not have URLs.
			urlRelativeToProject = pathRelativeToProject.EndsWith( ".cs" ) ? "" : pathRelativeToProject.Replace( System.IO.Path.DirectorySeparatorChar, '/' );

			// Load this item's code if it exists.
			path = StandardLibraryMethods.CombinePaths( webProjectPath, pathRelativeToProject );
			var codePath = path.EndsWith( ".cs" ) ? path : path + ".cs";
			code = File.Exists( codePath ) ? File.ReadAllText( codePath ) : "";

			// Attempt to get the namespace from the code. If this fails, use a namespace based on the item's path in the project.
			foreach( Match match in Regex.Matches( code, @"namespace\s(?<namespace>.*)\s{" ) )
				itemNamespace = match.Groups[ "namespace" ].Value;
			if( itemNamespace == null )
				itemNamespace = getNamespaceFromFilePath( webProjectConfiguration.NamespaceAndAssemblyName, pathRelativeToProject );

			className =
				StandardLibraryMethods.GetCSharpIdentifier(
					System.IO.Path.GetFileNameWithoutExtension( path ) + ( includeFileExtensionInClassName ? System.IO.Path.GetExtension( path ).CapitalizeString() : "" ) );
			this.webProjectConfiguration = webProjectConfiguration;
		}

		private string getNamespaceFromFilePath( string projectNamespace, string filePathRelativeToProject ) {
			var tokens = filePathRelativeToProject.Separate( System.IO.Path.DirectorySeparatorChar.ToString(), false );
			tokens = tokens.Take( tokens.Count - 1 ).ToList();
			return StaticFileHandler.CombineNamespacesAndProcessEwfIfNecessary(
				projectNamespace,
				StringTools.ConcatenateWithDelimiter( ".", tokens.Select( StandardLibraryMethods.GetCSharpIdentifier ).ToArray() ) );
		}

		internal string PathRelativeToProject { get { return pathRelativeToProject; } }
		internal string UrlRelativeToProject { get { return urlRelativeToProject; } }
		internal string Namespace { get { return itemNamespace; } }
		internal string ClassName { get { return className; } }
		internal string Path { get { return path; } }
		internal WebProject WebProjectConfiguration { get { return webProjectConfiguration; } }

		internal List<VariableSpecification> ReadParametersFromCode( bool readOptionalParameters ) {
			try {
				return getVariablesFromCode( code, readOptionalParameters ? "OptionalParameter" : "Parameter" ).ToList();
			}
			catch( Exception e ) {
				throw UserCorrectableException.CreateSecondaryException( "Failed to read parameters from \"" + pathRelativeToProject + "\".", e );
			}
		}

		internal void ReadPageStateVariablesFromCodeAndWriteTypedPageStateMethods( TextWriter writer ) {
			try {
				foreach( var variable in getVariablesFromCode( code, "PageState" ) ) {
					var methodArguments = "this, \"" + variable.Name + "\"";
					writer.WriteLine( "/// <summary>" );
					writer.WriteLine( "/// Returns the page state value of " + variable.Name + " if it has been set. Otherwise, returns the specified default value." );
					writer.WriteLine( "/// </summary>" );
					writer.WriteLine( "private " + variable.TypeName + " get" + variable.PropertyName + "( " + variable.TypeName + " defaultValue ) {" );
					writer.WriteLine( "return EwfPage.Instance.PageState.GetValue( " + methodArguments + ", defaultValue );" );
					writer.WriteLine( "}" );
					writer.WriteLine( "/// <summary>" );
					writer.WriteLine( "/// Sets the page state value of " + variable.Name + " to the specified value." );
					writer.WriteLine( "/// </summary>" );
					writer.WriteLine( "private void set" + variable.PropertyName + "( " + variable.TypeName + " value ) {" );
					writer.WriteLine( "EwfPage.Instance.PageState.SetValue( " + methodArguments + ", value );" );
					writer.WriteLine( "}" );
					writer.WriteLine( "/// <summary>" );
					writer.WriteLine( "/// Clears the page state value of " + variable.Name + "." );
					writer.WriteLine( "/// </summary>" );
					writer.WriteLine( "private void clear" + variable.PropertyName + "() {" );
					writer.WriteLine( "EwfPage.Instance.PageState.ClearValue( " + methodArguments + " );" );
					writer.WriteLine( "}" );
				}
			}
			catch( Exception e ) {
				throw UserCorrectableException.CreateSecondaryException( "Failed to read page state variables from \"" + pathRelativeToProject + "\".", e );
			}
		}

		private IEnumerable<VariableSpecification> getVariablesFromCode( string code, string keyword ) {
			var pattern = @"^//\s*" + keyword + @":\s(?<type>[a-zA-Z_0-9<>]*\??)\s(?<name>\w*)(\ *//(?<comment>[^\n]*))?";
			return from Match match in Regex.Matches( code, pattern, RegexOptions.Multiline )
			       select new VariableSpecification( match.Groups[ "type" ].Value, match.Groups[ "name" ].Value, match.Groups[ "comment" ].Value );
		}
	}
}