using System.Text.RegularExpressions;
using EnterpriseWebLibrary.InstallationSupportUtility;

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

		internal readonly string PathRelativeToProject;
		internal readonly string FileName;
		internal readonly string Namespace;
		internal readonly string ClassName;
		private readonly string code;
		internal readonly IReadOnlyCollection<WebItemParameter> RequiredParameters;
		internal readonly IReadOnlyCollection<WebItemParameter> OptionalParameters;

		internal WebItemGeneralData( string projectPath, string projectNamespace, string pathRelativeToProject, bool isStaticFile ) {
			PathRelativeToProject = pathRelativeToProject;
			var path = EwlStatics.CombinePaths( projectPath, pathRelativeToProject );
			FileName = Path.GetFileName( path );

			// Load this item’s code if it exists.
			code = path.EndsWith( ".cs" ) && !isStaticFile ? File.ReadAllText( path ) : "";

			// Attempt to get the namespace from the code. If this fails, use a namespace based on the item’s path in the project.
			foreach( Match match in Regex.Matches( code, @"namespace\s(?<namespace>.*)\s{" ) )
				Namespace = match.Groups[ "namespace" ].Value;
			Namespace ??= GetNamespaceFromPath( projectNamespace, pathRelativeToProject, true );

			ClassName = EwlStatics.GetCSharpIdentifier(
				Path.GetFileNameWithoutExtension( path ).CapitalizeString() + ( isStaticFile ? Path.GetExtension( path ).CapitalizeString() : "" ) );

			RequiredParameters = readParametersFromCode( false );
			OptionalParameters = readParametersFromCode( true );
		}

		private IReadOnlyCollection<WebItemParameter> readParametersFromCode( bool readOptionalParameters ) {
			try {
				return getVariablesFromCode( code, readOptionalParameters ? "OptionalParameter" : "Parameter" ).Materialize();
			}
			catch( Exception e ) {
				throw UserCorrectableException.CreateSecondaryException( "Failed to read parameters from \"" + PathRelativeToProject + "\".", e );
			}
		}

		private IEnumerable<WebItemParameter> getVariablesFromCode( string code, string keyword ) {
			var pattern = @"^//\s*" + keyword + @":\s(?<type>[a-zA-Z_0-9<>]*\??)\s(?<name>\w*)(\ *//(?<comment>[^\n]*))?";
			return from Match match in Regex.Matches( code, pattern, RegexOptions.Multiline )
			       select new WebItemParameter( match.Groups[ "type" ].Value, match.Groups[ "name" ].Value, match.Groups[ "comment" ].Value );
		}

		internal string FullClassName => Namespace + "." + ClassName;

		internal bool IsResource() =>
			Regex.IsMatch( code, "^// {0}Resource\r?$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline ) || IsPage() ||
			IsAutoCompleteService();

		internal bool IsPage() => Regex.IsMatch( code, "^// {0}Page\r?$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline );

		internal bool IsAutoCompleteService() =>
			Regex.IsMatch( code, "^// {0}AutoCompleteService\r?$".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ), RegexOptions.Multiline );
	}
}