using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems.StandardModification;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;

internal static class TableRetrievalStatics {
	private const string oracleRowVersionDataType = "decimal";

	internal static void Generate(
		DatabaseConnection cn, TextWriter writer, string baseNamespace, string templateBasePath, Database database,
		IEnumerable<( string name, bool hasModTable )> tables, EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration,
		List<string> initStatements ) {
		var subsystemName = "{0}TableRetrieval".FormatWith( database.SecondaryDatabaseName );
		var subsystemNamespace = "namespace {0}.{1}".FormatWith( baseNamespace, subsystemName );

		writer.WriteLine( "{0} {{".FormatWith( subsystemNamespace ) );
		foreach( var table in tables ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "Contains logic that retrieves rows from the " + table.name + " table." );
			writer.WriteLine( "public static partial class " + GetClassName( cn, table.name ) + " {" );

			var isRevisionHistoryTable = DataAccessStatics.IsRevisionHistoryTable( table.name, configuration );
			var columns = new TableColumns( cn, table.name, isRevisionHistoryTable );

			// Write nested classes.
			RetrievalStatics.WriteRowClasses(
				writer,
				columns.AllColumns,
				columns.HasKeyColumns ? columns.KeyColumns : null,
				_ => {
					if( !isRevisionHistoryTable )
						return;
					writer.WriteLine(
						"public UserTransaction Transaction { get { return RevisionHistoryStatics.UserTransactionsById[ RevisionHistoryStatics.RevisionsById[ System.Convert.ToInt32( " +
						EwlStatics.GetCSharpIdentifier( columns.PrimaryKeyAndRevisionIdColumn!.PascalCasedNameExceptForOracle ) + " ) ].UserTransactionId ]; } }" );
				},
				_ => {
					if( !columns.HasKeyColumns || !columns.DataColumns.Any() )
						return;

					var modClass = database.SecondaryDatabaseName + "Modification." +
					               StandardModificationStatics.GetClassName( cn, table.name, isRevisionHistoryTable, isRevisionHistoryTable );
					var revisionHistorySuffix = StandardModificationStatics.GetRevisionHistorySuffix( isRevisionHistoryTable );
					writer.WriteLine( "public " + modClass + " ToModification" + revisionHistorySuffix + "() {" );
					writer.WriteLine(
						"return " + modClass + ".CreateForSingleRowUpdate" + revisionHistorySuffix + "( " + StringTools.ConcatenateWithDelimiter(
							", ",
							columns.AllColumnsExceptRowVersion.Select( i => EwlStatics.GetCSharpIdentifier( i.PascalCasedNameExceptForOracle ) ).ToArray() ) + " );" );
					writer.WriteLine( "}" );
				} );
			writeCacheClass( cn, writer, database, table.name, columns, table.hasModTable, isRevisionHistoryTable );

			if( table.hasModTable ) {
				CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
				writer.WriteLine( "internal static void __Init() {" );
				writer.WriteLine( "__get{0}();".FormatWith( getTableCacheName( false ) ) );
				if( isRevisionHistoryTable )
					writer.WriteLine( "__get{0}();".FormatWith( getTableCacheName( true ) ) );
				writer.WriteLine( "}" );
				writer.WriteLine();
				writeTableCacheMethods( cn, writer, database, table.name, columns, false );
				if( isRevisionHistoryTable )
					writeTableCacheMethods( cn, writer, database, table.name, columns, true );
			}

			var isSmallTable = configuration.SmallTables != null && configuration.SmallTables.Any( i => i.EqualsIgnoreCase( table.name ) );

			var tableUsesRowVersionedCaching = configuration.TablesUsingRowVersionedDataCaching != null &&
			                                   configuration.TablesUsingRowVersionedDataCaching.Any( i => i.EqualsIgnoreCase( table.name ) );
			if( tableUsesRowVersionedCaching && ( !columns.HasKeyColumns || ( columns.RowVersionColumn is null && cn.DatabaseInfo is not OracleInfo ) ) )
				throw new UserCorrectableException(
					cn.DatabaseInfo is MySqlInfo ? "Row-versioned data caching cannot currently be used with MySQL databases." :
					cn.DatabaseInfo is OracleInfo ? "Row-versioned data caching can only be used with the {0} table if it has a primary key.".FormatWith( table.name ) :
					"Row-versioned data caching can only be used with the {0} table if it has a primary key and a rowversion column.".FormatWith( table.name ) );

			if( isSmallTable || table.hasModTable )
				writeGetAllRowsMethod( writer, database, isSmallTable, table.hasModTable, isRevisionHistoryTable, false );
			writeGetRowsMatchingConditionsMethod(
				cn,
				writer,
				database,
				table.name,
				columns,
				isSmallTable,
				table.hasModTable,
				tableUsesRowVersionedCaching,
				isRevisionHistoryTable,
				false );
			if( isRevisionHistoryTable ) {
				if( isSmallTable || table.hasModTable )
					writeGetAllRowsMethod( writer, database, isSmallTable, table.hasModTable, true, true );
				writeGetRowsMatchingConditionsMethod(
					cn,
					writer,
					database,
					table.name,
					columns,
					isSmallTable,
					table.hasModTable,
					tableUsesRowVersionedCaching,
					true,
					true );
			}

			if( columns.HasKeyColumns )
				writeGetRowMatchingPkMethods(
					cn,
					writer,
					database,
					table.name,
					columns,
					isSmallTable,
					table.hasModTable,
					tableUsesRowVersionedCaching,
					isRevisionHistoryTable );

			if( isRevisionHistoryTable )
				DataAccessStatics.WriteGetLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn!.Name );

