﻿using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems.StandardModification;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;

internal static class TableRetrievalStatics {
	private const string oracleRowVersionDataType = "decimal";

	internal static void Generate(
		DBConnection cn, TextWriter writer, string baseNamespace, string templateBasePath, Database database, IEnumerable<( string name, bool hasModTable )> tables,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		var subsystemName = "{0}TableRetrieval".FormatWith( database.SecondaryDatabaseName );
		var subsystemNamespace = "namespace {0}.{1}".FormatWith( baseNamespace, subsystemName );

		writer.WriteLine( "{0} {{".FormatWith( subsystemNamespace ) );
		foreach( var table in tables.Select( i => i.name ) ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "Contains logic that retrieves rows from the " + table + " table." );
			writer.WriteLine( "public static partial class " + GetClassName( cn, table ) + " {" );

			var isRevisionHistoryTable = DataAccessStatics.IsRevisionHistoryTable( table, configuration );
			var columns = new TableColumns( cn, table, isRevisionHistoryTable );

			// Write nested classes.
			DataAccessStatics.WriteRowClasses(
				writer,
				columns.AllColumns,
				_ => {
					if( !isRevisionHistoryTable )
						return;
					writer.WriteLine(
						"public UserTransaction Transaction { get { return RevisionHistoryStatics.UserTransactionsById[ RevisionHistoryStatics.RevisionsById[ System.Convert.ToInt32( " +
						EwlStatics.GetCSharpIdentifier( columns.PrimaryKeyAndRevisionIdColumn!.PascalCasedNameExceptForOracle ) + " ) ].UserTransactionId ]; } }" );
				},
				_ => {
					if( !columns.DataColumns.Any() )
						return;

					var modClass = database.SecondaryDatabaseName + "Modification." +
					               StandardModificationStatics.GetClassName( cn, table, isRevisionHistoryTable, isRevisionHistoryTable );
					var revisionHistorySuffix = StandardModificationStatics.GetRevisionHistorySuffix( isRevisionHistoryTable );
					writer.WriteLine( "public " + modClass + " ToModification" + revisionHistorySuffix + "() {" );
					writer.WriteLine(
						"return " + modClass + ".CreateForSingleRowUpdate" + revisionHistorySuffix + "( " + StringTools.ConcatenateWithDelimiter(
							", ",
							columns.AllColumnsExceptRowVersion.Select( i => EwlStatics.GetCSharpIdentifier( i.PascalCasedNameExceptForOracle ) ).ToArray() ) + " );" );
					writer.WriteLine( "}" );
				} );
			writeCacheClass( cn, writer, database, table, columns, isRevisionHistoryTable );

			var isSmallTable = configuration.SmallTables != null && configuration.SmallTables.Any( i => i.EqualsIgnoreCase( table ) );

			var tableUsesRowVersionedCaching = configuration.TablesUsingRowVersionedDataCaching != null &&
			                                   configuration.TablesUsingRowVersionedDataCaching.Any( i => i.EqualsIgnoreCase( table ) );
			if( tableUsesRowVersionedCaching && columns.RowVersionColumn == null && !( cn.DatabaseInfo is OracleInfo ) )
				throw new UserCorrectableException(
					cn.DatabaseInfo is MySqlInfo
						? "Row-versioned data caching cannot currently be used with MySQL databases."
						: "Row-versioned data caching can only be used with the {0} table if you add a rowversion column.".FormatWith( table ) );

			if( isSmallTable )
				writeGetAllRowsMethod( writer, isRevisionHistoryTable, false );
			writeGetRowsMethod( cn, writer, database, table, columns, isSmallTable, tableUsesRowVersionedCaching, isRevisionHistoryTable, false );
			if( isRevisionHistoryTable ) {
				if( isSmallTable )
					writeGetAllRowsMethod( writer, true, true );
				writeGetRowsMethod( cn, writer, database, table, columns, isSmallTable, tableUsesRowVersionedCaching, true, true );
			}

			writeGetRowMatchingPkMethod( cn, writer, database, table, columns, isSmallTable, tableUsesRowVersionedCaching, isRevisionHistoryTable );

			if( isRevisionHistoryTable )
				DataAccessStatics.WriteGetLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn!.Name );

