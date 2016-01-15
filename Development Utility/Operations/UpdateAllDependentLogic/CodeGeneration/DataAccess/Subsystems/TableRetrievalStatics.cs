using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems.StandardModification;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.IO;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class TableRetrievalStatics {
		private const string oracleRowVersionDataType = "decimal";

		public static string GetNamespaceDeclaration( string baseNamespace, Database database ) {
			return "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "TableRetrieval {";
		}

		internal static void Generate(
			DBConnection cn, TextWriter writer, string namespaceDeclaration, Database database, Configuration.SystemDevelopment.Database configuration ) {
			writer.WriteLine( namespaceDeclaration );
			foreach( var table in DatabaseOps.GetDatabaseTables( database ) ) {
				CodeGenerationStatics.AddSummaryDocComment( writer, "Contains logic that retrieves rows from the " + table + " table." );
				writer.WriteLine( "public static partial class " + GetClassName( cn, table ) + " {" );

				var isRevisionHistoryTable = DataAccessStatics.IsRevisionHistoryTable( table, configuration );
				var columns = new TableColumns( cn, table, isRevisionHistoryTable );

				// Write nested classes.
				DataAccessStatics.WriteRowClasses(
					writer,
					columns.AllColumns,
					localWriter => {
						if( !isRevisionHistoryTable )
							return;
						writer.WriteLine(
							"public UserTransaction Transaction { get { return RevisionHistoryStatics.UserTransactionsById[ RevisionHistoryStatics.RevisionsById[ " +
							EwlStatics.GetCSharpIdentifierSimple( columns.PrimaryKeyAndRevisionIdColumn.PascalCasedNameExceptForOracle ) + " ].UserTransactionId ]; } }" );
					},
					localWriter => {
						if( !columns.DataColumns.Any() )
							return;

						var modClass = database.SecondaryDatabaseName + "Modification." +
						               StandardModificationStatics.GetClassName( cn, table, isRevisionHistoryTable, isRevisionHistoryTable );
						var revisionHistorySuffix = StandardModificationStatics.GetRevisionHistorySuffix( isRevisionHistoryTable );
						writer.WriteLine( "public " + modClass + " ToModification" + revisionHistorySuffix + "() {" );
						writer.WriteLine(
							"return " + modClass + ".CreateForSingleRowUpdate" + revisionHistorySuffix + "( " +
							StringTools.ConcatenateWithDelimiter(
								", ",
								columns.AllColumnsExceptRowVersion.Select( i => EwlStatics.GetCSharpIdentifierSimple( i.PascalCasedNameExceptForOracle ) ).ToArray() ) + " );" );
						writer.WriteLine( "}" );
					} );
				writeCacheClass( cn, writer, database, table, columns, isRevisionHistoryTable );

				var isSmallTable = configuration.SmallTables != null && configuration.SmallTables.Any( i => i.EqualsIgnoreCase( table ) );

				var tableUsesRowVersionedCaching = configuration.TablesUsingRowVersionedDataCaching != null &&
				                                   configuration.TablesUsingRowVersionedDataCaching.Any( i => i.EqualsIgnoreCase( table ) );
				if( tableUsesRowVersionedCaching && columns.RowVersionColumn == null && !( cn.DatabaseInfo is OracleInfo ) ) {
					throw new UserCorrectableException(
						cn.DatabaseInfo is MySqlInfo
							? "Row-versioned data caching cannot currently be used with MySQL databases."
							: "Row-versioned data caching can only be used with the {0} table if you add a rowversion column.".FormatWith( table ) );
				}

				if( isSmallTable )
					writeGetAllRowsMethod( writer, isRevisionHistoryTable, false );
				writeGetRowsMethod( cn, writer, database, table, columns, isSmallTable, tableUsesRowVersionedCaching, isRevisionHistoryTable, false );
				if( isRevisionHistoryTable ) {
					if( isSmallTable )
						writeGetAllRowsMethod( writer, true, true );
					writeGetRowsMethod( cn, writer, database, table, columns, isSmallTable, tableUsesRowVersionedCaching, true, true );
				}

				if( columns.KeyColumns.Count() == 1 && columns.KeyColumns.Single().Name.ToLower().EndsWith( "id" ) )
					writeGetRowMatchingIdMethod( cn, writer, database, table, columns, isSmallTable, tableUsesRowVersionedCaching, isRevisionHistoryTable );

				if( isRevisionHistoryTable )
					DataAccessStatics.WriteGetLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn.Name );

				if( tableUsesRowVersionedCaching ) {
					var keyTupleTypeArguments = getPkAndVersionTupleTypeArguments( cn, columns );

					writer.WriteLine( "private static " + "Cache<System.Tuple<" + keyTupleTypeArguments + ">, BasicRow>" + " getRowsByPkAndVersion() {" );
					writer.WriteLine(
						"return AppMemoryCache.GetCacheValue<{0}>( \"{1}\", () => new {0}( i => System.Tuple.Create( {2} ) ) ).RowsByPkAndVersion;".FormatWith(
							"VersionedRowDataCache<System.Tuple<{0}>, System.Tuple<{1}>, BasicRow>".FormatWith( getPkTupleTypeArguments( columns ), keyTupleTypeArguments ),
							database.SecondaryDatabaseName + table.TableNameToPascal( cn ) + "TableRetrievalRowsByPkAndVersion",
							StringTools.ConcatenateWithDelimiter( ", ", Enumerable.Range( 1, columns.KeyColumns.Count() ).Select( i => "i.Item{0}".FormatWith( i ) ).ToArray() ) ) );
					writer.WriteLine( "}" );
				}

				// Initially we did not generate this method for small tables, but we found a need for it when the cache is disabled since that will cause
				// GetRowMatchingId to repeatedly query.
				if( columns.KeyColumns.Count() == 1 && columns.KeyColumns.Single().Name.ToLower().EndsWith( "id" ) )
					writeToIdDictionaryMethod( writer, columns );

				writer.WriteLine( "}" ); // class
			}
			writer.WriteLine( "}" ); // namespace
		}

		internal static void WritePartialClass( DBConnection cn, string libraryBasePath, string namespaceDeclaration, Database database, string tableName ) {
			var folderPath = EwlStatics.CombinePaths( libraryBasePath, "DataAccess", database.SecondaryDatabaseName + "TableRetrieval" );
			var templateFilePath = EwlStatics.CombinePaths( folderPath, GetClassName( cn, tableName ) + DataAccessStatics.CSharpTemplateFileExtension );
			IoMethods.DeleteFile( templateFilePath );

			// If a real file exists, don't create a template.
			if( File.Exists( EwlStatics.CombinePaths( folderPath, GetClassName( cn, tableName ) + ".cs" ) ) )
				return;

			using( var writer = IoMethods.GetTextWriterForWrite( templateFilePath ) ) {
				writer.WriteLine( namespaceDeclaration );
				writer.WriteLine( "	partial class " + GetClassName( cn, tableName ) + " {" );
				writer.WriteLine(
					"		// IMPORTANT: Change extension from \"{0}\" to \".cs\" before including in project and editing.".FormatWith(
						DataAccessStatics.CSharpTemplateFileExtension ) );
				writer.WriteLine( "	}" ); // class
				writer.WriteLine( "}" ); // namespace
			}
		}

		internal static string GetClassName( DBConnection cn, string table ) {
			return EwlStatics.GetCSharpSafeClassName( table.TableNameToPascal( cn ) + "TableRetrieval" );
		}

		private static void writeCacheClass(
			DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isRevisionHistoryTable ) {
			var cacheKey = database.SecondaryDatabaseName + table.TableNameToPascal( cn ) + "TableRetrieval";
			var pkTupleTypeArguments = getPkTupleTypeArguments( tableColumns );

			writer.WriteLine( "private partial class Cache {" );
			writer.WriteLine( "internal static Cache Current { get { return DataAccessState.Current.GetCacheValue( \"" + cacheKey + "\", () => new Cache() ); } }" );
			writer.WriteLine( "private readonly TableRetrievalQueryCache<Row> queries = new TableRetrievalQueryCache<Row>();" );
			writer.WriteLine(
				"private readonly Dictionary<System.Tuple<{0}>, Row> rowsByPk = new Dictionary<System.Tuple<{0}>, Row>();".FormatWith( pkTupleTypeArguments ) );
			if( isRevisionHistoryTable ) {
				writer.WriteLine(
					"private readonly Dictionary<System.Tuple<{0}>, Row> latestRevisionRowsByPk = new Dictionary<System.Tuple<{0}>, Row>();".FormatWith( pkTupleTypeArguments ) );
			}
			writer.WriteLine( "private Cache() {}" );
			writer.WriteLine( "internal TableRetrievalQueryCache<Row> Queries { get { return queries; } }" );
			writer.WriteLine( "internal Dictionary<System.Tuple<" + pkTupleTypeArguments + ">, Row> RowsByPk { get { return rowsByPk; } }" );
			if( isRevisionHistoryTable )
				writer.WriteLine( "internal Dictionary<System.Tuple<" + pkTupleTypeArguments + ">, Row> LatestRevisionRowsByPk { get { return latestRevisionRowsByPk; } }" );
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
				"public static IEnumerable<Row> " + methodName + "( params " + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + "[] conditions ) {" );


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
			writer.WriteLine( "return row.ToSingleElementArray();" );
			writer.WriteLine( "}" );

			var commandConditionsExpression = "conditions.Select( i => i.CommandCondition )";
			if( excludePreviousRevisions )
				commandConditionsExpression += ".Concat( getLatestRevisionsCondition().ToSingleElementArray() )";
			writer.WriteLine( "return cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
			writeResultSetCreatorBody( cn, writer, database, table, tableColumns, tableUsesRowVersionedCaching, excludePreviousRevisions, "!isPkQuery" );
			writer.WriteLine( "} );" );

			writer.WriteLine( "}" );
		}

		private static void writeGetRowMatchingIdMethod(
			DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isSmallTable, bool tableUsesRowVersionedCaching,
			bool isRevisionHistoryTable ) {
			writer.WriteLine( "public static Row GetRowMatchingId( " + tableColumns.KeyColumns.Single().DataTypeName + " id, bool returnNullIfNoMatch = false ) {" );
			if( isSmallTable ) {
				writer.WriteLine( "var cache = Cache.Current;" );
				var commandConditionsExpression = isRevisionHistoryTable ? "getLatestRevisionsCondition().ToSingleElementArray()" : "new InlineDbCommandCondition[ 0 ]";
				writer.WriteLine( "cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
				writeResultSetCreatorBody( cn, writer, database, table, tableColumns, tableUsesRowVersionedCaching, isRevisionHistoryTable, "true" );
				writer.WriteLine( "} );" );

				var rowsByPkExpression = "cache.{0}RowsByPk".FormatWith( isRevisionHistoryTable ? "LatestRevision" : "" );
				writer.WriteLine( "if( !returnNullIfNoMatch )" );
				writer.WriteLine( "return {0}[ System.Tuple.Create( id ) ];".FormatWith( rowsByPkExpression ) );
				writer.WriteLine( "Row row;" );
				writer.WriteLine( "return {0}.TryGetValue( System.Tuple.Create( id ), out row ) ? row : null;".FormatWith( rowsByPkExpression ) );
			}
			else {
				writer.WriteLine(
					"var rows = GetRows( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, table, tableColumns.KeyColumns.Single() ) + "( id ) );" );
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
								cn.DatabaseInfo is OracleInfo ? "ORA_ROWSCN" : tableColumns.RowVersionColumn.Name ),
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
							: tableColumns.RowVersionColumn.GetDataReaderValueExpression( "r", ordinalOverride: tableColumns.KeyColumns.Count() ) ) + " ); } );" );

				writer.WriteLine( "var rowsByPkAndVersion = getRowsByPkAndVersion();" );
				writer.WriteLine( "var cachedKeyCount = keys.Where( i => rowsByPkAndVersion.ContainsKey( i ) ).Count();" );

				// If all but a few results are cached, execute a single-row query for each missing result.
				writer.WriteLine( "if( cachedKeyCount >= keys.Count() - 1 || cachedKeyCount >= keys.Count() * .99 ) {" );
				writer.WriteLine( "foreach( var key in keys ) {" );
				writer.WriteLine( "results.Add( new Row( rowsByPkAndVersion.GetOrAdd( key, () => {" );
				writer.WriteLine( "var singleRowCommand = {0};".FormatWith( getInlineSelectExpression( table, tableColumns, "\"*\"", "false" ) ) );
				foreach( var i in tableColumns.KeyColumns.Select( ( c, i ) => new { column = c, index = i } ) ) {
					writer.WriteLine(
						"singleRowCommand.AddCondition( ( ({0})new {1}( key.Item{2} ) ).CommandCondition );".FormatWith(
							DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ),
							DataAccessStatics.GetEqualityConditionClassName( cn, database, table, i.column ),
							i.index + 1 ) );
				}
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
							: tableColumns.RowVersionColumn.GetDataReaderValueExpression( "r" ) ) );
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
			var pkTupleCreationArgs = tableColumns.KeyColumns.Select( i => "i." + EwlStatics.GetCSharpIdentifierSimple( i.PascalCasedNameExceptForOracle ) );
			var pkTuple = "System.Tuple.Create( " + StringTools.ConcatenateWithDelimiter( ", ", pkTupleCreationArgs.ToArray() ) + " )";
			writer.WriteLine( "cache.RowsByPk[ " + pkTuple + " ] = i;" );
			if( excludesPreviousRevisions )
				writer.WriteLine( "cache.LatestRevisionRowsByPk[ " + pkTuple + " ] = i;" );
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
				cn.DatabaseInfo is OracleInfo ? oracleRowVersionDataType : tableColumns.RowVersionColumn.DataTypeName );
		}

		private static string getPkTupleTypeArguments( TableColumns tableColumns ) {
			return StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.DataTypeName ).ToArray() );
		}

		private static void writeToIdDictionaryMethod( TextWriter writer, TableColumns tableColumns ) {
			writer.WriteLine( "public static Dictionary<" + tableColumns.KeyColumns.Single().DataTypeName + ", Row> ToIdDictionary( this IEnumerable<Row> rows ) {" );
			writer.WriteLine(
				"return rows.ToDictionary( i => i." + EwlStatics.GetCSharpIdentifierSimple( tableColumns.KeyColumns.Single().PascalCasedNameExceptForOracle ) + " );" );
			writer.WriteLine( "}" );
		}
	}
}