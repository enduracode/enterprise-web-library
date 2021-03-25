using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.InstallationSupportUtility;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal class WebItemGeneralData {
		private readonly string pathRelativeToProject;
		private readonly string urlRelativeToProject;
		private readonly string itemNamespace;
		private readonly string className;
		private readonly string path;
		private readonly string code;
		private readonly WebProject webProjectConfiguration;

		internal WebItemGeneralData(
			string webProjectPath, string pathRelativeToProject, bool includeFileExtensionInClassName, WebProject webProjectConfiguration ) {
			this.pathRelativeToProject = pathRelativeToProject;

			// Get the URL for this item. Entity setups do not have URLs.
			urlRelativeToProject = pathRelativeToProject.EndsWith( ".cs" ) ? "" : pathRelativeToProject.Replace( System.IO.Path.DirectorySeparatorChar, '/' );

			// Load this item's code if it exists.
			path = EwlStatics.CombinePaths( webProjectPath, pathRelativeToProject );
			var codePath = path.EndsWith( ".cs" ) ? path : path + ".cs";
			code = File.Exists( codePath ) ? File.ReadAllText( codePath ) : "";

			// Attempt to get the namespace from the code. If this fails, use a namespace based on the item's path in the project.
			foreach( Match match in Regex.Matches( code, @"namespace\s(?<namespace>.*)\s{" ) )
				itemNamespace = match.Groups[ "namespace" ].Value;
			if( itemNamespace == null )
				itemNamespace = getNamespaceFromFilePath( webProjectConfiguration.NamespaceAndAssemblyName, pathRelativeToProject );

			className = EwlStatics.GetCSharpIdentifier(
				System.IO.Path.GetFileNameWithoutExtension( path ).CapitalizeString() +
				( includeFileExtensionInClassName ? System.IO.Path.GetExtension( path ).CapitalizeString() : "" ) );
			this.webProjectConfiguration = webProjectConfiguration;
		}

		private string getNamespaceFromFilePath( string projectNamespace, string filePathRelativeToProject ) {
			var tokens = filePathRelativeToProject.Separate( System.IO.Path.DirectorySeparatorChar.ToString(), false );
			tokens = tokens.Take( tokens.Count - 1 ).ToList();
			return projectNamespace + StringTools
				       .ConcatenateWithDelimiter( ".", tokens.Select( i => EwlStatics.GetCSharpIdentifier( i.CapitalizeString() ) ).ToArray() )
				       .PrependDelimiter( "." );
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

		private IEnumerable<VariableSpecification> getVariablesFromCode( string code, string keyword ) {
			var pattern = @"^//\s*" + keyword + @":\s(?<type>[a-zA-Z_0-9<>]*\??)\s(?<name>\w*)(\ *//(?<comment>[^\n]*))?";
			return from Match match in Regex.Matches( code, pattern, RegexOptions.Multiline )
			       select new VariableSpecification( match.Groups[ "type" ].Value, match.Groups[ "name" ].Value, match.Groups[ "comment" ].Value );
		}
	}
}