using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;
using RedStapler.StandardLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems.StandardModification {
	internal static class StandardModificationStatics {
		private static TextWriter writer;
		private static Database database;
		private static TableColumns columns;

		public static string GetNamespaceDeclaration( string baseNamespace, Database database ) {
			return "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "Modification {";
		}

		internal static void Generate( DBConnection cn, TextWriter writer, string namespaceDeclaration, Database database,
		                               RedStapler.StandardLibrary.Configuration.SystemDevelopment.Database configuration ) {
			StandardModificationStatics.writer = writer;
			StandardModificationStatics.database = database;

			writer.WriteLine( namespaceDeclaration );
			foreach( var tableName in database.GetTables() ) {
				var isRevisionHistoryTable = DataAccessStatics.IsRevisionHistoryTable( tableName, configuration );

				writeClass( cn, tableName, isRevisionHistoryTable, false );
				if( isRevisionHistoryTable )
					writeClass( cn, tableName, true, true );
			}
			writer.WriteLine( "}" );
		}

		internal static void WritePartialClass( DBConnection cn, string libraryBasePath, string namespaceDeclaration, Database database, string tableName,
		                                        bool isRevisionHistoryTable ) {
			// We do not create templates for direct modification classes.
			var folderPath = StandardLibraryMethods.CombinePaths( libraryBasePath, "DataAccess", database.SecondaryDatabaseName + "Modification" );
			var templateFilePath = StandardLibraryMethods.CombinePaths( folderPath,
			                                                            GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryTable ) +
			                                                            DataAccessStatics.CSharpTemplateFileExtension );
			IoMethods.DeleteFile( templateFilePath );

			// If a real file exists, don't create a template.
			if( File.Exists( StandardLibraryMethods.CombinePaths( folderPath, GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryTable ) + ".cs" ) ) )
				return;

			using( var templateWriter = IoMethods.GetTextWriterForWrite( templateFilePath ) ) {
				templateWriter.WriteLine( namespaceDeclaration );
				templateWriter.WriteLine( "	partial class " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryTable ) + " {" );
				templateWriter.WriteLine(
					"		// IMPORTANT: Change extension from \"{0}\" to \".cs\" before including in project and editing.".FormatWith(
						DataAccessStatics.CSharpTemplateFileExtension ) );
				templateWriter.WriteLine( "	}" ); // class
				templateWriter.WriteLine( "}" ); // namespace
			}
		}

		private static void writeClass( DBConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass ) {
			columns = new TableColumns( cn, tableName, isRevisionHistoryClass );

			writer.WriteLine( "public partial class " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " {" );

			var revisionHistorySuffix = GetRevisionHistorySuffix( isRevisionHistoryClass );

			// Write public static methods.
			writeInsertRowMethod( tableName, revisionHistorySuffix, "", columns.KeyColumns );
			writeInsertRowMethod( tableName, revisionHistorySuffix, "WithoutAdditionalLogic", columns.KeyColumns );
			writeUpdateRowsMethod( cn, tableName, revisionHistorySuffix, "" );
			writeUpdateRowsMethod( cn, tableName, revisionHistorySuffix, "WithoutAdditionalLogic" );
			writeDeleteRowsMethod( cn, tableName, revisionHistorySuffix, true );
			writeDeleteRowsMethod( cn, tableName, revisionHistorySuffix + "WithoutAdditionalLogic", false );
			writePrivateDeleteRowsMethod( cn, tableName, isRevisionHistoryClass );
			writer.WriteLine( "static partial void preDelete( List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "> conditions, ref " +
			                  getPostDeleteCallClassName( cn, tableName ) + " postDeleteCall );" );

			writer.WriteLine( "private ModificationType modType;" );
			writer.WriteLine( "private List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "> conditions;" );

			foreach( var column in columns.AllColumns )
				writeFieldsAndPropertiesForColumn( column );

			foreach( var column in columns.DataColumns.Where( i => !columns.KeyColumns.Contains( i ) ) )
				FormItemStatics.WriteFormItemGetters( writer, column.GetModificationField() );

			// Write constructors.
			writeCreateForInsertMethod( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
			writeCreateForUpdateMethod( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
			if( columns.DataColumns.Any() )
				writeCreateForSingleRowUpdateMethod( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
			writeGetConditionListMethod( cn, tableName );
			writer.WriteLine( "private " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + "() {}" );

			if( columns.DataColumns.Any() )
				writeSetAllDataMethod();

			// Write execute methods and helpers.
			writeExecuteMethod( tableName );
			writer.WriteLine( "partial void preInsert();" );
			writer.WriteLine( "partial void preUpdate();" );
			writeExecuteWithoutAdditionalLogicMethod( tableName );
			writeExecuteInsertOrUpdateMethod( cn, tableName, isRevisionHistoryClass, columns.KeyColumns, columns.IdentityColumn );
			writeAddColumnModificationsMethod( columns.NonIdentityColumns );
			if( isRevisionHistoryClass ) {
				writeCopyLatestRevisionsMethod( cn, tableName, columns.NonIdentityColumns );
				DataAccessStatics.WriteGetLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn.Name );
			}
			writeRethrowAsEwfExceptionIfNecessary();
			writer.WriteLine( "static partial void populateConstraintNamesToViolationErrorMessages( Dictionary<string,string> constraintNamesToViolationErrorMessages );" );
			writer.WriteLine( "partial void postInsert();" );
			writer.WriteLine( "partial void postUpdate();" );
			writeMarkColumnValuesUnchangedMethod();

			writer.WriteLine( "}" );
		}

		internal static string GetRevisionHistorySuffix( bool isRevisionHistoryClass ) {
			return isRevisionHistoryClass ? "AsRevision" : "";
		}

		private static void writeInsertRowMethod( string tableName, string revisionHistorySuffix, string additionalLogicSuffix, IEnumerable<Column> keyColumns ) {
			Column returnColumn = null;
			var returnComment = "";
			if( keyColumns.Count() == 1 && !columns.DataColumns.Contains( keyColumns.Single() ) ) {
				returnColumn = keyColumns.Single();
				returnComment = " Returns the value of the " + returnColumn.Name + " column.";
			}

			// header
			CodeGenerationStatics.AddSummaryDocComment( writer, "Inserts a row into the " + tableName + " table." + returnComment );
			writeDocCommentsForColumnParams( columns.DataColumns );
			writer.Write( "public static " );
			writer.Write( returnColumn != null ? returnColumn.DataTypeName : "void" );
			writer.Write( " InsertRow" + revisionHistorySuffix + additionalLogicSuffix + "( " );
			writeColumnParameterDeclarations( columns.DataColumns );
			writer.WriteLine( " ) { " );

			// body
			writer.WriteLine( "var mod = CreateForInsert" + revisionHistorySuffix + "();" );
			writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
			writer.WriteLine( "mod.Execute" + additionalLogicSuffix + "();" );
			if( returnColumn != null )
				writer.WriteLine( "return mod." + returnColumn.Name + ";" );
			writer.WriteLine( "}" );
		}

		private static void writeUpdateRowsMethod( DBConnection cn, string tableName, string revisionHistorySuffix, string additionalLogicSuffix ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Updates rows in the " + tableName + " table that match the specified conditions with the specified data." );
			writeDocCommentsForColumnParams( columns.DataColumns );
			CodeGenerationStatics.AddParamDocComment( writer, "requiredCondition", "A condition." ); // This prevents Resharper warnings.
			CodeGenerationStatics.AddParamDocComment( writer, "additionalConditions", "Additional conditions." ); // This prevents Resharper warnings.
			writer.Write( "public static void UpdateRows" + revisionHistorySuffix + additionalLogicSuffix + "( " );
			writeColumnParameterDeclarations( columns.DataColumns );
			if( columns.DataColumns.Any() )
				writer.Write( ", " );
			writer.WriteLine( "" + getConditionParameterDeclarations( cn, tableName ) + " ) {" );

			// body
			writer.WriteLine( "var mod = CreateForUpdate" + revisionHistorySuffix + "( requiredCondition, additionalConditions );" );
			writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
			writer.WriteLine( "mod.Execute" + additionalLogicSuffix + "();" );
			writer.WriteLine( "}" );
		}

		private static void writeDeleteRowsMethod( DBConnection cn, string tableName, string methodNameSuffix, bool executeAdditionalLogic ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "<para>Deletes the rows that match the specified conditions and returns the number of rows deleted.</para>" +
			                                            "<para>WARNING: After calling this method, delete referenced rows in other tables that are no longer needed.</para>" );
			writer.WriteLine( "public static int DeleteRows" + methodNameSuffix + "( " + getConditionParameterDeclarations( cn, tableName ) + " ) {" );
			if( executeAdditionalLogic )
				writer.WriteLine( "return " + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );

			writer.WriteLine( "var conditions = getConditionList( requiredCondition, additionalConditions );" );

			if( executeAdditionalLogic ) {
				writer.WriteLine( getPostDeleteCallClassName( cn, tableName ) + " postDeleteCall = null;" );
				writer.WriteLine( "preDelete( conditions, ref postDeleteCall );" );
			}

			writer.WriteLine( "var rowsDeleted = deleteRows( conditions );" );

			if( executeAdditionalLogic ) {
				writer.WriteLine( "if( postDeleteCall != null )" );
				writer.WriteLine( "postDeleteCall.Execute();" );
			}

			writer.WriteLine( "return rowsDeleted;" );

			if( executeAdditionalLogic )
				writer.WriteLine( "} );" ); // cn.ExecuteInTransaction
			writer.WriteLine( "}" );
		}

		private static void writePrivateDeleteRowsMethod( DBConnection cn, string tableName, bool isRevisionHistoryClass ) {
			// NOTE: For revision history tables, we should have the delete method automatically clean up the revisions table (but not user transactions) for us when doing direct-with-revision-bypass deletions.

			writer.WriteLine( "private static int deleteRows( List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "> conditions ) {" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "return " + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );

			if( isRevisionHistoryClass )
				writer.WriteLine( "copyLatestRevisions( conditions );" );

			writer.WriteLine( "var delete = new InlineDelete( \"" + tableName + "\" );" );
			writer.WriteLine( "conditions.ForEach( condition => delete.AddCondition( condition.CommandCondition ) );" );

			if( isRevisionHistoryClass )
				writer.WriteLine( "delete.AddCondition( getLatestRevisionsCondition() );" );

			writer.WriteLine( "try {" );
			writer.WriteLine( "return delete.Execute( " + DataAccessStatics.GetConnectionExpression( database ) + " );" );
			writer.WriteLine( "}" ); // try
			writer.WriteLine( "catch( System.Exception e ) {" );
			writer.WriteLine( "rethrowAsEwfExceptionIfNecessary( e );" );
			writer.WriteLine( "throw;" );
			writer.WriteLine( "}" ); // catch

			if( isRevisionHistoryClass )
				writer.WriteLine( "} );" ); // cn.ExecuteInTransaction
			writer.WriteLine( "}" );
		}

		private static string getPostDeleteCallClassName( DBConnection cn, string tableName ) {
			return "PostDeleteCall<IEnumerable<" + database.SecondaryDatabaseName + "TableRetrieval." + TableRetrievalStatics.GetClassName( cn, tableName ) + ".Row>>";
		}

		private static void writeFieldsAndPropertiesForColumn( Column column ) {
			var columnIsReadOnly = !columns.DataColumns.Contains( column );

			writer.WriteLine( "private readonly DataValue<" + column.DataTypeName + "> " + getColumnFieldName( column ) + " = new DataValue<" + column.DataTypeName +
			                  ">();" );
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Gets " + ( columnIsReadOnly ? "" : "or sets " ) + "the value for the " + column.Name +
			                                            " column. Throws an exception if the value has not been initialized. " + getComment( column ) );
			var propertyDeclarationBeginning = "public " + column.DataTypeName + " " +
			                                   StandardLibraryMethods.GetCSharpIdentifierSimple( column.PascalCasedNameExceptForOracle ) + " { get { return " +
			                                   getColumnFieldName( column ) + ".Value; } ";
			if( columnIsReadOnly )
				writer.WriteLine( propertyDeclarationBeginning + "}" );
			else {
				writer.WriteLine( propertyDeclarationBeginning + "set { " + getColumnFieldName( column ) + ".Value = value; } }" );

				CodeGenerationStatics.AddSummaryDocComment( writer,
				                                            "Indicates whether or not the value for the " + column.Name +
				                                            " has been set since object creation or the last call to Execute, whichever was latest." );
				writer.WriteLine( "public bool " + StandardLibraryMethods.GetCSharpIdentifierSimple( column.PascalCasedNameExceptForOracle ) + "HasChanged { get { return " +
				                  getColumnFieldName( column ) + ".Changed; } }" );
			}
		}

		private static void writeCreateForInsertMethod( DBConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass,
		                                                string methodNameSuffix ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Creates a modification object in insert mode, which can be used to do a piecemeal insert of a new row in the " +
			                                            tableName + " table." );
			writer.WriteLine( "public static " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForInsert" + methodNameSuffix +
			                  "() {" );
			writer.WriteLine( "return new " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " { modType = ModificationType.Insert };" );
			writer.WriteLine( "}" );
		}

		private static void writeCreateForUpdateMethod( DBConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass,
		                                                string methodNameSuffix ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Creates a modification object in update mode with the specified conditions, which can be used to do a piecemeal update of the " +
			                                            tableName + " table." );
			writer.WriteLine( "public static " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForUpdate" + methodNameSuffix +
			                  "( " + getConditionParameterDeclarations( cn, tableName ) + " ) {" );


			// body

			writer.WriteLine( "var mod = new " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) +
			                  " { modType = ModificationType.Update, conditions = getConditionList( requiredCondition, additionalConditions ) };" );

			// Set column values that correspond to modification conditions to the values of those conditions. One reason this is important is so the primary
			// key can be retrieved in a consistent way regardless of whether the modification object is an insert or an update.
			writer.WriteLine( "foreach( var condition in mod.conditions ) {" );
			var prefix = "if";
			foreach( var column in columns.AllColumns ) {
				writer.WriteLine( prefix + "( condition is " + DataAccessStatics.GetEqualityConditionClassName( cn, database, tableName, column ) + " )" );
				writer.WriteLine( "mod." + getColumnFieldName( column ) + ".Value = ( condition as " +
				                  DataAccessStatics.GetEqualityConditionClassName( cn, database, tableName, column ) + " ).Value;" );
				prefix = "else if";
			}
			writer.WriteLine( "}" );
			writer.WriteLine( writer.NewLine + "mod.markColumnValuesUnchanged();" );

			writer.WriteLine( "return mod;" );
			writer.WriteLine( "}" );
		}

		private static void writeCreateForSingleRowUpdateMethod( DBConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass,
		                                                         string methodNameSuffix ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Creates a modification object in single-row update mode with the specified current data. All column values in this object will have HasChanged = false, despite being initialized. This object can then be used to do a piecemeal update of the " +
			                                            tableName + " table." );
			writer.Write( "public static " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForSingleRowUpdate" +
			              methodNameSuffix + "( " );
			writeColumnParameterDeclarations( columns.AllColumns );
			writer.WriteLine( " ) {" );


			// body

			writer.WriteLine( "var mod = new " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) +
			                  " { modType = ModificationType.Update };" );

			// Use the values of key columns as conditions.
			writer.WriteLine( "mod.conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + ">();" );
			foreach( var column in columns.KeyColumns ) {
				writer.WriteLine( "mod.conditions.Add( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, tableName, column ) + "( " + "@" +
				                  StandardLibraryMethods.GetCSharpIdentifierSimple( column.CamelCasedName ) + " ) );" );
			}

			writeColumnValueAssignmentsFromParameters( columns.AllColumns, "mod" );
			writer.WriteLine( "mod.markColumnValuesUnchanged();" );
			writer.WriteLine( "return mod;" );
			writer.WriteLine( "}" );
		}

		private static void writeGetConditionListMethod( DBConnection cn, string tableName ) {
			writer.WriteLine( "private static List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "> getConditionList( " +
			                  getConditionParameterDeclarations( cn, tableName ) + " ) {" );
			writer.WriteLine( "var conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + ">();" );
			writer.WriteLine( "conditions.Add( requiredCondition );" );
			writer.WriteLine( "foreach( var condition in additionalConditions )" );
			writer.WriteLine( "conditions.Add( condition );" );
			writer.WriteLine( "return conditions;" );
			writer.WriteLine( "}" );
		}

		private static string getConditionParameterDeclarations( DBConnection cn, string tableName ) {
			return "" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + " requiredCondition, params " +
			       DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "[] additionalConditions";
		}

		internal static string GetClassName( DBConnection cn, string table, bool isRevisionHistoryTable, bool isRevisionHistoryClass ) {
			return
				StandardLibraryMethods.GetCSharpSafeClassName( isRevisionHistoryTable && !isRevisionHistoryClass
					                                               ? "Direct" + table.TableNameToPascal( cn ) + "ModificationWithRevisionBypass"
					                                               : table.TableNameToPascal( cn ) + "Modification" );
		}

		private static void writeSetAllDataMethod() {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Sets all column values. This is useful for enforcing the number of arguments when deferred execution is needed." );
			writeDocCommentsForColumnParams( columns.DataColumns );
			writer.Write( "public void SetAllData( " );
			writeColumnParameterDeclarations( columns.DataColumns );
			writer.WriteLine( " ) {" );

			// body
			writeColumnValueAssignmentsFromParameters( columns.DataColumns, "this" );
			writer.WriteLine( "}" );
		}

		private static void writeDocCommentsForColumnParams( IEnumerable<Column> columns ) {
			foreach( var column in columns )
				CodeGenerationStatics.AddParamDocComment( writer, column.CamelCasedName, getComment( column ) );
		}

		private static string getComment( Column column ) {
			return column.AllowsNull ? "Column allows null." : "Column does not allow null.";
		}

		private static void writeColumnParameterDeclarations( IEnumerable<Column> columns ) {
			writer.Write( StringTools.ConcatenateWithDelimiter( ", ",
			                                                    columns.Select(
				                                                    i => i.DataTypeName + " @" + StandardLibraryMethods.GetCSharpIdentifierSimple( i.CamelCasedName ) )
			                                                           .ToArray() ) );
		}

		private static void writeColumnValueAssignmentsFromParameters( IEnumerable<Column> columns, string modObjectName ) {
			foreach( var column in columns ) {
				writer.WriteLine( modObjectName + "." + getColumnFieldName( column ) + ".Value = @" +
				                  StandardLibraryMethods.GetCSharpIdentifierSimple( column.CamelCasedName ) + ";" );
			}
		}

		private static void writeExecuteMethod( string tableName ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Executes this " + tableName +
			                                            " modification, persisting all changes. Executes any pre-insert, pre-update, post-insert, or post-update logic that may exist in the class." );
			writer.WriteLine( "public void Execute() {" );
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( delegate {" );

			// The mod type may change during execute.
			writer.WriteLine( "var frozenModType = modType;" );

			writer.WriteLine( "if( frozenModType == ModificationType.Insert )" );
			writer.WriteLine( "preInsert();" );
			writer.WriteLine( "else if( frozenModType == ModificationType.Update )" );
			writer.WriteLine( "preUpdate();" );

			writer.WriteLine( "executeInsertOrUpdate();" );

			writer.WriteLine( "if( frozenModType == ModificationType.Insert )" );
			writer.WriteLine( "postInsert();" );
			writer.WriteLine( "else if( frozenModType == ModificationType.Update )" );
			writer.WriteLine( "postUpdate();" );

			// This must be after the calls to postInsert and postUpdate in case their implementations need to know which column values changed.
			writer.WriteLine( "markColumnValuesUnchanged();" );

			writer.WriteLine( "} );" );
			writer.WriteLine( "}" );
		}

		private static void writeExecuteWithoutAdditionalLogicMethod( string tableName ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Executes this " + tableName +
			                                            " modification, persisting all changes. Does not execute pre-insert, pre-update, post-insert, or post-update logic that may exist in the class." );
			writer.WriteLine( "public void ExecuteWithoutAdditionalLogic() {" );
			writer.WriteLine( "executeInsertOrUpdate();" );
			writer.WriteLine( "markColumnValuesUnchanged();" );
			writer.WriteLine( "}" );
		}

		private static void writeExecuteInsertOrUpdateMethod( DBConnection cn, string tableName, bool isRevisionHistoryClass, IEnumerable<Column> keyColumns,
		                                                      Column identityColumn ) {
			writer.WriteLine( "private void executeInsertOrUpdate() {" );
			writer.WriteLine( "try {" );
			if( isRevisionHistoryClass )
				writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( delegate {" );


			// insert

			writer.WriteLine( "if( modType == ModificationType.Insert ) {" );

			// If this is a revision history table, write code to insert a new revision when a row is inserted into this table.
			if( isRevisionHistoryClass ) {
				writer.WriteLine( "var revisionHistorySetup = (RevisionHistoryProvider)DataAccessStatics.SystemProvider;" );
				writer.WriteLine( getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn ) + ".Value = revisionHistorySetup.GetNextMainSequenceValue();" );
				writer.WriteLine( "revisionHistorySetup.InsertRevision( System.Convert.ToInt32( " + getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn ) +
				                  ".Value ), System.Convert.ToInt32( " + getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn ) + ".Value ), " +
				                  DataAccessStatics.GetConnectionExpression( database ) + ".GetUserTransactionId() );" );
			}

			writer.WriteLine( "var insert = new InlineInsert( \"" + tableName + "\" );" );
			writer.WriteLine( "addColumnModifications( insert );" );
			if( identityColumn != null ) {
				writer.WriteLine( getColumnFieldName( identityColumn ) + ".Value = (" + identityColumn.DataTypeName + ")insert.Execute( " +
				                  DataAccessStatics.GetConnectionExpression( database ) + " );" );
			}
			else
				writer.WriteLine( "insert.Execute( " + DataAccessStatics.GetConnectionExpression( database ) + " );" );

			// Future calls to Execute should perform updates, not inserts. Use the values of key columns as conditions.
			writer.WriteLine( "modType = ModificationType.Update;" );
			writer.WriteLine( "conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + ">();" );
			foreach( var column in keyColumns ) {
				writer.WriteLine( "conditions.Add( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, tableName, column ) + "( " +
				                  StandardLibraryMethods.GetCSharpIdentifierSimple( column.PascalCasedNameExceptForOracle ) + " ) );" );
			}

			writer.WriteLine( "}" ); // if insert


			// update
			writer.WriteLine( "else {" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "copyLatestRevisions( conditions );" );
			writer.WriteLine( "var update = new InlineUpdate( \"" + tableName + "\" );" );
			writer.WriteLine( "addColumnModifications( update );" );
			writer.WriteLine( "conditions.ForEach( condition => update.AddCondition( condition.CommandCondition ) );" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "update.AddCondition( getLatestRevisionsCondition() );" );
			writer.WriteLine( "update.Execute( " + DataAccessStatics.GetConnectionExpression( database ) + " );" );
			writer.WriteLine( "}" ); // else

			if( isRevisionHistoryClass )
				writer.WriteLine( "} );" ); // cn.ExecuteInTransaction
			writer.WriteLine( "}" ); // try

			writer.WriteLine( "catch( System.Exception e ) {" );
			writer.WriteLine( "rethrowAsEwfExceptionIfNecessary( e );" );
			writer.WriteLine( "throw;" );
			writer.WriteLine( "}" ); // catch

			writer.WriteLine( "}" ); // method
		}

		private static void writeAddColumnModificationsMethod( IEnumerable<Column> nonIdentityColumns ) {
			writer.WriteLine( "private void addColumnModifications( InlineDbModificationCommand cmd ) {" );
			foreach( var column in nonIdentityColumns ) {
				writer.WriteLine( "if( " + getColumnFieldName( column ) + ".Changed )" );
				writer.WriteLine( "cmd.AddColumnModification( new InlineDbCommandColumnValue( \"" + column.Name + "\", new DbParameterValue( " +
				                  StandardLibraryMethods.GetCSharpIdentifierSimple( column.PascalCasedNameExceptForOracle ) + ", \"" + column.DbTypeString + "\" ) ) );" );
			}
			writer.WriteLine( "}" );
		}

		private static void writeCopyLatestRevisionsMethod( DBConnection cn, string tableName, IEnumerable<Column> nonIdentityColumns ) {
			writer.WriteLine( "private static void copyLatestRevisions( List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) +
			                  "> conditions ) {" );

			writer.WriteLine( "var revisionHistorySetup = (RevisionHistoryProvider)DataAccessStatics.SystemProvider;" );

			writer.WriteLine( "var command = new InlineSelect( \"SELECT " + columns.PrimaryKeyAndRevisionIdColumn.Name + " FROM " + tableName + "\" );" );
			writer.WriteLine( "conditions.ForEach( condition => command.AddCondition( condition.CommandCondition ) );" );
			writer.WriteLine( "command.AddCondition( getLatestRevisionsCondition() );" );
			writer.WriteLine( "var latestRevisionIds = new List<int>();" );
			writer.WriteLine( "command.Execute( " + DataAccessStatics.GetConnectionExpression( database ) +
			                  ", r => { while( r.Read() ) latestRevisionIds.Add( System.Convert.ToInt32( r[0] ) ); } );" );
			writer.WriteLine( "foreach( var latestRevisionId in latestRevisionIds ) {" );

			// Get the latest revision.
			writer.WriteLine( "var latestRevision = revisionHistorySetup.GetRevision( latestRevisionId );" );

			// If this condition is true, we've already modified the row in this transaction. If we were to copy it, we'd end up with two revisions of the same entity
			// in the same user transaction, which we don't support.
			writer.WriteLine( "if( latestRevision.UserTransactionId == " + DataAccessStatics.GetConnectionExpression( database ) + ".GetUserTransactionId() )" );
			writer.WriteLine( "continue;" );

			// Update the latest revision with a new user transaction.
			writer.WriteLine( "revisionHistorySetup.UpdateRevision( latestRevisionId, latestRevisionId, " + DataAccessStatics.GetConnectionExpression( database ) +
			                  ".GetUserTransactionId(), latestRevisionId );" );

			// Insert a copy of the latest revision with a new ID. This will represent the revision of the data before it was changed.
			writer.WriteLine( "var copiedRevisionId = revisionHistorySetup.GetNextMainSequenceValue();" );
			writer.WriteLine( "revisionHistorySetup.InsertRevision( copiedRevisionId, latestRevisionId, latestRevision.UserTransactionId );" );

			// Insert a copy of the data row and make it correspond to the copy of the latest revision.
			writer.WriteLine( "var copyCommand = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
			writer.WriteLine( "copyCommand.CommandText = \"INSERT INTO " + tableName + " SELECT \";" );
			foreach( var column in nonIdentityColumns ) {
				if( column == columns.PrimaryKeyAndRevisionIdColumn ) {
					writer.WriteLine( "var revisionIdParameter = new DbCommandParameter( \"copiedRevisionId\", new DbParameterValue( copiedRevisionId ) );" );
					writer.WriteLine( "copyCommand.CommandText += revisionIdParameter.GetNameForCommandText( " + DataAccessStatics.GetConnectionExpression( database ) +
					                  ".DatabaseInfo ) + \", \";" );
					writer.WriteLine( "copyCommand.Parameters.Add( revisionIdParameter.GetAdoDotNetParameter( " + DataAccessStatics.GetConnectionExpression( database ) +
					                  ".DatabaseInfo ) );" );
				}
				else
					writer.WriteLine( "copyCommand.CommandText += \"" + column.Name + ", \";" );
			}
			writer.WriteLine( "copyCommand.CommandText = copyCommand.CommandText.Remove( copyCommand.CommandText.Length - 2 );" );
			writer.WriteLine( "copyCommand.CommandText += \" FROM " + tableName + " WHERE \";" );
			writer.WriteLine( "( new EqualityCondition( new InlineDbCommandColumnValue( \"" + columns.PrimaryKeyAndRevisionIdColumn.Name +
			                  "\", new DbParameterValue( latestRevisionId ) ) ) as InlineDbCommandCondition ).AddToCommand( copyCommand, " +
			                  DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo, \"latestRevisionId\" );" );
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteNonQueryCommand( copyCommand );" );

			writer.WriteLine( "}" ); // foreach
			writer.WriteLine( "}" ); // method
		}

		private static void writeRethrowAsEwfExceptionIfNecessary() {
			writer.WriteLine( "private static void rethrowAsEwfExceptionIfNecessary( System.Exception e ) {" );
			writer.WriteLine( "var constraintNamesToViolationErrorMessages = new Dictionary<string,string>();" );
			writer.WriteLine( "populateConstraintNamesToViolationErrorMessages( constraintNamesToViolationErrorMessages );" );
			writer.WriteLine( "foreach( var pair in constraintNamesToViolationErrorMessages )" );
			writer.WriteLine( "if( e.GetBaseException().Message.ToLower().Contains( pair.Key.ToLower() ) ) throw new EwfException( pair.Value );" );
			writer.WriteLine( "}" ); // method
		}

		private static void writeMarkColumnValuesUnchangedMethod() {
			writer.WriteLine( "private void markColumnValuesUnchanged() {" );
			foreach( var column in columns.AllColumns )
				writer.WriteLine( getColumnFieldName( column ) + ".ClearChanged();" );
			writer.WriteLine( "}" );
		}

		private static string getColumnFieldName( Column column ) {
			return StandardLibraryMethods.GetCSharpIdentifierSimple( column.CamelCasedName + "ColumnValue" );
		}
	}
}