using System.Collections.Immutable;
using System.Data.Common;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Collections;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems.StandardModification;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

internal static class DataAccessStatics {
	internal const string CSharpTemplateFileExtension = ".ewlt.cs";

	public static void GenerateDataAccessCode( TextWriter writer, DevelopmentInstallation installation ) {
		var baseNamespace = installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + ".DataAccess";

		if( installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration.Any( d => d.SecondaryDatabaseName.Length > 0 ) ) {
			writer.WriteLine();
			writer.WriteLine( "namespace " + baseNamespace + " {" );
			writer.WriteLine( "public class SecondaryDatabaseNames {" );
			foreach( var secondaryDatabase in installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration.Where( d => d.SecondaryDatabaseName.Length > 0 ) )
				writer.WriteLine( "public const string " + secondaryDatabase.SecondaryDatabaseName + " = \"" + secondaryDatabase.SecondaryDatabaseName + "\";" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		var initStatements = new List<string>();
		var templateBasePath = EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, "DataAccess" );
		foreach( var database in installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration )
			try {
				generateDataAccessCodeForDatabase(
					writer,
					baseNamespace,
					templateBasePath,
					database,
					database.SecondaryDatabaseName.Length == 0
						? installation.DevelopmentInstallationLogic.DevelopmentConfiguration.database
						: installation.DevelopmentInstallationLogic.DevelopmentConfiguration.secondaryDatabases.Single( sd => sd.name == database.SecondaryDatabaseName ),
					initStatements );
			}
			catch( Exception e ) {
				throw UserCorrectableException.CreateSecondaryException(
					"An exception occurred while generating data access logic for the {0}.".FormatWith( DatabaseOps.GetDatabaseNounPhrase( database ) ),
					e );
			}

		if( initStatements.Any() ) {
			writer.WriteLine();
			writer.WriteLine(
				"namespace {0}.Configuration.Providers {{".FormatWith(
					installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName ) );
			writer.WriteLine(
				"internal partial class {0}: SystemDataAccessProvider {{".FormatWith( EnterpriseWebLibrary.DataAccess.DataAccessStatics.ProviderName ) );
			writer.WriteLine( "void SystemDataAccessProvider.InitRetrievalCaches() {" );
			foreach( var statement in initStatements )
				writer.WriteLine( statement );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}
	}

	private static void generateDataAccessCodeForDatabase(
		TextWriter writer, string baseNamespace, string templateBasePath, Database database,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration, List<string> initStatements ) {
		var tables = DatabaseOps.GetDatabaseTables( database ).Materialize();
		var tableNames = tables.Select( i => i.name ).Materialize();

		ensureTablesExist( tableNames, configuration.SmallTables, "small" );

		ensureTablesExist( tableNames, configuration.TablesUsingRowVersionedDataCaching, "row-versioned data caching" );
		foreach( var table in tables.Where( i => i.hasModTable ).Select( i => i.name ) )
			if( configuration.TablesUsingRowVersionedDataCaching is {} specifiedTables && specifiedTables.Any( i => i.EqualsIgnoreCase( table ) ) )
				throw new UserCorrectableException(
					"Table {0} is cached using a modification table and therefore cannot also use row-versioned data caching.".FormatWith( table ) );

		ensureTablesExist( tableNames, configuration.revisionHistoryTables, "revision history" );

		ensureTablesExist( tableNames, configuration.WhitelistedTables, "whitelisted" );
		tableNames = tableNames.Where( table => configuration.WhitelistedTables == null || configuration.WhitelistedTables.Any( i => i.EqualsIgnoreCase( table ) ) )
			.Materialize();

		database.ExecuteDbMethod(
			delegate( DBConnection cn ) {
				foreach( var table in tables.Where( i => i.hasModTable ).Select( i => i.name ) ) {
					var columns = new TableColumns( cn, table, false );

					// This check ensures safety in the table-retrieval method that gets modified rows given a list of primary keys. This method uses inline SQL in order
					// to support a potentially large number of keys in a single query.
					var types = new[] { "System.Int32" }.Concat(
							database is InstallationSupportUtility.DatabaseAbstraction.Databases.Oracle ? "System.Decimal".ToCollection() : Enumerable.Empty<string>() )
						.ToImmutableHashSet( StringComparer.Ordinal );
					foreach( var column in columns.KeyColumns )
						if( !types.Contains( column.DataTypeName ) )
							throw new UserCorrectableException(
								"Table {0} is cached using a modification table but the {1} primary-key column is not numeric.".FormatWith( table, column.Name ) );

					var modTableColumns = Column.GetColumnsInQueryResults(
						cn,
						"SELECT * FROM {0}".FormatWith( table + DatabaseOps.GetModificationTableSuffix( database ) ),
						false,
						false );

					if( modTableColumns.Count != columns.KeyColumns.Count )
						throw new UserCorrectableException( "The modification table for {0} must have columns that match the primary key.".FormatWith( table ) );

					foreach( var column in columns.KeyColumns ) {
						var modTableColumn = modTableColumns.SingleOrDefault( i => string.Equals( i.Name, column.Name, StringComparison.OrdinalIgnoreCase ) );
						if( modTableColumn is null )
							throw new UserCorrectableException( "The modification table for {0} must have a {1} column.".FormatWith( table, column.Name ) );
					}
				}

				// database logic access - standard
				writer.WriteLine();
				TableConstantStatics.Generate( cn, writer, baseNamespace, database, tableNames );

				// database logic access - custom
				writer.WriteLine();
				RowConstantStatics.Generate( cn, writer, baseNamespace, database, configuration );

				// retrieval and modification commands - standard
				writer.WriteLine();
				CommandConditionStatics.Generate( cn, writer, baseNamespace, database, tableNames );

				writer.WriteLine();
				TableRetrievalStatics.Generate( cn, writer, baseNamespace, templateBasePath, database, tables, configuration, initStatements );

				writer.WriteLine();
				StandardModificationStatics.Generate( cn, writer, baseNamespace, templateBasePath, database, tables, configuration );

				// retrieval and modification commands - custom
				writer.WriteLine();
				QueryRetrievalStatics.Generate( cn, writer, baseNamespace, database, configuration );
				writer.WriteLine();
				CustomModificationStatics.Generate( cn, writer, baseNamespace, database, configuration );

				// other commands
				if( cn.DatabaseInfo is SqlServerInfo ) {
					writer.WriteLine();
					writer.WriteLine( "namespace {0} {{".FormatWith( baseNamespace ) );
					writer.WriteLine( "public static class {0}MainSequence {{".FormatWith( database.SecondaryDatabaseName ) );
					writer.WriteLine( "public static int GetNextValue() {" );
					writer.WriteLine( "var command = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
					writer.WriteLine( "command.CommandText = \"SELECT NEXT VALUE FOR MainSequence\";" );
					writer.WriteLine( "return (int)" + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteScalarCommand( command );" );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				}
				else if( cn.DatabaseInfo is OracleInfo ) {
					writer.WriteLine();
					SequenceStatics.Generate( cn, writer, baseNamespace, database );
					writer.WriteLine();
					ProcedureStatics.Generate( cn, writer, baseNamespace, database );
				}
			} );
	}

	private static void ensureTablesExist( IReadOnlyCollection<string> databaseTables, IEnumerable<string>? specifiedTables, string tableAdjective ) {
		if( specifiedTables == null )
			return;
		var nonexistentTables = specifiedTables.Where( specifiedTable => databaseTables.All( i => !i.EqualsIgnoreCase( specifiedTable ) ) ).ToArray();
		if( nonexistentTables.Any() )
			throw new UserCorrectableException(
				tableAdjective.CapitalizeString() + " " + ( nonexistentTables.Length > 1 ? "tables" : "table" ) + " " +
				StringTools.GetEnglishListPhrase( nonexistentTables.Select( i => "'" + i + "'" ), true ) + " " + ( nonexistentTables.Length > 1 ? "do" : "does" ) +
				" not exist." );
	}

	/// <summary>
	/// Given a string, returns all instances of @abc in an ordered set containing abc (the token without the @ sign). If a token is used more than once, it
	/// only appears in the list once. A different prefix may be used for certain databases.
	/// </summary>
	internal static ListSet<string> GetNamedParamList( DatabaseInfo info, string statement ) {
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
	/// Given raw query text such as that from Development.xml, returns a command that has had all of its parameters filled in with
	/// good dummy values and is ready to safely execute using schema only or key info behavior.
	/// </summary>
	internal static DbCommand GetCommandFromRawQueryText( DBConnection cn, string commandText ) {
		// This replacement is necessary because SQL Server chooses to care about the type of the parameter passed to TOP.
		commandText = Regex.Replace( commandText, @"TOP\( *@\w+ *\)", "TOP 0", RegexOptions.IgnoreCase );

		var cmd = cn.DatabaseInfo.CreateCommand();
		cmd.CommandText = commandText;
		foreach( var param in GetNamedParamList( cn.DatabaseInfo, cmd.CommandText ) )
			cmd.Parameters.Add(
				new DbCommandParameter( param, new DbParameterValue( cn.DatabaseInfo is MySqlInfo ? 0 : "0" ) ).GetAdoDotNetParameter( cn.DatabaseInfo ) );
		return cmd;
	}

	internal static string GetMethodParamsFromCommandText( DatabaseInfo info, string commandText ) {
		return StringTools.ConcatenateWithDelimiter( ", ", GetNamedParamList( info, commandText ).Select( i => "object " + i ).ToArray() );
	}

	internal static void WriteAddParamBlockFromCommandText(
		TextWriter writer, string commandVariable, DatabaseInfo info, string commandText, Database database ) {
		foreach( var param in GetNamedParamList( info, commandText ) )
			writer.WriteLine(
				commandVariable + ".Parameters.Add( new DbCommandParameter( \"" + param + "\", new DbParameterValue( " + param + " ) ).GetAdoDotNetParameter( " +
				GetConnectionExpression( database ) + ".DatabaseInfo ) );" );
	}

	internal static bool IsRevisionHistoryTable( string table, EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		return configuration.revisionHistoryTables != null &&
		       configuration.revisionHistoryTables.Any( revisionHistoryTable => revisionHistoryTable.EqualsIgnoreCase( table ) );
	}

	internal static string GetTableConditionInterfaceName( DBConnection cn, Database database, string table ) {
		return database.SecondaryDatabaseName + "CommandConditions." + CommandConditionStatics.GetTableConditionInterfaceName( cn, table );
	}

	internal static string GetEqualityConditionClassName( DBConnection cn, Database database, string tableName, Column column ) {
		return database.SecondaryDatabaseName + "CommandConditions." + CommandConditionStatics.GetTableEqualityConditionsClassName( cn, tableName ) + "." +
		       CommandConditionStatics.GetConditionClassName( column );
	}

	internal static void WriteGetLatestRevisionsConditionMethod( TextWriter writer, string revisionIdColumn ) {
		writer.WriteLine( "private static InlineDbCommandCondition getLatestRevisionsCondition() {" );
		writer.WriteLine( "var provider = RevisionHistoryStatics.SystemProvider;" );
		writer.WriteLine( "return new InCondition( \"" + revisionIdColumn + "\", provider.GetLatestRevisionsQuery() );" );
		writer.WriteLine( "}" );
	}

	internal static string TableNameToPascal( this string tableName, DBConnection cn ) {
		return cn.DatabaseInfo is MySqlInfo ? tableName.OracleToEnglish().EnglishToPascal() : tableName;
	}

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
			writer.WriteLine(
				"return revisionDelta.GetValueDelta( valueName, i => i.{0} );".FormatWith( EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) ) );
			writer.WriteLine( "}" );
		}
	}
}