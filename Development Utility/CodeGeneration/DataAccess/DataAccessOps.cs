using System.Collections.Immutable;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems.StandardModification;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

internal static class DataAccessOps {
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
				"namespace {0}.Providers {{".FormatWith( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName ) );
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
		var migrationTable = database.Info is null ? null : DataMigrationOps.GetMigrationTableName( database.Info );
		var tables = DatabaseOps.GetDatabaseTables( database )
			.Where( i => migrationTable is null || !i.tableName.Name.Equals( migrationTable, StringComparison.Ordinal ) )
			.Materialize();
		var tableNames = tables.Select( i => i.tableName ).Materialize();

		ensureTablesExist( database, tableNames, configuration.SmallTables, "small" );

		ensureTablesExist( database, tableNames, configuration.TablesUsingRowVersionedDataCaching, "row-versioned data caching" );
		foreach( var table in tables.Where( i => i.hasModTable ).Select( i => i.tableName ) )
			if( configuration.TablesUsingRowVersionedDataCaching is {} specifiedTables &&
			    specifiedTables.Any( i => tableMatchesSpecifiedName( database, table, i ) ) )
				throw new UserCorrectableException(
					"Table {0} is cached using a modification table and therefore cannot also use row-versioned data caching.".FormatWith( table.QualifiedName ) );

		ensureTablesExist( database, tableNames, configuration.revisionHistoryTables, "revision history" );

		ensureTablesExist( database, tableNames, configuration.WhitelistedTables, "whitelisted" );
		tables = tables.Where( table =>
				configuration.WhitelistedTables == null || configuration.WhitelistedTables.Any( i => tableMatchesSpecifiedName( database, table.tableName, i ) ) )
			.Materialize();
		tableNames = tables.Select( i => i.tableName ).Materialize();

		database.ExecuteDbMethod(
			delegate( DatabaseConnection cn ) {
				foreach( var table in tables.Where( i => i.hasModTable ).Select( i => i.tableName ) ) {
					var columns = new TableColumns( cn, table, false );

					if( !columns.HasKeyColumns )
						throw new UserCorrectableException( $"Table {table.QualifiedName} is cached using a modification table but does not have a primary key." );

					// This check ensures safety in the table-retrieval method that gets modified rows given a list of primary keys. This method uses inline SQL in order
					// to support a potentially large number of keys in a single query.
					var types = new[] { "System.Int32" }.Concat( cn.DatabaseInfo is OracleInfo ? "System.Decimal".ToCollection() : Enumerable.Empty<string>() )
						.ToImmutableHashSet( StringComparer.Ordinal );
					foreach( var column in columns.KeyColumns )
						if( !types.Contains( column.DataTypeName ) )
							throw new UserCorrectableException(
								"Table {0} is cached using a modification table but the {1} primary-key column is not numeric.".FormatWith(
									table.QualifiedName,
									column.Name ) );

					var modTableColumns = Column.GetColumnsInQueryResults(
						cn,
						"SELECT * FROM {0}".FormatWith( ( table with { Name = table.Name + DatabaseOps.GetModificationTableSuffix( database ) } ).QualifiedName ),
						false,
						false );

					if( modTableColumns.Count != columns.KeyColumns.Count )
						throw new UserCorrectableException(
							"The modification table for {0} must have columns that match the primary key.".FormatWith( table.QualifiedName ) );

					foreach( var column in columns.KeyColumns ) {
						var modTableColumn = modTableColumns.SingleOrDefault( i => string.Equals( i.Name, column.Name, StringComparison.OrdinalIgnoreCase ) );
						if( modTableColumn is null )
							throw new UserCorrectableException( "The modification table for {0} must have a {1} column.".FormatWith( table.QualifiedName, column.Name ) );
					}
				}

				// database logic access - standard
				writer.WriteLine();
				TableConstantStatics.Generate( cn, writer, baseNamespace, database, tableNames );

				// database logic access - custom
				writer.WriteLine();
				RowConstantStatics.Generate(
					cn,
					writer,
					baseNamespace,
					database,
					configuration,
					specifiedName => tableNames.Single( i => tableMatchesSpecifiedName( database, i, specifiedName ) ) );

				// retrieval and modification commands - standard
				writer.WriteLine();
				CommandConditionStatics.Generate( cn, writer, baseNamespace, database, tableNames );

				writer.WriteLine();
				TableRetrievalStatics.Generate(
					cn,
					writer,
					baseNamespace,
					templateBasePath,
					database,
					tables.Select( table => ( table.tableName,
						                        configuration.SmallTables is {} smallTables &&
						                        smallTables.Any( i => tableMatchesSpecifiedName( database, table.tableName, i ) ), table.hasModTable,
						                        configuration.TablesUsingRowVersionedDataCaching is {} rvdcTables &&
						                        rvdcTables.Any( i => tableMatchesSpecifiedName( database, table.tableName, i ) ),
						                        configuration.revisionHistoryTables is {} rhTables &&
						                        rhTables.Any( i => tableMatchesSpecifiedName( database, table.tableName, i ) ) ) ),
					initStatements );

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
					writer.WriteLine( "return (int)" + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteScalarCommand( command )!;" );
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

	private static void ensureTablesExist(
		Database database, IReadOnlyCollection<DatabaseTable> databaseTables, IEnumerable<string>? specifiedTables, string tableAdjective ) {
		if( specifiedTables == null )
			return;
		var nonexistentTables = specifiedTables.Where( specifiedTable => databaseTables.All( i => !tableMatchesSpecifiedName( database, i, specifiedTable ) ) )
			.Materialize();
		if( nonexistentTables.Any() )
			throw new UserCorrectableException(
				tableAdjective.Capitalize() + " " + ( nonexistentTables.Count > 1 ? "tables" : "table" ) + " " +
				StringTools.GetEnglishListPhrase( nonexistentTables.Select( i => "'" + i + "'" ), true ) + " " + ( nonexistentTables.Count > 1 ? "do" : "does" ) +
				" not exist." );
	}

	private static bool tableMatchesSpecifiedName( Database database, DatabaseTable table, string specifiedName ) =>
		database.GetDefaultSchema() is { Length: > 0 } defaultSchema && !specifiedName.Contains( '.', StringComparison.Ordinal )
			? table.Schema.EqualsIgnoreCase( defaultSchema ) && table.Name.EqualsIgnoreCase( specifiedName )
			: table.QualifiedName.EqualsIgnoreCase( specifiedName );
}