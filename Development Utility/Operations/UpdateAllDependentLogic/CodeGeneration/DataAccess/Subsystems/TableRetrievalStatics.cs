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
				var classDeclaration = "public static partial class " + table + "TableRetrieval {";
				writer.WriteLine( classDeclaration );

				var isRevisionHistoryTable = DataAccessStatics.IsRevisionHistoryTable( table, configuration );
				var columns = new TableColumns( cn, table, isRevisionHistoryTable );

				// Write nested classes.
				DataAccessStatics.WriteRowClass( writer, columns.AllColumns, cn.DatabaseInfo );

				writeGetRowsMethod( writer, database, table, columns, isRevisionHistoryTable, false );
				if( isRevisionHistoryTable ) {
					writeGetRowsMethod( writer, database, table, columns, true, true );
					DataAccessStatics.WriteAddLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn.Name );
				}

				writer.WriteLine( "}" ); // class
			}
			writer.WriteLine( "}" ); // namespace
		}

		/// <summary>
		/// Writer user-editable partial class file.
		/// </summary>
		public static void WritePartialClass( string libraryBasePath, string namespaceDeclaration, Database database, string tableName ) {
			var userEditableFileFolder = StandardLibraryMethods.CombinePaths( libraryBasePath, "DataAccess", database.SecondaryDatabaseName + "TableRetrieval" );
			var userEditableFilePath = StandardLibraryMethods.CombinePaths( userEditableFileFolder, tableName + "TableRetrieval.cs" );
			// The file will already exist if it is in source control. We want to generate a blank one if it isn't in source control to make them much easier to add.
			if( !File.Exists( userEditableFilePath ) ) {
				using( var userEditableFileWriter = IoMethods.GetTextWriterForWrite( userEditableFilePath ) ) {
					userEditableFileWriter.WriteLine( namespaceDeclaration );
					userEditableFileWriter.WriteLine( "	" + getClassDeclaration( tableName ) );
					userEditableFileWriter.WriteLine();
					userEditableFileWriter.WriteLine( "	}" ); // class
					userEditableFileWriter.WriteLine( "}" ); // namespace
				}
			}
		}

		private static string getClassDeclaration( string tableName ) {
			return "public static partial class " + tableName + "TableRetrieval {";
		}

		private static void writeGetRowsMethod( TextWriter writer, Database database, string table, TableColumns tableColumns, bool isRevisionHistoryTable,
		                                        bool excludePreviousRevisions ) {
			// header
			var methodName = isRevisionHistoryTable && !excludePreviousRevisions ? "GetRowsIncludingPreviousRevisions" : "GetRows";
			CodeGenerationStatics.AddSummaryDocComment( writer, "Retrieves the rows from the table that match the specified conditions, ordered in a stable way." );
			writer.WriteLine( "public static IEnumerable<Row> " + methodName + "( DBConnection cn, params " +
			                  DataAccessStatics.GetTableConditionInterfaceName( database, table ) + "[] conditions ) {" );

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