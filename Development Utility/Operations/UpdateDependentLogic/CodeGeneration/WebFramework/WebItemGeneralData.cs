using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.InstallationSupportUtility;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework {
	internal class WebItemGeneralData {
		internal const string ParameterDefaultsFieldName = "__parameterDefaults";

		internal static string GetNamespaceFromPath( string projectNamespace, string pathRelativeToProject, bool isFilePath ) {
			var tokens = pathRelativeToProject.Separate( Path.DirectorySeparatorChar.ToString(), false );
			var namespaceTokens = isFilePath ? tokens.Take( tokens.Count - 1 ) : tokens;
			return projectNamespace + StringTools
				       .ConcatenateWithDelimiter( ".", namespaceTokens.Select( i => EwlStatics.GetCSharpIdentifier( i.CapitalizeString() ) ) )
				       .PrependDelimiter( "." );
		}

		private readonly string pathRelativeToProject;
		private readonly string fileName;
		private readonly string itemNamespace;
		private readonly string className;
		private readonly string code;

		internal WebItemGeneralData( string projectPath, string projectNamespace, string pathRelativeToProject, bool includeFileExtensionInClassName ) {
			this.pathRelativeToProject = pathRelativeToProject;
			var path = EwlStatics.CombinePaths( projectPath, pathRelativeToProject );
			fileName = Path.GetFileName( path );

			// Load this item's code if it exists.
			code = path.EndsWith( ".cs" ) ? File.ReadAllText( path ) : "";

			// Attempt to get the namespace from the code. If this fails, use a namespace based on the item's path in the project.
			foreach( Match match in Regex.Matches( code, @"namespace\s(?<namespace>.*)\s{" ) )
				itemNamespace = match.Groups[ "namespace" ].Value;
			if( itemNamespace == null )
				itemNamespace = GetNamespaceFromPath( projectNamespace, pathRelativeToProject, true );

			className = EwlStatics.GetCSharpIdentifier(
				Path.GetFileNameWithoutExtension( path ).CapitalizeString() + ( includeFileExtensionInClassName ? Path.GetExtension( path ).CapitalizeString() : "" ) );
		}

		internal string PathRelativeToProject => pathRelativeToProject;
		internal string FileName => fileName;
		internal string Namespace => itemNamespace;
		internal string ClassName => className;

		internal bool IsResource() => Regex.IsMatch( code, "^// {0}Resource\r?$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline );
		internal bool IsPage() => Regex.IsMatch( code, "^// {0}Page\r?$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline );

		internal bool IsAutoCompleteService() =>
			Regex.IsMatch( code, "^// {0}AutoCompleteService\r?$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline );

		internal List<WebItemParameter> ReadParametersFromCode( bool readOptionalParameters ) {
			try {
				return getVariablesFromCode( code, readOptionalParameters ? "OptionalParameter" : "Parameter" ).ToList();
			}
			catch( Exception e ) {
				throw UserCorrectableException.CreateSecondaryException( "Failed to read parameters from \"" + pathRelativeToProject + "\".", e );
			}
		}

		private IEnumerable<WebItemParameter> getVariablesFromCode( string code, string keyword ) {
			var pattern = @"^//\s*" + keyword + @":\s(?<type>[a-zA-Z_0-9<>]*\??)\s(?<name>\w*)(\ *//(?<comment>[^\n]*))?";
			return from Match match in Regex.Matches( code, pattern, RegexOptions.Multiline )
			       select new WebItemParameter( match.Groups[ "type" ].Value, match.Groups[ "name" ].Value, match.Groups[ "comment" ].Value );
		}
	}
}