using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;

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

		/// <summary>
		/// Writer user-editable partial class file.
		/// </summary>
		public static void WritePartialClass( string libraryBasePath, string namespaceDeclaration, Database database, string tableName, bool isRevisionHistoryTable ) {
			// We do not write a partial class for direct modification classes.

			var userEditableFileFolder = StandardLibraryMethods.CombinePaths( libraryBasePath, "DataAccess", database.SecondaryDatabaseName + "Modification" );
			var userEditableFilePath = StandardLibraryMethods.CombinePaths( userEditableFileFolder, tableName + "Modification.cs" );
			// The file will already exist if it is in source control. We want to generate a blank one if it isn't in source control to make them much easier to add.
			if( !File.Exists( userEditableFilePath ) ) {
				using( var userEditableFileWriter = IoMethods.GetTextWriterForWrite( userEditableFilePath ) ) {
					userEditableFileWriter.WriteLine( namespaceDeclaration );
					userEditableFileWriter.WriteLine( "	partial class " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryTable ) + " {" );
					userEditableFileWriter.WriteLine();
					userEditableFileWriter.WriteLine( "	}" ); // class
					userEditableFileWriter.WriteLine( "}" ); // namespace
				}
			}
		}

		private static void writeClass( DBConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass ) {
			columns = new TableColumns( cn, tableName, isRevisionHistoryClass );

			writer.WriteLine( "public partial class " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + ": DomainDbCommand {" );

			// NOTE: Get rid of this and migrate systems to use CommandConditions namespace instead.
			foreach( var column in columns.AllColumns )
				writeEqualityConditionClass( tableName, column );

			var revisionHistorySuffix = isRevisionHistoryClass ? "AsRevision" : "";

			// Write public static methods.
			writeInsertRowMethod( tableName, revisionHistorySuffix, "", columns.KeyColumns );
			writeInsertRowMethod( tableName, revisionHistorySuffix, "WithoutAdditionalLogic", columns.KeyColumns );
			writeUpdateRowsMethod( tableName, revisionHistorySuffix, "" );
			writeUpdateRowsMethod( tableName, revisionHistorySuffix, "WithoutAdditionalLogic" );
			writeDeleteRowsMethod( tableName, revisionHistorySuffix, true );
			writeDeleteRowsMethod( tableName, revisionHistorySuffix + "WithoutAdditionalLogic", false );
			writePrivateDeleteRowsMethod( tableName, isRevisionHistoryClass );
			writer.WriteLine( "static partial void preDelete( DBConnection cn, List<" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) +
			                  "> conditions, ref " + getPostDeleteCallClassName( tableName ) + " postDeleteCall );" );

			writer.WriteLine( "private ModificationType modType;" );
			writer.WriteLine( "private List<" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) + "> conditions;" );

			foreach( var column in columns.AllColumns )
				writeFieldsAndPropertiesForColumn( column );

			foreach( var column in columns.DataColumns.Where( i => !columns.KeyColumns.Contains( i ) ) )
				FormItemStatics.WriteFormItemGetters( writer, column.GetModificationField() );

			// Write constructors.
			writeCreateForInsertMethod( tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
			writeCreateForUpdateMethod( tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
			if( columns.DataColumns.Any() )
				writeCreateForUpdateWithInitialDataMethod( tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
			writeGetConditionListMethod( tableName );
			writePrivateConstructor( tableName, isRevisionHistoryTable, isRevisionHistoryClass );

			if( columns.DataColumns.Any() )
				writeSetAllDataMethod();

			// Write execute methods and helpers.
			writeExecuteMethod( tableName );
			writer.WriteLine( "partial void preInsert( DBConnection cn );" );
			writer.WriteLine( "partial void preUpdate( DBConnection cn );" );
			writeExecuteWithoutAdditionalLogicMethod( tableName );
			writeExecuteInsertOrUpdateMethod( tableName, isRevisionHistoryClass, columns.KeyColumns, columns.IdentityColumn );
			writeAddColumnModificationsMethod( columns.NonIdentityColumns, cn.DatabaseInfo );
			if( isRevisionHistoryClass ) {
				writeCopyLatestRevisionsMethod( tableName, columns.NonIdentityColumns );
				DataAccessStatics.WriteAddLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn.Name );
			}
			writeRethrowAsEwfExceptionIfNecessary();
			writer.WriteLine( "static partial void populateConstraintNamesToViolationErrorMessages( Dictionary<string,string> constraintNamesToViolationErrorMessages );" );
			writer.WriteLine( "partial void postInsert( DBConnection cn );" );
			writer.WriteLine( "partial void postUpdate( DBConnection cn );" );
			writeMarkDataColumnValuesUnchangedMethod();

			writer.WriteLine( "}" );
		}

		private static void writeEqualityConditionClass( string tableName, Column column ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "This class will be deleted. Use the corresponding version in the CommandConditions namespace instead." );
			writer.WriteLine( "public class " + StandardLibraryMethods.GetCSharpSafeClassName( column.Name ) + "WhereClauseParam: " +
			                  getEqualityConditionClassName( tableName, column ) + " {" );
			CodeGenerationStatics.AddSummaryDocComment( writer, "This class will be deleted. Use the corresponding version in the CommandConditions namespace instead." );
			writer.WriteLine( "public " + StandardLibraryMethods.GetCSharpSafeClassName( column.Name ) + "WhereClauseParam( " + column.DataTypeName +
			                  " value ): base( value ) {}" );
			writer.WriteLine( "}" );
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
			CodeGenerationStatics.AddParamDocComment( writer, "cn", "An open database connection." ); // This prevents Resharper warnings.
			writeDocCommentsForColumnParams( columns.DataColumns );
			writer.Write( "public static " );
			if( returnColumn != null )
				writer.Write( returnColumn.DataTypeName );
			else
				writer.Write( "void" );
			writer.Write( " InsertRow" + revisionHistorySuffix + additionalLogicSuffix + "( DBConnection cn" );
			if( columns.DataColumns.Any() )
				writer.Write( ", " );
			writeColumnParameterDeclarations( columns.DataColumns );
			writer.WriteLine( " ) { " );

			// body
			writer.WriteLine( "var mod = CreateForInsert" + revisionHistorySuffix + "();" );
			writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
			writer.WriteLine( "mod.Execute" + additionalLogicSuffix + "( cn );" );
			if( returnColumn != null )
				writer.WriteLine( "return mod." + returnColumn.Name + ";" );
			writer.WriteLine( "}" );
		}

		private static void writeUpdateRowsMethod( string tableName, string revisionHistorySuffix, string additionalLogicSuffix ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Updates rows in the " + tableName + " table that match the specified conditions with the specified data." );
			CodeGenerationStatics.AddParamDocComment( writer, "cn", "An open database connection." ); // This prevents Resharper warnings.
			writeDocCommentsForColumnParams( columns.DataColumns );
			CodeGenerationStatics.AddParamDocComment( writer, "requiredCondition", "A condition." ); // This prevents Resharper warnings.
			CodeGenerationStatics.AddParamDocComment( writer, "additionalConditions", "Additional conditions." ); // This prevents Resharper warnings.
			writer.Write( "public static void UpdateRows" + revisionHistorySuffix + additionalLogicSuffix + "( DBConnection cn, " );
			writeColumnParameterDeclarations( columns.DataColumns );
			if( columns.DataColumns.Any() )
				writer.Write( ", " );
			writer.WriteLine( "" + getConditionParameterDeclarations( tableName ) + " ) {" );

			// body
			writer.WriteLine( "var mod = CreateForUpdate" + revisionHistorySuffix + "( requiredCondition, additionalConditions );" );
			writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
			writer.WriteLine( "mod.Execute" + additionalLogicSuffix + "( cn );" );
			writer.WriteLine( "}" );
		}

		private static void writeDeleteRowsMethod( string tableName, string methodNameSuffix, bool executeAdditionalLogic ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "<para>Deletes the rows that match the specified conditions and returns the number of rows deleted.</para><para>WARNING: After calling this method, delete referenced rows in other tables that are no longer needed.</para>" );
			writer.WriteLine( "public static int DeleteRows" + methodNameSuffix + "( DBConnection cn, " + getConditionParameterDeclarations( tableName ) + " ) {" );
			writer.WriteLine( "return deleteRows( cn, getConditionList( requiredCondition, additionalConditions ), " + ( executeAdditionalLogic ? "true" : "false" ) +
			                  " );" );
			writer.WriteLine( "}" );
		}

		private static void writePrivateDeleteRowsMethod( string tableName, bool isRevisionHistoryClass ) {
			// NOTE: For revision history tables, we should have the delete method automatically clean up the revisions table (but not user transactions) for us when doing direct-with-revision-bypass deletions.

			writer.WriteLine( "private static int deleteRows( DBConnection cn, List<" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) +
			                  "> conditions, bool executeAdditionalLogic ) {" );
			writer.WriteLine( "var rowsDeleted = 0;" );
			writer.WriteLine( "DataAccessMethods.ExecuteInTransaction( cn, delegate {" );

			writer.WriteLine( getPostDeleteCallClassName( tableName ) + " postDeleteCall = null;" );
			writer.WriteLine( "if( executeAdditionalLogic )" );
			writer.WriteLine( "preDelete( cn, conditions, ref postDeleteCall );" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "copyLatestRevisions( cn, conditions );" );
			writer.WriteLine( "var delete = new InlineDelete( \"" + tableName + "\" );" );
			writer.WriteLine( "conditions.ForEach( condition => delete.AddCondition( condition.CommandCondition ) );" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "addLatestRevisionsCondition( delete );" );

			writer.WriteLine( "try {" );
			writer.WriteLine( "rowsDeleted = delete.Execute( cn );" );
			writer.WriteLine( "}" ); // try
			writer.WriteLine( "catch( System.Exception e ) {" );
			writer.WriteLine( "rethrowAsEwfExceptionIfNecessary( e );" );
			writer.WriteLine( "throw;" );
			writer.WriteLine( "}" ); // catch

			writer.WriteLine( "if( postDeleteCall != null )" );
			writer.WriteLine( "postDeleteCall.Execute( cn );" );

			writer.WriteLine( "} );" );
			writer.WriteLine( "return rowsDeleted;" );
			writer.WriteLine( "}" );
		}

		private static string getPostDeleteCallClassName( string tableName ) {
			return "PostDeleteCall<IEnumerable<" + database.SecondaryDatabaseName + "TableRetrieval." + tableName + "TableRetrieval.Row>>";
		}

		private static void writeFieldsAndPropertiesForColumn( Column column ) {
			var columnIsReadOnly = !columns.DataColumns.Contains( column );

			writer.WriteLine( "private readonly DataValue<" + column.DataTypeName + "> " + getColumnFieldName( column ) + " = new DataValue<" + column.DataTypeName +
			                  ">();" );
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Gets " + ( columnIsReadOnly ? "" : "or sets " ) + "the value for the " + column.Name +
			                                            " column. Throws an exception if the value has not been initialized. " + getComment( column ) );
			var propertyDeclarationBeginning = "public " + column.DataTypeName + " " + StandardLibraryMethods.GetCSharpIdentifierSimple( column.Name ) +
			                                   " { get { return " + getColumnFieldName( column ) + ".Value; } ";
			if( columnIsReadOnly )
				writer.WriteLine( propertyDeclarationBeginning + "}" );
			else {
				writer.WriteLine( propertyDeclarationBeginning + "set { " + getColumnFieldName( column ) + ".Value = value; } }" );

				CodeGenerationStatics.AddSummaryDocComment( writer,
				                                            "Indicates whether or not the value for the " + column.Name +
				                                            " has been set since object creation or the last call to Execute, whichever was latest." );
				writer.WriteLine( "public bool " + StandardLibraryMethods.GetCSharpIdentifierSimple( column.Name ) + "HasChanged { get { return " +
				                  getColumnFieldName( column ) + ".Changed; } }" );
			}
		}

		private static void writeCreateForInsertMethod( string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Creates a modification object in insert mode, which can be used to do a piecemeal insert of a new row in the " +
			                                            tableName + " table." );
			writer.WriteLine( "public static " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForInsert" + methodNameSuffix +
			                  "() {" );
			writer.WriteLine( "return new " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " { modType = ModificationType.Insert };" );
			writer.WriteLine( "}" );
		}

		private static void writeCreateForUpdateMethod( string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Creates a modification object in update mode with the specified conditions, which can be used to do a piecemeal update of the " +
			                                            tableName + " table." );
			writer.WriteLine( "public static " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForUpdate" + methodNameSuffix +
			                  "( " + getConditionParameterDeclarations( tableName ) + " ) {" );


			// body

			writer.WriteLine( "var mod = new " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryClass ) +
			                  " { modType = ModificationType.Update, conditions = getConditionList( requiredCondition, additionalConditions ) };" );

			// Set column values that correspond to modification conditions to the values of those conditions. One reason this is important is so the primary
			// key can be retrieved in a consistent way regardless of whether the modification object is an insert or an update.
			writer.WriteLine( "foreach( var condition in mod.conditions ) {" );
			var prefix = "if";
			foreach( var column in columns.AllColumns ) {
				writer.WriteLine( prefix + "( condition is " + getEqualityConditionClassName( tableName, column ) + " )" );
				writer.WriteLine( "mod." + getColumnFieldName( column ) + ".Value = ( condition as " + getEqualityConditionClassName( tableName, column ) + " ).Value;" );
				prefix = "else if";
			}
			writer.WriteLine( "}" );
			writer.WriteLine( writer.NewLine + "mod.markDataColumnValuesUnchanged();" );

			writer.WriteLine( "return mod;" );
			writer.WriteLine( "}" );
		}

		private static void writeCreateForUpdateWithInitialDataMethod( string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass,
		                                                               string methodNameSuffix ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Creates a modification object in update mode with the specified conditions and initial data. All column values in this object will have HasChanged = false, despite being initialized. This object can then be used to do a piecemeal update of the " +
			                                            tableName + " table." );
			writer.Write( "public static " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForUpdate" + methodNameSuffix +
			              "WithInitialData( " );
			writeColumnParameterDeclarations( columns.DataColumns );
			writer.WriteLine( ", " + getConditionParameterDeclarations( tableName ) + " ) {" );

			// body
			writer.WriteLine( "var mod = CreateForUpdate" + methodNameSuffix + "( requiredCondition, additionalConditions );" );
			writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
			writer.WriteLine( "mod.markDataColumnValuesUnchanged();" );
			writer.WriteLine( "return mod;" );
			writer.WriteLine( "}" );
		}

		private static void writeGetConditionListMethod( string tableName ) {
			writer.WriteLine( "private static List<" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) + "> getConditionList( " +
			                  getConditionParameterDeclarations( tableName ) + " ) {" );
			writer.WriteLine( "var conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) + ">();" );
			writer.WriteLine( "conditions.Add( requiredCondition );" );
			writer.WriteLine( "foreach( var condition in additionalConditions )" );
			writer.WriteLine( "conditions.Add( condition );" );
			writer.WriteLine( "return conditions;" );
			writer.WriteLine( "}" );
		}

		private static string getConditionParameterDeclarations( string tableName ) {
			return "" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) + " requiredCondition, params " +
			       DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) + "[] additionalConditions";
		}

		private static void writePrivateConstructor( string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass ) {
			writer.WriteLine( "private " + getModClassName( tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + "() {}" );
		}

		private static string getModClassName( string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass ) {
			if( isRevisionHistoryTable ) {
				if( isRevisionHistoryClass )
					tableName = tableName + "Modification";
				else
					tableName = "Direct" + tableName + "ModificationWithRevisionBypass";
			}
			else
				tableName = tableName + "Modification";
			return StandardLibraryMethods.GetCSharpSafeClassName( tableName );
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
				CodeGenerationStatics.AddParamDocComment( writer, column.Name, getComment( column ) );
		}

		private static string getComment( Column column ) {
			return column.AllowsNull ? "Column allows null." : "Column does not allow null.";
		}

		private static void writeColumnParameterDeclarations( IEnumerable<Column> columns ) {
			writer.Write( StringTools.ConcatenateWithDelimiter( ", ",
			                                                    columns.Select( i => i.DataTypeName + " " + StandardLibraryMethods.GetCSharpIdentifierSimple( i.Name ) )
			                                                           .ToArray() ) );
		}

		private static void writeColumnValueAssignmentsFromParameters( IEnumerable<Column> columns, string modObjectName ) {
			foreach( var column in columns ) {
				writer.WriteLine( modObjectName + "." + StandardLibraryMethods.GetCSharpIdentifierSimple( column.Name ) + " = " +
				                  StandardLibraryMethods.GetCSharpIdentifierSimple( column.Name ) + ";" );
			}
		}

		private static void writeExecuteMethod( string tableName ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Executes this " + tableName +
			                                            " modification, persisting all changes. Executes any pre-insert, pre-update, post-insert, or post-update logic that may exist in the class." );
			writer.WriteLine( "public void Execute( DBConnection cn ) {" );
			writer.WriteLine( "DataAccessMethods.ExecuteInTransaction( cn, delegate {" );

			// The mod type may change during execute.
			writer.WriteLine( "var frozenModType = modType;" );

			writer.WriteLine( "if( frozenModType == ModificationType.Insert )" );
			writer.WriteLine( "preInsert( cn );" );
			writer.WriteLine( "else if( frozenModType == ModificationType.Update )" );
			writer.WriteLine( "preUpdate( cn );" );

			writer.WriteLine( "executeInsertOrUpdate( cn );" );

			writer.WriteLine( "if( frozenModType == ModificationType.Insert )" );
			writer.WriteLine( "postInsert( cn );" );
			writer.WriteLine( "else if( frozenModType == ModificationType.Update )" );
			writer.WriteLine( "postUpdate( cn );" );

			// This must be after the calls to postInsert and postUpdate in case their implementations need to know which column values changed.
			writer.WriteLine( "markDataColumnValuesUnchanged();" );

			writer.WriteLine( "} );" );
			writer.WriteLine( "}" );
		}

		private static void writeExecuteWithoutAdditionalLogicMethod( string tableName ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Executes this " + tableName +
			                                            " modification, persisting all changes. Does not execute pre-insert, pre-update, post-insert, or post-update logic that may exist in the class." );
			writer.WriteLine( "public void ExecuteWithoutAdditionalLogic( DBConnection cn ) {" );
			writer.WriteLine( "DataAccessMethods.ExecuteInTransaction( cn, delegate {" );
			writer.WriteLine( "executeInsertOrUpdate( cn );" );
			writer.WriteLine( "markDataColumnValuesUnchanged();" );
			writer.WriteLine( "} );" );
			writer.WriteLine( "}" );
		}

		private static void writeExecuteInsertOrUpdateMethod( string tableName, bool isRevisionHistoryClass, IEnumerable<Column> keyColumns, Column identityColumn ) {
			writer.WriteLine( "private void executeInsertOrUpdate( DBConnection cn ) {" );
			writer.WriteLine( "try {" );

			// insert

			writer.WriteLine( "if( modType == ModificationType.Insert ) {" );

			// If this is a revision history table, write code to insert a new revision when a row is inserted into this table.
			if( isRevisionHistoryClass ) {
				writer.WriteLine( "var revisionHistorySetup = AppTools.SystemLogic as RevisionHistorySetup;" );
				writer.WriteLine( getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn ) + ".Value = revisionHistorySetup.GetNextMainSequenceValue( cn );" );
				writer.WriteLine( "revisionHistorySetup.InsertRevision( cn, System.Convert.ToInt32( " + getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn ) +
				                  ".Value ), System.Convert.ToInt32( " + getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn ) +
				                  ".Value ), cn.GetUserTransactionId() );" );
			}

			writer.WriteLine( "var insert = new InlineInsert( \"" + tableName + "\" );" );
			writer.WriteLine( "addColumnModifications( insert );" );
			if( identityColumn != null )
				writer.WriteLine( getColumnFieldName( identityColumn ) + ".Value = (" + identityColumn.DataTypeName + ")insert.Execute( cn );" );
			else
				writer.WriteLine( "insert.Execute( cn );" );

			// Future calls to Execute should perform updates, not inserts. Use the values of key columns as conditions.
			writer.WriteLine( "modType = ModificationType.Update;" );
			writer.WriteLine( "conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) + ">();" );
			foreach( var column in keyColumns )
				writer.WriteLine( "conditions.Add( new " + getEqualityConditionClassName( tableName, column ) + "( " + column.Name + " ) );" );

			writer.WriteLine( "}" ); // if insert


			// update

			writer.WriteLine( "else {" );

			if( isRevisionHistoryClass )
				writer.WriteLine( "copyLatestRevisions( cn, conditions );" );

			writer.WriteLine( "var update = new InlineUpdate( \"" + tableName + "\" );" );
			writer.WriteLine( "addColumnModifications( update );" );
			writer.WriteLine( "conditions.ForEach( condition => update.AddCondition( condition.CommandCondition ) );" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "addLatestRevisionsCondition( update );" );
			writer.WriteLine( "update.Execute( cn );" );
			writer.WriteLine( "}" ); // else

			writer.WriteLine( "}" ); // try
			writer.WriteLine( "catch( System.Exception e ) {" );
			writer.WriteLine( "rethrowAsEwfExceptionIfNecessary( e );" );
			writer.WriteLine( "throw;" );
			writer.WriteLine( "}" ); // catch

			writer.WriteLine( "}" ); // method
		}

		private static string getEqualityConditionClassName( string tableName, Column column ) {
			return database.SecondaryDatabaseName + "CommandConditions." + CommandConditionStatics.GetTableEqualityConditionsClassName( tableName ) + "." +
			       CommandConditionStatics.GetConditionClassName( column.Name );
		}

		private static void writeAddColumnModificationsMethod( IEnumerable<Column> nonIdentityColumns, DatabaseInfo databaseInfo ) {
			writer.WriteLine( "private void addColumnModifications( InlineDbModificationCommand cmd ) {" );
			foreach( var column in nonIdentityColumns ) {
				writer.WriteLine( "if( " + getColumnFieldName( column ) + ".Changed )" );
				writer.WriteLine( "cmd.AddColumnModification( new InlineDbCommandColumnValue( \"" + column.Name + "\", new DbParameterValue( " +
				                  StandardLibraryMethods.GetCSharpIdentifierSimple( column.Name ) + ", \"" + column.DbTypeString + "\" ) ) );" );
			}
			writer.WriteLine( "}" );
		}

		private static void writeCopyLatestRevisionsMethod( string tableName, IEnumerable<Column> nonIdentityColumns ) {
			writer.WriteLine( "private static void copyLatestRevisions( DBConnection cn, List<" + DataAccessStatics.GetTableConditionInterfaceName( database, tableName ) +
			                  "> conditions ) {" );

			writer.WriteLine( "var revisionHistorySetup = AppTools.SystemLogic as RevisionHistorySetup;" );

			writer.WriteLine( "var command = new InlineSelect( \"SELECT " + columns.PrimaryKeyAndRevisionIdColumn.Name + " FROM " + tableName + "\" );" );
			writer.WriteLine( "conditions.ForEach( condition => command.AddCondition( condition.CommandCondition ) );" );
			writer.WriteLine( "addLatestRevisionsCondition( command );" );
			writer.WriteLine( "var latestRevisionIds = new List<int>();" );
			writer.WriteLine( "command.Execute( cn, r => { while( r.Read() ) latestRevisionIds.Add( System.Convert.ToInt32( r[0] ) ); } );" );
			writer.WriteLine( "foreach( var latestRevisionId in latestRevisionIds ) {" );

			// Get the latest revision.
			writer.WriteLine( "var latestRevision = revisionHistorySetup.GetRevision( cn, latestRevisionId );" );

			// If this condition is true, we've already modified the row in this transaction. If we were to copy it, we'd end up with two revisions of the same entity
			// in the same user transaction, which we don't support.
			writer.WriteLine( "if( latestRevision.UserTransactionId == cn.GetUserTransactionId() )" );
			writer.WriteLine( "continue;" );

			// Update the latest revision with a new user transaction.
			writer.WriteLine( "revisionHistorySetup.UpdateRevision( cn, latestRevisionId, latestRevisionId, cn.GetUserTransactionId(), latestRevisionId );" );

			// Insert a copy of the latest revision with a new ID. This will represent the revision of the data before it was changed.
			writer.WriteLine( "var copiedRevisionId = revisionHistorySetup.GetNextMainSequenceValue( cn );" );
			writer.WriteLine( "revisionHistorySetup.InsertRevision( cn, copiedRevisionId, latestRevisionId, latestRevision.UserTransactionId );" );

			// Insert a copy of the data row and make it correspond to the copy of the latest revision.
			writer.WriteLine( "var copyCommand = cn.DatabaseInfo.CreateCommand();" );
			writer.WriteLine( "copyCommand.CommandText = \"INSERT INTO " + tableName + " SELECT \";" );
			foreach( var column in nonIdentityColumns ) {
				if( column == columns.PrimaryKeyAndRevisionIdColumn ) {
					writer.WriteLine( "var revisionIdParameter = new DbCommandParameter( \"copiedRevisionId\", new DbParameterValue( copiedRevisionId ) );" );
					writer.WriteLine( "copyCommand.CommandText += revisionIdParameter.GetNameForCommandText( cn.DatabaseInfo ) + \", \";" );
					writer.WriteLine( "copyCommand.Parameters.Add( revisionIdParameter.GetAdoDotNetParameter( cn.DatabaseInfo ) );" );
				}
				else
					writer.WriteLine( "copyCommand.CommandText += \"" + column.Name + ", \";" );
			}
			writer.WriteLine( "copyCommand.CommandText = copyCommand.CommandText.Remove( copyCommand.CommandText.Length - 2 );" );
			writer.WriteLine( "copyCommand.CommandText += \" FROM " + tableName + " WHERE \";" );
			writer.WriteLine( "( new EqualityCondition( new InlineDbCommandColumnValue( \"" + columns.PrimaryKeyAndRevisionIdColumn.Name +
			                  "\", new DbParameterValue( latestRevisionId ) ) ) as InlineDbCommandCondition ).AddToCommand( copyCommand, cn.DatabaseInfo, \"latestRevisionId\" );" );
			writer.WriteLine( "cn.ExecuteNonQueryCommand( copyCommand );" );

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

		private static void writeMarkDataColumnValuesUnchangedMethod() {
			writer.WriteLine( "private void markDataColumnValuesUnchanged() {" );
			foreach( var column in columns.DataColumns )
				writer.WriteLine( getColumnFieldName( column ) + ".ClearChanged();" );
			writer.WriteLine( "}" );
		}

		private static string getColumnFieldName( Column column ) {
			return StandardLibraryMethods.GetCSharpIdentifierSimple( column.Name.ToLower() + "ColumnValue" );
		}
	}
}