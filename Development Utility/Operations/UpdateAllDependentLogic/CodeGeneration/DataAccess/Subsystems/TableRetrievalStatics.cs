using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;

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
				DataAccessStatics.WriteRowClass( writer, columns.AllColumns, cn.DatabaseInfo );

				writeGetRowsMethod( cn, writer, database, table, columns, isRevisionHistoryTable, false );
				if( isRevisionHistoryTable ) {
					writeGetRowsMethod( cn, writer, database, table, columns, true, true );
					DataAccessStatics.WriteAddLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn.Name );
				}

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
				writer.WriteLine( "	partial class " + GetClassName( cn, tableName ) );
				writer.WriteLine( "		// IMPORTANT: Change extension from \".ewlt.cs\" to \".cs\" before including in project and editing." );
				writer.WriteLine( "	}" ); // class
				writer.WriteLine( "}" ); // namespace
			}
		}

		internal static string GetClassName( DBConnection cn, string table ) {
			return StandardLibraryMethods.GetCSharpSafeClassName( table.TableNameToPascal( cn ) + "TableRetrieval" );
		}

		private static void writeGetRowsMethod( DBConnection cn, TextWriter writer, Database database, string table, TableColumns tableColumns,
		                                        bool isRevisionHistoryTable, bool excludePreviousRevisions ) {
			// header
			var methodName = isRevisionHistoryTable && !excludePreviousRevisions ? "GetRowsIncludingPreviousRevisions" : "GetRows";
			CodeGenerationStatics.AddSummaryDocComment( writer, "Retrieves the rows from the table that match the specified conditions, ordered in a stable way." );
			writer.WriteLine( "public static IEnumerable<Row> " + methodName + "( DBConnection cn, params " +
			                  DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + "[] conditions ) {" );

			// body
			writer.WriteLine( "var command = new InlineSelect( \"SELECT * FROM " + table + "\", \"ORDER BY " +
			                  StringTools.ConcatenateWithDelimiter( ", ", tableColumns.KeyColumns.Select( c => c.Name ).ToArray() ) + "\" );" );
			writer.WriteLine( "foreach( var condition in conditions )" );
			writer.WriteLine( "command.AddCondition( condition.CommandCondition );" );
			if( excludePreviousRevisions )
				writer.WriteLine( "addLatestRevisionsCondition( command );" );
			writer.WriteLine( "var results = new List<Row>();" );
			writer.WriteLine( "command.Execute( cn, r => { while( r.Read() ) results.Add( new Row( r ) ); } );" );
			writer.WriteLine( "return results;" );
			writer.WriteLine( "}" ); // GetRows
		}
	}
}