			if( tableUsesRowVersionedCaching ) {
				var keyTupleTypeArguments = getPkAndVersionTupleTypeArguments( cn, columns );

				writer.WriteLine( "private static " + "Cache<System.Tuple<" + keyTupleTypeArguments + ">, BasicRow>" + " getRowsByPkAndVersion() {" );
				writer.WriteLine(
					"return AppMemoryCache.GetCacheValue<{0}>( \"{1}\", () => new {0}( i => System.Tuple.Create( {2} ) ) ).RowsByPkAndVersion;".FormatWith(
						"VersionedRowDataCache<System.Tuple<{0}>, System.Tuple<{1}>, BasicRow>".FormatWith( getPkTupleTypeArguments( columns ), keyTupleTypeArguments ),
						database.SecondaryDatabaseName + table.name.TableNameToPascal( cn ) + "TableRetrievalRowsByPkAndVersion",
						StringTools.ConcatenateWithDelimiter(
							", ",
							Enumerable.Range( 1, columns.KeyColumns.Count ).Select( i => "i.Item{0}".FormatWith( i ) ).ToArray() ) ) );
				writer.WriteLine( "}" );
			}

			// Initially we did not generate this method for small tables, but we found a need for it when the cache is disabled since that will cause
			// GetRowMatchingId to repeatedly query.
			if( columns.HasKeyColumns && columns.KeyColumns.Count == 1 && columns.KeyColumns.Single().Name.ToLower().EndsWith( "id" ) )
				writeToIdDictionaryMethod( writer, columns );

			if( isRevisionHistoryTable )
				DataAccessStatics.WriteRevisionDeltaExtensionMethods( writer, GetClassName( cn, table.name ), columns.DataColumns );

			writer.WriteLine( "}" ); // class

			if( table.hasModTable )
				initStatements.Add( "{0}.{1}.{2}.__Init();".FormatWith( baseNamespace, subsystemName, GetClassName( cn, table.name ) ) );

			var templateClassName = GetClassName( cn, table.name );
			var templateFilePath = EwlStatics.CombinePaths( templateBasePath, subsystemName, templateClassName );
			IoMethods.DeleteFile( templateFilePath + DataAccessStatics.CSharpTemplateFileExtension );

			// If a real file exists, don’t create a template.
			if( File.Exists( templateFilePath + ".cs" ) )
				continue;