			if( tableUsesRowVersionedCaching ) {
				var keyTupleTypeArguments = getPkAndVersionTupleTypeArguments( cn, columns );

				writer.WriteLine( "private static " + "Cache<System.Tuple<" + keyTupleTypeArguments + ">, BasicRow>" + " getRowsByPkAndVersion() {" );
				writer.WriteLine(
					"return AppMemoryCache.GetCacheValue<{0}>( \"{1}\", () => new {0}( i => System.Tuple.Create( {2} ) ) ).RowsByPkAndVersion;".FormatWith(
						"VersionedRowDataCache<System.Tuple<{0}>, System.Tuple<{1}>, BasicRow>".FormatWith( getPkTupleTypeArguments( columns ), keyTupleTypeArguments ),
						database.SecondaryDatabaseName + table.TableNameToPascal( cn ) + "TableRetrievalRowsByPkAndVersion",
						StringTools.ConcatenateWithDelimiter(
							", ",
							Enumerable.Range( 1, columns.KeyColumns.Count() ).Select( i => "i.Item{0}".FormatWith( i ) ).ToArray() ) ) );
				writer.WriteLine( "}" );
			}

			// Initially we did not generate this method for small tables, but we found a need for it when the cache is disabled since that will cause
			// GetRowMatchingId to repeatedly query.
			if( columns.KeyColumns.Count() == 1 && columns.KeyColumns.Single().Name.ToLower().EndsWith( "id" ) )
				writeToIdDictionaryMethod( writer, columns );

			if( isRevisionHistoryTable )
				DataAccessStatics.WriteRevisionDeltaExtensionMethods( writer, GetClassName( cn, table ), columns.DataColumns );

			writer.WriteLine( "}" ); // class

			var templateClassName = GetClassName( cn, table );
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

	internal static string GetClassName( DBConnection cn, string table ) {
		return EwlStatics.GetCSharpIdentifier( table.TableNameToPascal( cn ) + "TableRetrieval" );
	}

	private static void writeCacheClass(
		DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isRevisionHistoryTable ) {
		var cacheKey = database.SecondaryDatabaseName + table.TableNameToPascal( cn ) + "TableRetrieval";
		var pkTupleTypeArguments = getPkTupleTypeArguments( tableColumns );

		writer.WriteLine( "private partial class Cache {" );
		writer.WriteLine( "internal static Cache Current { get { return DataAccessState.Current.GetCacheValue( \"" + cacheKey + "\", () => new Cache() ); } }" );
		writer.WriteLine( "internal readonly TableRetrievalQueryCache<Row> Queries = new TableRetrievalQueryCache<Row>();" );
		writer.WriteLine(
			"internal readonly Cache<System.Tuple<{0}>, Row> RowsByPk = new Cache<System.Tuple<{0}>, Row>( false );".FormatWith( pkTupleTypeArguments ) );
		if( isRevisionHistoryTable )
			writer.WriteLine(
				"internal readonly Cache<System.Tuple<{0}>, Row> LatestRevisionRowsByPk = new Cache<System.Tuple<{0}>, Row>( false );".FormatWith(
					pkTupleTypeArguments ) );
		writer.WriteLine( "private Cache() {}" );
		writer.WriteLine( "}" );
	}

