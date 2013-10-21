using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems.StandardModification;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;
using RedStapler.StandardLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class TableRetrievalStatics {
		public static string GetNamespaceDeclaration( string baseNamespace, Database database ) {
			return "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "TableRetrieval {";
		}

		internal static void Generate( DBConnection cn, TextWriter writer, string namespaceDeclaration, Database database,
		                               RedStapler.StandardLibrary.Configuration.SystemDevelopment.Database configuration ) {
			writer.WriteLine( namespaceDeclaration );
			foreach( var table in database.GetTables() ) {
				CodeGenerationStatics.AddSummaryDocComment( writer, "Contains logic that retrieves rows from the " + table + " table." );
				writer.WriteLine( "public static partial class " + GetClassName( cn, table ) + " {" );

				var isRevisionHistoryTable = DataAccessStatics.IsRevisionHistoryTable( table, configuration );
				var columns = new TableColumns( cn, table, isRevisionHistoryTable );

				// Write nested classes.
				DataAccessStatics.WriteRowClass( writer,
				                                 columns.AllColumns,
				                                 localWriter => {
					                                 if( !columns.DataColumns.Any() )
						                                 return;

					                                 var modClass = database.SecondaryDatabaseName + "Modification." +
					                                                StandardModificationStatics.GetClassName( cn, table, isRevisionHistoryTable, isRevisionHistoryTable );
					                                 var revisionHistorySuffix = StandardModificationStatics.GetRevisionHistorySuffix( isRevisionHistoryTable );
					                                 writer.WriteLine( "public " + modClass + " ToModification" + revisionHistorySuffix + "() {" );
					                                 writer.WriteLine( "return " + modClass + ".CreateForSingleRowUpdate" + revisionHistorySuffix + "( " +
					                                                   StringTools.ConcatenateWithDelimiter( ", ",
					                                                                                         columns.AllColumns.Select(
						                                                                                         i =>
						                                                                                         StandardLibraryMethods.GetCSharpIdentifierSimple(
							                                                                                         i.PascalCasedNameExceptForOracle ) ).ToArray() ) + " );" );
					                                 writer.WriteLine( "}" );
				                                 },
				                                 cn.DatabaseInfo );
				var isSmallTable = configuration.SmallTables != null && configuration.SmallTables.Any( i => i.EqualsIgnoreCase( table ) );
				writeCacheClass( cn, writer, database, table, columns, isRevisionHistoryTable );

				if( isSmallTable )
					writeGetAllRowsMethod( writer, isRevisionHistoryTable, false );
				writeGetRowsMethod( cn, writer, database, table, columns, isSmallTable, isRevisionHistoryTable, false );
				if( isRevisionHistoryTable ) {
					if( isSmallTable )
						writeGetAllRowsMethod( writer, true, true );
					writeGetRowsMethod( cn, writer, database, table, columns, isSmallTable, true, true );
				}

				if( columns.KeyColumns.Count() == 1 && columns.KeyColumns.Single().Name.ToLower().EndsWith( "id" ) )
					writeGetRowMatchingIdMethod( cn, writer, database, table, columns, isSmallTable, isRevisionHistoryTable );

				if( isRevisionHistoryTable )
					DataAccessStatics.WriteGetLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn.Name );

				// Initially we did not generate this method for small tables, but we found a need for it when the cache is disabled since that will cause
				// GetRowMatchingId to repeatedly query.
				if( columns.KeyColumns.Count() == 1 && columns.KeyColumns.Single().Name.ToLower().EndsWith( "id" ) )
					writeToIdDictionaryMethod( writer, columns );

				writer.WriteLine( "}" ); // class
			}
			writer.WriteLine( "}" ); // namespace
		}

		internal static void WritePartialClass( DBConnection cn, string libraryBasePath, string namespaceDeclaration, Database database, string tableName ) {
			var folderPath = StandardLibraryMethods.CombinePaths( libraryBasePath, "DataAccess", database.SecondaryDatabaseName + "TableRetrieval" );
			var templateFilePath = StandardLibraryMethods.CombinePaths( folderPath, GetClassName( cn, tableName ) + DataAccessStatics.CSharpTemplateFileExtension );
			IoMethods.DeleteFile( templateFilePath );

			// If a real file exists, don't create a template.
			if( File.Exists( StandardLibraryMethods.CombinePaths( folderPath, GetClassName( cn, tableName ) + ".cs" ) ) )
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
			return StandardLibraryMethods.GetCSharpSafeClassName( table.TableNameToPascal( cn ) + "TableRetrieval" );
		}

		private static void writeCacheClass( DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns,
		                                     bool isRevisionHistoryTable ) {
			var cacheKey = database.SecondaryDatabaseName + table.TableNameToPascal( cn ) + "TableRetrieval";
			var pkTupleTypeArguments = StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( i => i.DataTypeName ).ToArray() );

			writer.WriteLine( "private class Cache {" );
			writer.WriteLine( "internal static Cache Current { get { return DataAccessState.Current.GetCacheValue( \"" + cacheKey + "\", () => new Cache() ); } }" );
			writer.WriteLine( "private readonly TableRetrievalQueryCache<Row> queries = new TableRetrievalQueryCache<Row>();" );
			writer.WriteLine( "private readonly Dictionary<System.Tuple<{0}>, Row> rowsByPk = new Dictionary<System.Tuple<{0}>, Row>();".FormatWith( pkTupleTypeArguments ) );
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

		private static void writeGetRowsMethod( DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns, bool isSmallTable,
		                                        bool isRevisionHistoryTable, bool excludePreviousRevisions ) {
			// header
			var methodName = "GetRows" + ( isSmallTable ? "MatchingConditions" : "" ) +
			                 ( isRevisionHistoryTable && !excludePreviousRevisions ? "IncludingPreviousRevisions" : "" );
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Retrieves the rows from the table that match the specified conditions, ordered in a stable way." +
			                                            ( isSmallTable
				                                              ? " Since the table is specified as small, you should only use this method if you cannot filter the rows in code."
				                                              : "" ) );
			writer.WriteLine( "public static IEnumerable<Row> " + methodName + "( params " + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) +
			                  "[] conditions ) {" );


			// body

			// If it's a primary key query, use RowsByPk if possible.
			foreach( var i in tableColumns.KeyColumns ) {
				var equalityConditionClassName = DataAccessStatics.GetEqualityConditionClassName( cn, database, table, i );
				writer.WriteLine( "var {0}Condition = conditions.OfType<{1}>().FirstOrDefault();".FormatWith( i.CamelCasedName, equalityConditionClassName ) );
			}
			writer.WriteLine( "var cache = Cache.Current;" );
			var pkConditionVariableNames = tableColumns.KeyColumns.Select( i => i.CamelCasedName + "Condition" );
			writer.WriteLine( "if( " + StringTools.ConcatenateWithDelimiter( " && ", pkConditionVariableNames.Select( i => i + " != null" ).ToArray() ) +
			                  " && conditions.Count() == " + tableColumns.KeyColumns.Count() + " ) {" );
			writer.WriteLine( "Row row;" );
			writer.WriteLine( "if( cache." + ( excludePreviousRevisions ? "LatestRevision" : "" ) + "RowsByPk.TryGetValue( System.Tuple.Create( " +
			                  StringTools.ConcatenateWithDelimiter( ", ", pkConditionVariableNames.Select( i => i + ".Value" ).ToArray() ) + " ), out row ) )" );
			writer.WriteLine( "return row.ToSingleElementArray();" );
			writer.WriteLine( "}" );

			var commandConditionsExpression = "conditions.Select( i => i.CommandCondition )";
			if( excludePreviousRevisions )
				commandConditionsExpression += ".Concat( getLatestRevisionsCondition().ToSingleElementArray() )";
			writer.WriteLine( "return cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
			writeResultSetCreatorBody( writer, database, table, tableColumns, excludePreviousRevisions );
			writer.WriteLine( "} );" );

			writer.WriteLine( "}" );
		}

		private static void writeGetRowMatchingIdMethod( DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns,
		                                                 bool isSmallTable, bool isRevisionHistoryTable ) {
			writer.WriteLine( "public static Row GetRowMatchingId( " + tableColumns.KeyColumns.Single().DataTypeName + " id ) {" );
			if( isSmallTable ) {
				writer.WriteLine( "var cache = Cache.Current;" );
				var commandConditionsExpression = isRevisionHistoryTable ? "getLatestRevisionsCondition().ToSingleElementArray()" : "new InlineDbCommandCondition[ 0 ]";
				writer.WriteLine( "cache.Queries.GetResultSet( " + commandConditionsExpression + ", commandConditions => {" );
				writeResultSetCreatorBody( writer, database, table, tableColumns, isRevisionHistoryTable );
				writer.WriteLine( "} );" );
				writer.WriteLine( "return cache." + ( isRevisionHistoryTable ? "LatestRevision" : "" ) + "RowsByPk[ System.Tuple.Create( id ) ];" );
			}
			else {
				writer.WriteLine( "return GetRows( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, table, tableColumns.KeyColumns.Single() ) +
				                  "( id ) ).Single();" );
			}
			writer.WriteLine( "}" );
		}

		private static void writeResultSetCreatorBody( TextWriter writer, Database database, string table, TableColumns tableColumns, bool excludesPreviousRevisions ) {
			writer.WriteLine( "var command = new InlineSelect( \"SELECT * FROM " + table + "\", \"ORDER BY " +
			                  StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( c => c.Name ).ToArray() ) + "\" );" );
			writer.WriteLine( "foreach( var i in commandConditions )" );
			writer.WriteLine( "command.AddCondition( i );" );
			writer.WriteLine( "var results = new List<Row>();" );
			writer.WriteLine( "command.Execute( " + DataAccessStatics.GetConnectionExpression( database ) +
			                  ", r => { while( r.Read() ) results.Add( new Row( r ) ); } );" );

			// Add all results to RowsByPk.
			writer.WriteLine( "foreach( var i in results ) {" );
			var pkTupleCreationArgs = tableColumns.KeyColumns.Select( i => "i." + StandardLibraryMethods.GetCSharpIdentifierSimple( i.PascalCasedNameExceptForOracle ) );
			var pkTuple = "System.Tuple.Create( " + StringTools.ConcatenateWithDelimiter( ", ", pkTupleCreationArgs.ToArray() ) + " )";
			writer.WriteLine( "cache.RowsByPk[ " + pkTuple + " ] = i;" );
			if( excludesPreviousRevisions )
				writer.WriteLine( "cache.LatestRevisionRowsByPk[ " + pkTuple + " ] = i;" );
			writer.WriteLine( "}" );

			writer.WriteLine( "return results;" );
		}

		private static void writeToIdDictionaryMethod( TextWriter writer, TableColumns tableColumns ) {
			writer.WriteLine( "public static Dictionary<" + tableColumns.KeyColumns.Single().DataTypeName + ", Row> ToIdDictionary( this IEnumerable<Row> rows ) {" );
			writer.WriteLine( "return rows.ToDictionary( i => i." +
			                  StandardLibraryMethods.GetCSharpIdentifierSimple( tableColumns.KeyColumns.Single().PascalCasedNameExceptForOracle ) + " );" );
			writer.WriteLine( "}" );
		}
	}
}