			using var templateWriter = IoMethods.GetTextWriterForWrite( templateFilePath + DataAccessStatics.CSharpTemplateFileExtension );
			templateWriter.WriteLine( "{0};".FormatWith( subsystemNamespace ) );
			templateWriter.WriteLine();
			templateWriter.WriteLine( "partial class {0} {{".FormatWith( templateClassName ) );
			templateWriter.WriteLine(
				"	// IMPORTANT: Change extension from \"{0}\" to \".cs\" before editing.".FormatWith( DataAccessStatics.CSharpTemplateFileExtension ) );
			templateWriter.WriteLine( "}" );
		}
		writer.WriteLine( "}" ); // namespace
	}

	private static void writeCacheClass(
		DatabaseConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool hasModTable, bool isRevisionHistoryTable ) {
		writer.WriteLine( "private partial class Cache {" );
		writer.WriteLine(
			"internal static Cache Current => DataAccessState.Current.GetCacheValue( \"{0}\", () => new Cache() );".FormatWith(
				database.SecondaryDatabaseName + table.TableNameToPascal( cn ) + "TableRetrieval" ) );
		if( hasModTable ) {
			writer.WriteLine(
				"public readonly Lazy<{0}.DataRetriever> {1}DataRetriever = new( () => __get{1}().GetDataRetriever( __get{1}RowModificationCounts(), __get{1}Rows ), LazyThreadSafetyMode.None );"
					.FormatWith( getTableCacheType( tableColumns ), getTableCacheName( false ) ) );
			if( isRevisionHistoryTable )
				writer.WriteLine(
					"public readonly Lazy<{0}.DataRetriever> {1}DataRetriever = new( () => __get{1}().GetDataRetriever( __get{1}RowModificationCounts(), __get{1}Rows ), LazyThreadSafetyMode.None );"
						.FormatWith( getTableCacheType( tableColumns ), getTableCacheName( true ) ) );
		}
		writer.WriteLine( "internal readonly TableRetrievalQueryCache<Row> Queries = new TableRetrievalQueryCache<Row>();" );
		if( tableColumns.HasKeyColumns )
			writer.WriteLine(
				"internal readonly Cache<{0}, Row> RowsByPk = new( false );".FormatWith( RetrievalStatics.GetColumnTupleTypeName( tableColumns.KeyColumns ) ) );
		if( isRevisionHistoryTable )
			writer.WriteLine(
				"internal readonly Cache<{0}, Row> LatestRevisionRowsByPk = new( false );".FormatWith(
					RetrievalStatics.GetColumnTupleTypeName( tableColumns.KeyColumns ) ) );
		writer.WriteLine( "private Cache() {}" );
		writer.WriteLine( "}" );
	}

	private static void writeTableCacheMethods(
		DatabaseConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool excludePreviousRevisions ) {
		CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
		writer.WriteLine( "private static {0} __get{1}() =>".FormatWith( getTableCacheType( tableColumns ), getTableCacheName( excludePreviousRevisions ) ) );
		writer.WriteLine(
			"AppMemoryCache.GetCacheValue( \"dataAccess-{0}\", () => {1} );".FormatWith(
				database.SecondaryDatabaseName + table.TableNameToPascal( cn ) + getTableCacheName( excludePreviousRevisions ),
				"new DataAccessState().ExecuteWithThis( () => {0}.ExecuteWithConnectionOpen( () => {0}.ExecuteInTransaction( () => new {1}( __get{2}Rows( null ), __get{2}RowModificationCounts(), cacheRecreator => new DataAccessState().ExecuteWithThis( () => {0}.ExecuteWithConnectionOpen( () => {0}.ExecuteInTransaction( () => cacheRecreator( __get{2}RowModificationCounts(), __get{2}Rows ) ) ) ) ) ) ) )"
					.FormatWith(
						DataAccessStatics.GetConnectionExpression( database ),
						getTableCacheType( tableColumns ),
						getTableCacheName( excludePreviousRevisions ) ) ) );

		CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
		writer.WriteLine(
			"private static IReadOnlyCollection<BasicRow> __get{0}Rows( IReadOnlyCollection<{1}>? primaryKeys ) {{".FormatWith(
				getTableCacheName( excludePreviousRevisions ),
				RetrievalStatics.GetColumnTupleTypeName( tableColumns.KeyColumns ) ) );
		writer.WriteLine( "List<BasicRow> results;" );
		writer.WriteLine( "if( primaryKeys is null ) {" );
		writer.WriteLine( "var countCommand = {0}.DatabaseInfo.CreateCommand();".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine(
			"countCommand.CommandText = \"SELECT {0} FROM {1}\";".FormatWith( cn.DatabaseInfo is SqlServerInfo ? "COUNT_BIG(*)" : "COUNT(*)", table ) );
		writer.WriteLine(
			"results = new List<BasicRow>( (int)({0}){1}.ExecuteScalarCommand( countCommand )! );".FormatWith(
				cn.DatabaseInfo is OracleInfo ? "decimal" : "long",
				DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine( "var command = {0};".FormatWith( getInlineSelectExpression( table, tableColumns, "\"*\"", "true" ) ) );
		if( excludePreviousRevisions )
			writer.WriteLine( "{0}.AddConditions( getLatestRevisionsCondition().ToCollection() );".FormatWith( "command" ) );
		writer.WriteLine(
			"command.Execute( {0}, r => {{ while( r.Read() ) results.Add( new BasicRow( r ) ); }} );".FormatWith(
				DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine( "}" );
		writer.WriteLine( "else {" );
		writer.WriteLine( "results = new List<BasicRow>( primaryKeys.Count );" );
		writer.WriteLine( "var command = {0}.DatabaseInfo.CreateCommand();".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine(
			"command.CommandText = \"SELECT * FROM {0} WHERE \" + StringTools.ConcatenateWithDelimiter( \" OR \", primaryKeys.Select( i => $\"( {1} )\" ) );"
				.FormatWith(
					table,
					tableColumns.KeyColumns.Count < 2
						? "{0} = {{i}}".FormatWith( tableColumns.KeyColumns.Single().DelimitedIdentifier.EscapeForLiteral() )
						: StringTools.ConcatenateWithDelimiter(
							" AND ",
							tableColumns.KeyColumns.Select(
								i => "{0} = {{i.{1}}}".FormatWith( i.DelimitedIdentifier.EscapeForLiteral(), EwlStatics.GetCSharpIdentifier( i.CamelCasedName ) ) ) ) ) );
		writer.WriteLine(
			"{0}.ExecuteReaderCommand( command, r => {{ while( r.Read() ) results.Add( new BasicRow( r ) ); }} );".FormatWith(
				DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine( "}" );
		writer.WriteLine( "return results;" );
		writer.WriteLine( "}" );

		CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
		writer.WriteLine(
			"private static IReadOnlyCollection<( {0}, long )> __get{1}RowModificationCounts() {{".FormatWith(
				RetrievalStatics.GetColumnTupleTypeName( tableColumns.KeyColumns ),
				getTableCacheName( excludePreviousRevisions ) ) );
		writer.WriteLine( "var command = {0}.DatabaseInfo.CreateCommand();".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine(
			"command.CommandText = \"SELECT {0}, {1} FROM {2}\";".FormatWith(
				StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.DelimitedIdentifier.EscapeForLiteral() ) ),
				cn.DatabaseInfo is SqlServerInfo ? "COUNT_BIG(*)" : "COUNT(*)",
				table + DatabaseOps.GetModificationTableSuffix( database ) ) );
		if( excludePreviousRevisions ) {
			writer.WriteLine( "command.CommandText += \" WHERE \";" );
			writer.WriteLine(
				"getLatestRevisionsCondition().AddToCommand( command, {0}.DatabaseInfo, \"latestRevisions\" );".FormatWith(
					DataAccessStatics.GetConnectionExpression( database ) ) );
		}
		writer.WriteLine(
			"command.CommandText += \" GROUP BY {0}\";".FormatWith(
				StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.DelimitedIdentifier.EscapeForLiteral() ) ) ) );
		writer.WriteLine( "var results = new List<( {0}, long )>();".FormatWith( RetrievalStatics.GetColumnTupleTypeName( tableColumns.KeyColumns ) ) );
		writer.WriteLine(
			"{0}.ExecuteReaderCommand( command, r => {{ while( r.Read() ) results.Add( ( {1}, r.GetInt64( {2} ) ) ); }} );".FormatWith(
				DataAccessStatics.GetConnectionExpression( database ),
				RetrievalStatics.GetColumnTupleExpression(
					tableColumns.KeyColumns.Select( ( c, i ) => c.GetDataReaderValueExpression( "r", ordinalOverride: i ) ).Materialize() ),
				tableColumns.KeyColumns.Count ) );
		writer.WriteLine( "return results;" );
		writer.WriteLine( "}" );
	}

	private static string getTableCacheType( TableColumns tableColumns ) =>
		"RowModificationCountTableCache<BasicRow, {0}>".FormatWith( RetrievalStatics.GetColumnTupleTypeName( tableColumns.KeyColumns ) );

	private static void writeGetAllRowsMethod(
		TextWriter writer, Database database, bool isSmallTable, bool hasModTable, bool isRevisionHistoryTable, bool excludePreviousRevisions ) {
		var revisionHistorySuffix = isRevisionHistoryTable && !excludePreviousRevisions ? "IncludingPreviousRevisions" : "";
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Retrieves {0} from the table. ".FormatWith( isSmallTable ? "all rows" : "rows" ) +
			( hasModTable ? "Since the table is cached, the rows are NOT ordered in a stable way." : "The rows are ordered in a stable way." ) +
			( isSmallTable
				  ? ""
				  : " If you specify a predicate, we recommend storing the results of this call in the Cache object using a key that corresponds to the predicate." ) );
		var allRowsStatement = "return GetRowsMatchingConditions{0}();".FormatWith( revisionHistorySuffix );
		writer.WriteLine(
			"public static IEnumerable<Row> {0} {{".FormatWith(
				isSmallTable
					? "GetAllRows{0}()".FormatWith( revisionHistorySuffix )
					: "GetRows{0}( Func<BasicRow, bool>? predicate = null )".FormatWith( revisionHistorySuffix ) ) );

		if( isSmallTable )
			writer.WriteLine( allRowsStatement );
		else {
			writer.WriteLine( "if( predicate is null ) " + allRowsStatement );
			writer.WriteLine( "var cache = Cache.Current;" );
			writer.WriteLine(
				"return {0}.ExecuteInTransaction( () => cache.{1}DataRetriever.Value.GetRows() ).Where( predicate ).Select( basicRow => {{".FormatWith(
					DataAccessStatics.GetConnectionExpression( database ),
					getTableCacheName( excludePreviousRevisions ) ) );
			writer.WriteLine( "var row = new Row( basicRow );" );
			writer.WriteLine( "cache.RowsByPk.TryAdd( basicRow.PrimaryKey, row );" );
			if( excludePreviousRevisions )
				writer.WriteLine( "cache.LatestRevisionRowsByPk.TryAdd( basicRow.PrimaryKey, row );" );
			writer.WriteLine( "return row;" );
			writer.WriteLine( "} );" );
		}

		writer.WriteLine( "}" );
	}

	private static void writeGetRowsMatchingConditionsMethod(
		DatabaseConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isSmallTable, bool hasModTable,
		bool tableUsesRowVersionedCaching, bool isRevisionHistoryTable, bool excludePreviousRevisions ) {
		// header
		var methodName = "GetRows" + ( isSmallTable || hasModTable ? "MatchingConditions" : "" ) +
		                 ( isRevisionHistoryTable && !excludePreviousRevisions ? "IncludingPreviousRevisions" : "" );
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			StringTools.ConcatenateWithDelimiter(
				" ",
				"Retrieves the rows from the table that match the specified conditions.",
				isSmallTable || hasModTable
					? "Since the table is {0}, you should only use this method if you cannot filter the rows in code.".FormatWith(
						isSmallTable ? "specified as small" : "cached" )
					: "",
				hasModTable ? "The rows are ordered in a stable way UNLESS you do not specify any conditions." : "The rows are ordered in a stable way." ) );
		writer.WriteLine(
			"public static IEnumerable<Row> " + methodName + "( params " + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) +
			"[] conditions ) {" );


		// body

		writer.WriteLine( "var cache = Cache.Current;" );

		if( tableColumns.HasKeyColumns ) {
			// If it's a primary key query, use RowsByPk if possible.
			foreach( var i in tableColumns.KeyColumns ) {
				var equalityConditionClassName = DataAccessStatics.GetEqualityConditionClassName( cn, database, table, i );
				writer.WriteLine( "var {0}Condition = conditions.OfType<{1}>().FirstOrDefault();".FormatWith( i.CamelCasedName, equalityConditionClassName ) );
			}
			var pkConditionVariableNames = tableColumns.KeyColumns.Select( i => i.CamelCasedName + "Condition" );
			writer.WriteLine(
				"var isPkQuery = " + StringTools.ConcatenateWithDelimiter( " && ", pkConditionVariableNames.Select( i => i + " is not null" ).ToArray() ) +
				" && conditions.Count() == " + tableColumns.KeyColumns.Count + ";" );
			writer.WriteLine( "if( isPkQuery ) {" );
			writer.WriteLine(
				"var pk = {0};".FormatWith( RetrievalStatics.GetColumnTupleExpression( pkConditionVariableNames.Select( i => i + "!.Value" ).Materialize() ) ) );
			writer.WriteLine(
				"if( cache." + ( excludePreviousRevisions ? "LatestRevision" : "" ) + "RowsByPk.TryGetValue( pk, out var row ) ) return row.ToCollection();" );
			if( hasModTable ) {
				writer.WriteLine( "BasicRow? basicRow = null;" );
				writer.WriteLine(
					"if( {0}.ExecuteInTransaction( () => cache.{1}DataRetriever.Value.TryGetRowMatchingPk( pk, out basicRow ) ) ) {{".FormatWith(
						DataAccessStatics.GetConnectionExpression( database ),
						getTableCacheName( excludePreviousRevisions ) ) );
				writer.WriteLine( "row = new Row( basicRow! );" );
				writer.WriteLine( "cache.RowsByPk.TryAdd( basicRow!.PrimaryKey, row );" );
				if( excludePreviousRevisions )
					writer.WriteLine( "cache.LatestRevisionRowsByPk.TryAdd( basicRow!.PrimaryKey, row );" );
				writer.WriteLine( "return row.ToCollection();" );
				writer.WriteLine( "}" );
			}
			writer.WriteLine( "}" );
		}

		var commandConditionsExpression = "conditions.Select( i => i.CommandCondition )";
		if( excludePreviousRevisions )
			commandConditionsExpression += ".Append( getLatestRevisionsCondition() )";
		writer.WriteLine( "return cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
		writeResultSetCreatorBody(
			cn,
			writer,
			database,
			table,
			tableColumns,
			hasModTable,
			tableUsesRowVersionedCaching,
			excludePreviousRevisions,
			tableColumns.HasKeyColumns ? "!isPkQuery" : "true" );
		writer.WriteLine( "} );" );

		writer.WriteLine( "}" );
	}

	private static void writeGetRowMatchingPkMethods(
		DatabaseConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isSmallTable, bool hasModTable,
		bool tableUsesRowVersionedCaching, bool isRevisionHistoryTable ) {
		var pkIsId = tableColumns.KeyColumns.Count == 1 && tableColumns.KeyColumns.Single().Name.ToLower().EndsWith( "id" );
		var methodName = pkIsId ? "GetRowMatchingId" : "GetRowMatchingPk";
		var pkParameters = pkIsId
			                   ? "{0} id".FormatWith( tableColumns.KeyColumns.Single().DataTypeName )
			                   : StringTools.ConcatenateWithDelimiter(
				                   ", ",
				                   tableColumns.KeyColumns.Select( i => "{0} {1}".FormatWith( i.DataTypeName, i.CamelCasedName ) ).ToArray() );

		writeMethod( false );
		writeMethod( true );
		return;

		void writeMethod( bool isTry ) {
			if( isTry )
				writer.WriteLine( $$"""public static bool Try{{methodName}}( {{pkParameters}}, [ MaybeNullWhen( false ) ] out Row row ) {""" );
			else
				writer.WriteLine( $$"""public static Row {{methodName}}( {{pkParameters}} ) {""" );
			if( isSmallTable ) {
				writer.WriteLine( "var cache = Cache.Current;" );
				var commandConditionsExpression = isRevisionHistoryTable ? "getLatestRevisionsCondition().ToCollection()" : "new InlineDbCommandCondition[ 0 ]";
				writer.WriteLine( "cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
				writeResultSetCreatorBody( cn, writer, database, table, tableColumns, hasModTable, tableUsesRowVersionedCaching, isRevisionHistoryTable, "true" );
				writer.WriteLine( "} );" );

				var cacheExpression = "cache.{0}RowsByPk".FormatWith( isRevisionHistoryTable ? "LatestRevision" : "" );
				var keyExpression = pkIsId ? "id" : RetrievalStatics.GetColumnTupleExpression( tableColumns.KeyColumns.Select( i => i.CamelCasedName ).Materialize() );
				writer.WriteLine( isTry ? $"return {cacheExpression}.TryGetValue( {keyExpression}, out row );" : $"return {cacheExpression}[ {keyExpression} ];" );
			}
			else {
				writer.WriteLine(
					"var rows = Get{0}( {1} );".FormatWith(
						hasModTable ? "RowsMatchingConditions" : "Rows",
						pkIsId
							? "new {0}( id )".FormatWith( DataAccessStatics.GetEqualityConditionClassName( cn, database, table, tableColumns.KeyColumns.Single() ) )
							: StringTools.ConcatenateWithDelimiter(
								", ",
								tableColumns.KeyColumns.Select(
										i => "new {0}( {1} )".FormatWith( DataAccessStatics.GetEqualityConditionClassName( cn, database, table, i ), i.CamelCasedName ) )
									.ToArray() ) ) );
				if( isTry ) {
					writer.WriteLine( "row = rows.SingleOrDefault();" );
					writer.WriteLine( "return row is not null;" );
				}
				else
					writer.WriteLine( "return rows.Single();" );
			}
			writer.WriteLine( "}" );
		}
	}

	private static void writeResultSetCreatorBody(
		DatabaseConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool hasModTable, bool tableUsesRowVersionedCaching,
		bool excludesPreviousRevisions, string cacheQueryInDbExpression ) {
		writer.WriteLine( "var results = new List<Row>();" );
		if( tableUsesRowVersionedCaching ) {
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );

			// Query for the cache keys of the results.
			writer.WriteLine(
				"var keyCommand = {0};".FormatWith(
					getInlineSelectExpression(
						table,
						tableColumns,
						"{0}, \"{1}\"".FormatWith(
							StringTools.ConcatenateWithDelimiter(
								", ",
								tableColumns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.DelimitedIdentifier.EscapeForLiteral() ) ).ToArray() ),
							cn.DatabaseInfo is OracleInfo ? "ORA_ROWSCN" : tableColumns.RowVersionColumn!.DelimitedIdentifier.EscapeForLiteral() ),
						cacheQueryInDbExpression ) ) );
			writer.WriteLine( getCommandConditionAddingStatement( "keyCommand" ) );
			writer.WriteLine( "var keys = new List<System.Tuple<{0}>>();".FormatWith( getPkAndVersionTupleTypeArguments( cn, tableColumns ) ) );
			writer.WriteLine(
				"keyCommand.Execute( " + DataAccessStatics.GetConnectionExpression( database ) + ", r => { while( r.Read() ) keys.Add( " +
				"System.Tuple.Create( {0}, {1} )".FormatWith(
					StringTools.ConcatenateWithDelimiter(
						", ",
						tableColumns.KeyColumns.Select( ( c, i ) => c.GetDataReaderValueExpression( "r", ordinalOverride: i ) ).ToArray() ),
					cn.DatabaseInfo is OracleInfo
						? "({0})r.GetValue( {1} )".FormatWith( oracleRowVersionDataType, tableColumns.KeyColumns.Count )
						: tableColumns.RowVersionColumn!.GetDataReaderValueExpression( "r", ordinalOverride: tableColumns.KeyColumns.Count ) ) + " ); } );" );

			writer.WriteLine( "var rowsByPkAndVersion = getRowsByPkAndVersion();" );
			writer.WriteLine( "var cachedKeyCount = keys.Where( i => rowsByPkAndVersion.ContainsKey( i ) ).Count();" );

			// If all but a few results are cached, execute a single-row query for each missing result.
			writer.WriteLine( "if( cachedKeyCount >= keys.Count() - 1 || cachedKeyCount >= keys.Count() * .99 ) {" );
			writer.WriteLine( "foreach( var key in keys ) {" );
			writer.WriteLine( "results.Add( new Row( rowsByPkAndVersion.GetOrAdd( key, () => {" );
			writer.WriteLine( "var singleRowCommand = {0};".FormatWith( getInlineSelectExpression( table, tableColumns, "\"*\"", "false" ) ) );
			writer.WriteLine(
				"singleRowCommand.AddConditions( new[] {{ {0} }} );".FormatWith(
					StringTools.ConcatenateWithDelimiter(
						", ",
						tableColumns.KeyColumns.Select(
							( column, index ) => "( ({0})new {1}( key.Item{2} ) ).CommandCondition".FormatWith(
								DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ),
								DataAccessStatics.GetEqualityConditionClassName( cn, database, table, column ),
								index + 1 ) ) ) ) );
			writer.WriteLine( "var singleRowResults = new List<BasicRow>();" );
			writer.WriteLine(
				"singleRowCommand.Execute( " + DataAccessStatics.GetConnectionExpression( database ) +
				", r => { while( r.Read() ) singleRowResults.Add( new BasicRow( r ) ); } );" );
			writer.WriteLine( "return singleRowResults.Single();" );
			writer.WriteLine( "} ) ) );" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );

			// Otherwise, execute the full query.
			writer.WriteLine( "else {" );
			writer.WriteLine(
				"var command = {0};".FormatWith(
					getInlineSelectExpression(
						table,
						tableColumns,
						cn.DatabaseInfo is OracleInfo ? "\"{0}.*\", \"ORA_ROWSCN\"".FormatWith( table ) : "\"*\"",
						cacheQueryInDbExpression ) ) );
			writer.WriteLine( getCommandConditionAddingStatement( "command" ) );
			writer.WriteLine( "command.Execute( " + DataAccessStatics.GetConnectionExpression( database ) + ", r => {" );
			writer.WriteLine(
				"while( r.Read() ) results.Add( new Row( rowsByPkAndVersion.GetOrAdd( System.Tuple.Create( {0}, {1} ), () => new BasicRow( r ) ) ) );".FormatWith(
					StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.GetDataReaderValueExpression( "r" ) ).ToArray() ),
					cn.DatabaseInfo is OracleInfo
						? "({0})r.GetValue( {1} )".FormatWith( oracleRowVersionDataType, tableColumns.AllColumns.Count )
						: tableColumns.RowVersionColumn!.GetDataReaderValueExpression( "r" ) ) );
			writer.WriteLine( "} );" );
			writer.WriteLine( "}" );

			writer.WriteLine( "} );" );
		}
		else {
			if( hasModTable ) {
				writer.WriteLine(
					"if( commandConditions.Count == {0} ) results.AddRange( {1}.ExecuteInTransaction( () => cache.{2}DataRetriever.Value.GetRows() ).Select( i => new Row( i ) ) );"
						.FormatWith(
							excludesPreviousRevisions ? "1" : "0",
							DataAccessStatics.GetConnectionExpression( database ),
							getTableCacheName( excludesPreviousRevisions ) ) );
				writer.WriteLine( "else {" );
			}

			writer.WriteLine( "var command = {0};".FormatWith( getInlineSelectExpression( table, tableColumns, "\"*\"", cacheQueryInDbExpression ) ) );
			writer.WriteLine( getCommandConditionAddingStatement( "command" ) );
			writer.WriteLine(
				"command.Execute( " + DataAccessStatics.GetConnectionExpression( database ) +
				", r => { while( r.Read() ) results.Add( new Row( new BasicRow( r ) ) ); } );" );

			if( hasModTable )
				writer.WriteLine( "}" );
		}

		if( tableColumns.HasKeyColumns ) {
			// Add all results to RowsByPk.
			writer.WriteLine( "foreach( var i in results ) {" );
			var pk = RetrievalStatics.GetColumnTupleExpression(
				tableColumns.KeyColumns.Select( i => "i." + EwlStatics.GetCSharpIdentifier( i.PascalCasedNameExceptForOracle ) ).Materialize() );
			writer.WriteLine( "cache.RowsByPk.TryAdd( " + pk + ", i );" );
			if( excludesPreviousRevisions )
				writer.WriteLine( "cache.LatestRevisionRowsByPk.TryAdd( " + pk + ", i );" );
			writer.WriteLine( "}" );
		}

		writer.WriteLine( "return results;" );
	}

	private static string getTableCacheName( bool excludePreviousRevisions ) => excludePreviousRevisions ? "LatestRevisionTableCache" : "TableCache";

	private static string getInlineSelectExpression( string table, TableColumns tableColumns, string selectExpressions, string cacheQueryInDbExpression ) =>
		"new InlineSelect( {0}, \"FROM {1}\", {2}, orderByClause: \"{3}\" )".FormatWith(
			"new[] { " + selectExpressions + " }",
			table,
			cacheQueryInDbExpression,
			tableColumns.HasKeyColumns
				? $"ORDER BY {StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.DelimitedIdentifier.EscapeForLiteral() ) )}"
				: "" );

	private static string getCommandConditionAddingStatement( string commandName ) => "{0}.AddConditions( commandConditions );".FormatWith( commandName );

	private static string getPkAndVersionTupleTypeArguments( DatabaseConnection cn, TableColumns tableColumns ) =>
		"{0}, {1}".FormatWith(
			getPkTupleTypeArguments( tableColumns ),
			cn.DatabaseInfo is OracleInfo ? oracleRowVersionDataType : tableColumns.RowVersionColumn!.DataTypeName );

	private static string getPkTupleTypeArguments( TableColumns tableColumns ) =>
		StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.DataTypeName ).ToArray() );

	private static void writeToIdDictionaryMethod( TextWriter writer, TableColumns tableColumns ) {
		writer.WriteLine( "public static Dictionary<" + tableColumns.KeyColumns.Single().DataTypeName + ", Row> ToIdDictionary( this IEnumerable<Row> rows ) {" );
		writer.WriteLine(
			"return rows.ToDictionary( i => i." + EwlStatics.GetCSharpIdentifier( tableColumns.KeyColumns.Single().PascalCasedNameExceptForOracle ) + " );" );
		writer.WriteLine( "}" );
	}

	internal static string GetClassName( DatabaseConnection cn, string table ) =>
		EwlStatics.GetCSharpIdentifier( "{0}TableRetrieval".FormatWith( table.TableNameToPascal( cn ) ) );
}