	private static void writeGetAllRowsMethod( TextWriter writer, bool isRevisionHistoryTable, bool excludePreviousRevisions ) {
		var revisionHistorySuffix = isRevisionHistoryTable && !excludePreviousRevisions ? "IncludingPreviousRevisions" : "";
		CodeGenerationStatics.AddSummaryDocComment( writer, "Retrieves the rows from the table, ordered in a stable way." );
		writer.WriteLine( "public static IEnumerable<Row> GetAllRows" + revisionHistorySuffix + "() {" );
		writer.WriteLine( "return GetRowsMatchingConditions" + revisionHistorySuffix + "();" );
		writer.WriteLine( "}" );
	}

	private static void writeGetRowsMethod(
		DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isSmallTable, bool tableUsesRowVersionedCaching,
		bool isRevisionHistoryTable, bool excludePreviousRevisions ) {
		// header
		var methodName = "GetRows" + ( isSmallTable ? "MatchingConditions" : "" ) +
		                 ( isRevisionHistoryTable && !excludePreviousRevisions ? "IncludingPreviousRevisions" : "" );
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Retrieves the rows from the table that match the specified conditions, ordered in a stable way." +
			( isSmallTable ? " Since the table is specified as small, you should only use this method if you cannot filter the rows in code." : "" ) );
		writer.WriteLine(
			"public static IEnumerable<Row> " + methodName + "( params " + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) +
			"[] conditions ) {" );


		// body

		// If it's a primary key query, use RowsByPk if possible.
		foreach( var i in tableColumns.KeyColumns ) {
			var equalityConditionClassName = DataAccessStatics.GetEqualityConditionClassName( cn, database, table, i );
			writer.WriteLine( "var {0}Condition = conditions.OfType<{1}>().FirstOrDefault();".FormatWith( i.CamelCasedName, equalityConditionClassName ) );
		}
		writer.WriteLine( "var cache = Cache.Current;" );
		var pkConditionVariableNames = tableColumns.KeyColumns.Select( i => i.CamelCasedName + "Condition" );
		writer.WriteLine(
			"var isPkQuery = " + StringTools.ConcatenateWithDelimiter( " && ", pkConditionVariableNames.Select( i => i + " != null" ).ToArray() ) +
			" && conditions.Count() == " + tableColumns.KeyColumns.Count() + ";" );
		writer.WriteLine( "if( isPkQuery ) {" );
		writer.WriteLine( "Row row;" );
		writer.WriteLine(
			"if( cache." + ( excludePreviousRevisions ? "LatestRevision" : "" ) + "RowsByPk.TryGetValue( System.Tuple.Create( " +
			StringTools.ConcatenateWithDelimiter( ", ", pkConditionVariableNames.Select( i => i + ".Value" ).ToArray() ) + " ), out row ) )" );
		writer.WriteLine( "return row.ToCollection();" );
		writer.WriteLine( "}" );

		var commandConditionsExpression = "conditions.Select( i => i.CommandCondition )";
		if( excludePreviousRevisions )
			commandConditionsExpression += ".Concat( getLatestRevisionsCondition().ToCollection() )";
		writer.WriteLine( "return cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
		writeResultSetCreatorBody( cn, writer, database, table, tableColumns, tableUsesRowVersionedCaching, excludePreviousRevisions, "!isPkQuery" );
		writer.WriteLine( "} );" );

		writer.WriteLine( "}" );
	}

	private static void writeGetRowMatchingPkMethod(
		DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isSmallTable, bool tableUsesRowVersionedCaching,
		bool isRevisionHistoryTable ) {
		var pkIsId = tableColumns.KeyColumns.Count() == 1 && tableColumns.KeyColumns.Single().Name.ToLower().EndsWith( "id" );
		var methodName = pkIsId ? "GetRowMatchingId" : "GetRowMatchingPk";
		var pkParameters = pkIsId
			                   ? "{0} id".FormatWith( tableColumns.KeyColumns.Single().DataTypeName )
			                   : StringTools.ConcatenateWithDelimiter(
				                   ", ",
				                   tableColumns.KeyColumns.Select( i => "{0} {1}".FormatWith( i.DataTypeName, i.CamelCasedName ) ).ToArray() );
		writer.WriteLine( "public static Row " + methodName + "( " + pkParameters + ", bool returnNullIfNoMatch = false ) {" );
		if( isSmallTable ) {
			writer.WriteLine( "var cache = Cache.Current;" );
			var commandConditionsExpression = isRevisionHistoryTable ? "getLatestRevisionsCondition().ToCollection()" : "new InlineDbCommandCondition[ 0 ]";
			writer.WriteLine( "cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
			writeResultSetCreatorBody( cn, writer, database, table, tableColumns, tableUsesRowVersionedCaching, isRevisionHistoryTable, "true" );
			writer.WriteLine( "} );" );

			var rowsByPkExpression = "cache.{0}RowsByPk".FormatWith( isRevisionHistoryTable ? "LatestRevision" : "" );
			var pkTupleCreationArguments =
				pkIsId ? "id" : StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.CamelCasedName ).ToArray() );
			writer.WriteLine( "if( !returnNullIfNoMatch )" );
			writer.WriteLine( "return {0}[ System.Tuple.Create( {1} ) ];".FormatWith( rowsByPkExpression, pkTupleCreationArguments ) );
			writer.WriteLine( "Row row;" );
			writer.WriteLine(
				"return {0}.TryGetValue( System.Tuple.Create( {1} ), out row ) ? row : null;".FormatWith( rowsByPkExpression, pkTupleCreationArguments ) );
		}
		else {
			writer.WriteLine(
				"var rows = GetRows( {0} );".FormatWith(
					pkIsId
						? "new {0}( id )".FormatWith( DataAccessStatics.GetEqualityConditionClassName( cn, database, table, tableColumns.KeyColumns.Single() ) )
						: StringTools.ConcatenateWithDelimiter(
							", ",
							tableColumns.KeyColumns.Select(
									i => "new {0}( {1} )".FormatWith( DataAccessStatics.GetEqualityConditionClassName( cn, database, table, i ), i.CamelCasedName ) )
								.ToArray() ) ) );
			writer.WriteLine( "return returnNullIfNoMatch ? rows.SingleOrDefault() : rows.Single();" );
		}
		writer.WriteLine( "}" );
	}

	private static void writeResultSetCreatorBody(
		DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool tableUsesRowVersionedCaching,
		bool excludesPreviousRevisions, string cacheQueryInDbExpression ) {
		if( tableUsesRowVersionedCaching ) {
			writer.WriteLine( "var results = new List<Row>();" );
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( delegate {" );

			// Query for the cache keys of the results.
			writer.WriteLine(
				"var keyCommand = {0};".FormatWith(
					getInlineSelectExpression(
						table,
						tableColumns,
						"{0}, \"{1}\"".FormatWith(
							StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ).ToArray() ),
							cn.DatabaseInfo is OracleInfo ? "ORA_ROWSCN" : tableColumns.RowVersionColumn!.Name ),
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
						? "({0})r.GetValue( {1} )".FormatWith( oracleRowVersionDataType, tableColumns.KeyColumns.Count() )
						: tableColumns.RowVersionColumn!.GetDataReaderValueExpression( "r", ordinalOverride: tableColumns.KeyColumns.Count() ) ) + " ); } );" );

			writer.WriteLine( "var rowsByPkAndVersion = getRowsByPkAndVersion();" );
			writer.WriteLine( "var cachedKeyCount = keys.Where( i => rowsByPkAndVersion.ContainsKey( i ) ).Count();" );

			// If all but a few results are cached, execute a single-row query for each missing result.
			writer.WriteLine( "if( cachedKeyCount >= keys.Count() - 1 || cachedKeyCount >= keys.Count() * .99 ) {" );
			writer.WriteLine( "foreach( var key in keys ) {" );
			writer.WriteLine( "results.Add( new Row( rowsByPkAndVersion.GetOrAdd( key, () => {" );
			writer.WriteLine( "var singleRowCommand = {0};".FormatWith( getInlineSelectExpression( table, tableColumns, "\"*\"", "false" ) ) );
			foreach( var i in tableColumns.KeyColumns.Select( ( c, i ) => new { column = c, index = i } ) )
				writer.WriteLine(
					"singleRowCommand.AddCondition( ( ({0})new {1}( key.Item{2} ) ).CommandCondition );".FormatWith(
						DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ),
						DataAccessStatics.GetEqualityConditionClassName( cn, database, table, i.column ),
						i.index + 1 ) );
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
						? "({0})r.GetValue( {1} )".FormatWith( oracleRowVersionDataType, tableColumns.AllColumns.Count() )
						: tableColumns.RowVersionColumn!.GetDataReaderValueExpression( "r" ) ) );
			writer.WriteLine( "} );" );
			writer.WriteLine( "}" );

			writer.WriteLine( "} );" );
		}
		else {
			writer.WriteLine( "var command = {0};".FormatWith( getInlineSelectExpression( table, tableColumns, "\"*\"", cacheQueryInDbExpression ) ) );
			writer.WriteLine( getCommandConditionAddingStatement( "command" ) );
			writer.WriteLine( "var results = new List<Row>();" );
			writer.WriteLine(
				"command.Execute( " + DataAccessStatics.GetConnectionExpression( database ) +
				", r => { while( r.Read() ) results.Add( new Row( new BasicRow( r ) ) ); } );" );
		}

		// Add all results to RowsByPk.
		writer.WriteLine( "foreach( var i in results ) {" );
		var pkTupleCreationArgs = tableColumns.KeyColumns.Select( i => "i." + EwlStatics.GetCSharpIdentifier( i.PascalCasedNameExceptForOracle ) );
		var pkTuple = "System.Tuple.Create( " + StringTools.ConcatenateWithDelimiter( ", ", pkTupleCreationArgs.ToArray() ) + " )";
		writer.WriteLine( "cache.RowsByPk.TryAdd( " + pkTuple + ", i );" );
		if( excludesPreviousRevisions )
			writer.WriteLine( "cache.LatestRevisionRowsByPk.TryAdd( " + pkTuple + ", i );" );
		writer.WriteLine( "}" );

		writer.WriteLine( "return results;" );
	}

	private static string getInlineSelectExpression( string table, TableColumns tableColumns, string selectExpressions, string cacheQueryInDbExpression ) {
		return "new InlineSelect( {0}, \"FROM {1}\", {2}, orderByClause: \"ORDER BY {3}\" )".FormatWith(
			"new[] { " + selectExpressions + " }",
			table,
			cacheQueryInDbExpression,
			StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.Name ).ToArray() ) );
	}

	private static string getCommandConditionAddingStatement( string commandName ) {
		return "foreach( var i in commandConditions ) {0}.AddCondition( i );".FormatWith( commandName );
	}

	private static string getPkAndVersionTupleTypeArguments( DBConnection cn, TableColumns tableColumns ) {
		return "{0}, {1}".FormatWith(
			getPkTupleTypeArguments( tableColumns ),
			cn.DatabaseInfo is OracleInfo ? oracleRowVersionDataType : tableColumns.RowVersionColumn!.DataTypeName );
	}

	private static string getPkTupleTypeArguments( TableColumns tableColumns ) {
		return StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.DataTypeName ).ToArray() );
	}

	private static void writeToIdDictionaryMethod( TextWriter writer, TableColumns tableColumns ) {
		writer.WriteLine( "public static Dictionary<" + tableColumns.KeyColumns.Single().DataTypeName + ", Row> ToIdDictionary( this IEnumerable<Row> rows ) {" );
		writer.WriteLine(
			"return rows.ToDictionary( i => i." + EwlStatics.GetCSharpIdentifier( tableColumns.KeyColumns.Single().PascalCasedNameExceptForOracle ) + " );" );
		writer.WriteLine( "}" );
	}
}