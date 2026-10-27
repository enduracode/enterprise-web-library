using System.Data.Common;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Collections;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

static class DataAccessStatics {
	internal const string CSharpTemplateFileExtension = ".ewlt.cs";

	/// <summary>
	/// Given a string, returns all instances of @abc in an ordered set containing abc (the token without the @ sign). If a token is used more than once, it
	/// only appears in the list once. A different prefix may be used for certain databases.
	/// </summary>
	private static ListSet<string> getNamedParamList( DatabaseInfo info, string statement ) {
		// We don't want to find parameters in quoted text.
		statement = statement.RemoveTextBetweenStrings( "'", "'" ).RemoveTextBetweenStrings( "\"", "\"" );

		var parameters = new ListSet<string>();
		foreach( Match match in Regex.Matches( statement, getParamRegex( info ) ) )
			parameters.Add( match.Value.Substring( 1 ) );

		return parameters;
	}

	private static string getParamRegex( DatabaseInfo info ) {
		// Matches spaced followed by @abc. The space prevents @@identity, etc. from getting matched.
		return @"(?<!{0}){0}\w*\w".FormatWith( info.ParameterPrefix );
	}

	/// <summary>
	/// Executes the specified method with a command created from raw query text such as that from Development.xml. All parameters will be filled in with good
	/// dummy values, and the command can be safely executed using schema only or key info behavior. Returns parameter metadata.
	/// </summary>
	internal static IReadOnlyCollection<ValueContainer> ExecuteWithCommandFromRawQueryText(
		DatabaseConnection cn, Database database, string commandText, Action<DbCommand> method ) {
		var parameterNames = getNamedParamList( cn.DatabaseInfo, commandText );
		var inferredParametersByName =
			( parameterNames.Any() ? database.InferStatementParameterTypes( cn, commandText ) : [ ] ).ToDictionary( i => i.Name, StringComparer.Ordinal );
		var parameters = parameterNames.Select( name => inferredParametersByName.TryGetValue( name, out var parameter )
			                                                ? new ValueContainer(
				                                                name,
				                                                parameter.DataType,
				                                                parameter.DbTypeString,
				                                                parameter.Size,
				                                                parameter.NumericScale,
				                                                true,
				                                                cn.DatabaseInfo )
			                                                : new ValueContainer( name, cn.DatabaseInfo ) )
			.Materialize();

		// This replacement is necessary because SQL Server chooses to care about the type of the parameter passed to TOP.
		commandText = Regex.Replace( commandText, @"TOP\( *@\w+ *\)", "TOP 0", RegexOptions.IgnoreCase );

		using var command = cn.DatabaseInfo.CreateCommand();
		command.CommandText = commandText;

		foreach( var param in parameters )
			command.Parameters.Add( new DbCommandParameter( param.Name, param.GetDummyParameterValue( cn.DatabaseInfo ) ).GetAdoDotNetParameter( cn.DatabaseInfo ) );

		method( command );
		return parameters;
	}

	internal static string GetSchemaFolderName( Database database, DatabaseTable table ) =>
		database.GetDefaultSchema() is { Length: > 0 } defaultSchema && !table.Schema.Equals( defaultSchema, StringComparison.Ordinal )
			? table.Schema.Capitalize()
			: "";

	internal static string GetSchemaNamespaceSuffix( Database database, DatabaseTable table, bool omitAtSignPrefixIfNotRequired = false ) =>
		database.GetDefaultSchema() is { Length: > 0 } defaultSchema && !table.Schema.Equals( defaultSchema, StringComparison.Ordinal )
			? '.' + EwlStatics.GetCSharpIdentifier( table.Schema.Capitalize(), omitAtSignPrefixIfNotRequired: omitAtSignPrefixIfNotRequired )
			: "";

	internal static string GetMethodParams( IEnumerable<ValueContainer> parameters ) =>
		StringTools.ConcatenateWithDelimiter( ", ", parameters.Select( i => $"{i.DataTypeName} {i.Name}" ) );

	internal static void WriteAddParamBlock( TextWriter writer, string commandVariable, IEnumerable<ValueContainer> parameters, Database database ) {
		foreach( var param in parameters )
			writer.WriteLine(
				commandVariable + ".Parameters.Add( new DbCommandParameter( \"" + param.Name + "\", " + param.GetParameterValueExpression( param.Name ) +
				" ).GetAdoDotNetParameter( " + GetConnectionExpression( database ) + ".DatabaseInfo ) );" );
	}

	internal static string GetTableConditionInterfaceName( DatabaseConnection cn, Database database, DatabaseTable table ) =>
		$"{database.SecondaryDatabaseName}CommandConditions{GetSchemaNamespaceSuffix( database, table )}.{CommandConditionStatics.GetTableConditionInterfaceName( cn, table.Name )}";

	internal static string GetEqualityConditionClassName( DatabaseConnection cn, Database database, DatabaseTable table, Column column ) =>
		$"{database.SecondaryDatabaseName}CommandConditions{GetSchemaNamespaceSuffix( database, table )}.{CommandConditionStatics.GetTableEqualityConditionsClassName( cn, table.Name )}.{CommandConditionStatics.GetConditionClassName( column )}";

	internal static void WriteGetLatestRevisionsConditionMethod( TextWriter writer, string revisionIdColumn ) {
		writer.WriteLine( "private static InlineDbCommandCondition getLatestRevisionsCondition() {" );
		writer.WriteLine( "var provider = RevisionHistoryStatics.SystemProvider;" );
		writer.WriteLine( "return new InCondition( \"" + revisionIdColumn + "\", provider.GetLatestRevisionsQuery() );" );
		writer.WriteLine( "}" );
	}

	internal static string TableNameToPascal( this string tableName, DatabaseConnection cn ) =>
		cn.DatabaseInfo is MySqlInfo or OracleInfo ? tableName.OracleToEnglish().EnglishToPascal() : tableName;

	internal static string GetConnectionExpression( Database database ) {
		return "DataAccessState.Current.{0}".FormatWith(
			database.SecondaryDatabaseName.Any()
				? "GetSecondaryDatabaseConnection( SecondaryDatabaseNames.{0} )".FormatWith( database.SecondaryDatabaseName )
				: "PrimaryDatabaseConnection" );
	}

	internal static void WriteRevisionDeltaExtensionMethods( TextWriter writer, string retrievalClassName, IEnumerable<Column> columns ) {
		foreach( var column in columns ) {
			writer.WriteLine(
				"public static ValueDelta<{0}> Get{1}Delta( this RevisionDelta<{2}.Row> revisionDelta, string valueName = \"{3}\" ) {{".FormatWith(
					column.DataTypeName,
					column.PascalCasedName,
					retrievalClassName,
					column.PascalCasedName.CamelToEnglish() ) );
			writer.WriteLine( "return revisionDelta.GetValueDelta( valueName, i => i.{0} );".FormatWith( EwlStatics.GetCSharpIdentifier( column.PascalCasedName ) ) );
			writer.WriteLine( "}" );
		}
	}
}