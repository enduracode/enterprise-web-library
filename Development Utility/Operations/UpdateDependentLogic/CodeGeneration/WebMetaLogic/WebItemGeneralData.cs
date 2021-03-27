using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.InstallationSupportUtility;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal class WebItemGeneralData {
		private readonly string pathRelativeToProject;
		private readonly string itemNamespace;
		private readonly string className;
		private readonly string code;

		internal WebItemGeneralData( string projectPath, string projectNamespace, string pathRelativeToProject, bool includeFileExtensionInClassName ) {
			this.pathRelativeToProject = pathRelativeToProject;

			// Load this item's code if it exists.
			var path = EwlStatics.CombinePaths( projectPath, pathRelativeToProject );
			code = path.EndsWith( ".cs" ) ? File.ReadAllText( path ) : "";

			// Attempt to get the namespace from the code. If this fails, use a namespace based on the item's path in the project.
			foreach( Match match in Regex.Matches( code, @"namespace\s(?<namespace>.*)\s{" ) )
				itemNamespace = match.Groups[ "namespace" ].Value;
			if( itemNamespace == null )
				itemNamespace = getNamespaceFromFilePath( projectNamespace, pathRelativeToProject );

			className = EwlStatics.GetCSharpIdentifier(
				Path.GetFileNameWithoutExtension( path ).CapitalizeString() + ( includeFileExtensionInClassName ? Path.GetExtension( path ).CapitalizeString() : "" ) );
		}

		private string getNamespaceFromFilePath( string projectNamespace, string filePathRelativeToProject ) {
			var tokens = filePathRelativeToProject.Separate( System.IO.Path.DirectorySeparatorChar.ToString(), false );
			tokens = tokens.Take( tokens.Count - 1 ).ToList();
			return projectNamespace + StringTools
				       .ConcatenateWithDelimiter( ".", tokens.Select( i => EwlStatics.GetCSharpIdentifier( i.CapitalizeString() ) ).ToArray() )
				       .PrependDelimiter( "." );
		}

		internal string PathRelativeToProject => pathRelativeToProject;
		internal string Namespace => itemNamespace;
		internal string ClassName => className;

		internal bool IsResource() => Regex.IsMatch( code, "^// {0}Resource$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline );
		internal bool IsPage() => Regex.IsMatch( code, "^// {0}Page$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline );

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