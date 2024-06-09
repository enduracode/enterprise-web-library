using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems.StandardModification;

internal static class StandardModificationStatics {
	private static TextWriter writer = null!;
	private static Database database = null!;
	private static TableColumns columns = null!;

	internal static void Generate(
		DatabaseConnection cn, TextWriter writer, string baseNamespace, string templateBasePath, Database database,
		IEnumerable<( string name, bool hasModTable )> tables, EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		StandardModificationStatics.writer = writer;
		StandardModificationStatics.database = database;

		var subsystemName = "{0}Modification".FormatWith( database.SecondaryDatabaseName );
		var subsystemNamespace = "namespace {0}.{1}".FormatWith( baseNamespace, subsystemName );

		writer.WriteLine( "{0} {{".FormatWith( subsystemNamespace ) );
		foreach( var table in tables ) {
			var isRevisionHistoryTable = DataAccessStatics.IsRevisionHistoryTable( table.name, configuration );

			writeClass( cn, table.name, isRevisionHistoryTable, table.hasModTable, false );
			if( isRevisionHistoryTable )
				writeClass( cn, table.name, true, table.hasModTable, true );

			// We do not create templates for direct modification classes.
			var templateClassName = GetClassName( cn, table.name, isRevisionHistoryTable, isRevisionHistoryTable );

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
		writer.WriteLine( "}" );
	}

	private static void writeClass( DatabaseConnection cn, string tableName, bool isRevisionHistoryTable, bool hasModTable, bool isRevisionHistoryClass ) {
		columns = new TableColumns( cn, tableName, isRevisionHistoryClass );

		writer.WriteLine( "public partial class " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " {" );

		var revisionHistorySuffix = GetRevisionHistorySuffix( isRevisionHistoryClass );

		writeInsertRowMethod( tableName, revisionHistorySuffix, "" );
		writeInsertRowMethod( tableName, revisionHistorySuffix, "WithoutAdditionalLogic" );
		writeUpdateRowsMethod( cn, tableName, revisionHistorySuffix, "", false );
		writeUpdateRowsMethod( cn, tableName, revisionHistorySuffix, "", true );
		writeUpdateRowsMethod( cn, tableName, revisionHistorySuffix, "WithoutAdditionalLogic", false );
		writeUpdateRowsMethod( cn, tableName, revisionHistorySuffix, "WithoutAdditionalLogic", true );
		writeDeleteRowsMethod( cn, tableName, revisionHistorySuffix, false, true );
		writeDeleteRowsMethod( cn, tableName, revisionHistorySuffix, true, true );
		writeDeleteRowsMethod( cn, tableName, revisionHistorySuffix + "WithoutAdditionalLogic", false, false );
		writeDeleteRowsMethod( cn, tableName, revisionHistorySuffix + "WithoutAdditionalLogic", true, false );
		writePrivateDeleteRowsMethod( cn, tableName, hasModTable, isRevisionHistoryClass );
		writer.WriteLine(
			"static partial void preDelete( List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "> conditions, ref " +
			getPostDeleteCallClassName( cn, tableName ) + "? postDeleteCall );" );

		writeCreateForInsertMethod( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
		writeCreateForUpdateMethod( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
		writeGetConditionListMethod( cn, tableName );
		if( columns.HasKeyColumns && columns.DataColumns.Any() )
			writeCreateForSingleRowUpdateMethod( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );

		writer.WriteLine( "private ModificationType modType;" );
		writer.WriteLine( "private List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + ">? conditions;" );

		foreach( var column in columns.AllColumnsExceptRowVersion )
			writeFieldsAndPropertiesForColumn( column );

		foreach( var column in columns.DataColumns )
			FormItemStatics.WriteFormItemGetters( writer, column.GetModificationField( getColumnFieldName( column ) ) );

		writer.WriteLine( "private " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + "() {}" );

		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Gets whether the value for any column has been set since object creation or the last call to Execute, whichever was latest." );
		writer.WriteLine(
			"public bool AnyColumnValueHasChanged => {0};".FormatWith(
				StringTools.ConcatenateWithDelimiter(
					" || ",
					columns.AllColumnsExceptRowVersion.Select( i => "{0}.Changed".FormatWith( getColumnFieldName( i ) ) ) ) ) );

		if( columns.DataColumns.Any() )
			writeSetAllDataMethod();

		// Write execute methods and helpers.
		writeExecuteMethod( tableName );
		writer.WriteLine( "partial void preInsert();" );
		writer.WriteLine( "partial void preUpdate();" );
		writeExecuteWithoutAdditionalLogicMethod( tableName );
		writeExecuteInsertOrUpdateMethod( cn, tableName, columns.IdentityColumn, hasModTable, isRevisionHistoryClass );
		writeGetColumnModificationValuesMethod( columns.AllNonIdentityColumnsExceptRowVersion );
		if( isRevisionHistoryClass ) {
			writeCopyLatestRevisionsMethod( cn, tableName, columns.AllNonIdentityColumnsExceptRowVersion, hasModTable );
			DataAccessStatics.WriteGetLatestRevisionsConditionMethod( writer, columns.PrimaryKeyAndRevisionIdColumn!.Name );
		}
		writeRethrowAsEwfExceptionIfNecessary();
		writer.WriteLine(
			"static partial void populateConstraintNamesToViolationErrorMessages( Dictionary<string,string> constraintNamesToViolationErrorMessages );" );
		writer.WriteLine( "partial void postInsert();" );
		writer.WriteLine( "partial void postUpdate();" );
		writeMarkColumnValuesUnchangedMethod();

		writer.WriteLine( "}" );
	}

	internal static string GetRevisionHistorySuffix( bool isRevisionHistoryClass ) => isRevisionHistoryClass ? "AsRevision" : "";

	private static void writeInsertRowMethod( string tableName, string revisionHistorySuffix, string additionalLogicSuffix ) {
		Column? returnColumn = null;
		var returnComment = "";
		if( columns.HasKeyColumns && columns.KeyColumns.Count == 1 && !columns.DataColumns.Contains( columns.KeyColumns.Single() ) ) {
			returnColumn = columns.KeyColumns.Single();
			returnComment = " Returns the value of the " + returnColumn.Name + " column.";
		}

		// header
		CodeGenerationStatics.AddSummaryDocComment( writer, "Inserts a row into the " + tableName + " table." + returnComment );
		writeDocCommentsForColumnParams( columns.DataColumns );
		writer.Write( "public static " );
		writer.Write( returnColumn != null ? returnColumn.DataTypeName : "void" );
		writer.Write( " InsertRow" + revisionHistorySuffix + additionalLogicSuffix + "( " );
		writeColumnParameterDeclarations( columns.DataColumns );
		if( columns.DataColumns.Any() )
			writer.Write( ", " );
		writer.WriteLine( "bool isLongRunning = false ) { " );

		// body
		writer.WriteLine( "var mod = CreateForInsert" + revisionHistorySuffix + "();" );
		writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
		writer.WriteLine( "mod.Execute{0}( isLongRunning: isLongRunning );".FormatWith( additionalLogicSuffix ) );
		if( returnColumn != null )
			writer.WriteLine( "return mod." + returnColumn.Name + ";" );
		writer.WriteLine( "}" );
	}

	private static void writeUpdateRowsMethod(
		DatabaseConnection cn, string tableName, string revisionHistorySuffix, string additionalLogicSuffix, bool includeIsLongRunningParameter ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Updates rows in the " + tableName + " table that match the specified conditions with the specified data." );
		writeDocCommentsForColumnParams( columns.DataColumns );
		CodeGenerationStatics.AddParamDocComment( writer, "requiredCondition", "A condition." ); // This prevents Resharper warnings.
		CodeGenerationStatics.AddParamDocComment( writer, "additionalConditions", "Additional conditions." ); // This prevents Resharper warnings.
		writer.Write( "public static void UpdateRows" + revisionHistorySuffix + additionalLogicSuffix + "( " );
		writeColumnParameterDeclarations( columns.DataColumns );
		if( columns.DataColumns.Any() )
			writer.Write( ", " );
		writer.WriteLine(
			"{0} ) {{".FormatWith(
				StringTools.ConcatenateWithDelimiter(
					", ",
					includeIsLongRunningParameter ? "bool isLongRunning" : "",
					getConditionParameterDeclarations( cn, tableName ) ) ) );

		// body
		writer.WriteLine( "var mod = CreateForUpdate" + revisionHistorySuffix + "( requiredCondition, additionalConditions );" );
		writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
		writer.WriteLine( "mod.Execute{0}( isLongRunning: {1} );".FormatWith( additionalLogicSuffix, includeIsLongRunningParameter ? "isLongRunning" : "false" ) );
		writer.WriteLine( "}" );
	}

	private static void writeDeleteRowsMethod(
		DatabaseConnection cn, string tableName, string methodNameSuffix, bool includeIsLongRunningParameter, bool executeAdditionalLogic ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"<para>Deletes the rows that match the specified conditions and returns the number of rows deleted.</para>" +
			"<para>WARNING: After calling this method, delete referenced rows in other tables that are no longer needed.</para>" );
		writer.WriteLine(
			"public static int DeleteRows{0}( {1} ) {{".FormatWith(
				methodNameSuffix,
				StringTools.ConcatenateWithDelimiter(
					", ",
					includeIsLongRunningParameter ? "bool isLongRunning" : "",
					getConditionParameterDeclarations( cn, tableName ) ) ) );
		if( executeAdditionalLogic )
			writer.WriteLine( "return " + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );

		writer.WriteLine( "var conditions = getConditionList( requiredCondition, additionalConditions );" );

		if( executeAdditionalLogic ) {
			writer.WriteLine( getPostDeleteCallClassName( cn, tableName ) + "? postDeleteCall = null;" );
			writer.WriteLine( "preDelete( conditions, ref postDeleteCall );" );
		}

		writer.WriteLine( "var rowsDeleted = deleteRows( conditions, {0} );".FormatWith( includeIsLongRunningParameter ? "isLongRunning" : "false" ) );

		if( executeAdditionalLogic ) {
			writer.WriteLine( "if( postDeleteCall is not null )" );
			writer.WriteLine( "postDeleteCall.Execute();" );
		}

		writer.WriteLine( "return rowsDeleted;" );

		if( executeAdditionalLogic )
			writer.WriteLine( "} );" ); // cn.ExecuteInTransaction
		writer.WriteLine( "}" );
	}

	private static void writePrivateDeleteRowsMethod( DatabaseConnection cn, string tableName, bool hasModTable, bool isRevisionHistoryClass ) {
		// NOTE: For revision history tables, we should have the delete method automatically clean up the revisions table (but not user transactions) for us when doing direct-with-revision-bypass deletions.

		writer.WriteLine(
			"private static int deleteRows( List<{0}> conditions, bool isLongRunning ) {{".FormatWith(
				DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) ) );
		if( hasModTable || isRevisionHistoryClass )
			writer.WriteLine( "return " + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );

		if( isRevisionHistoryClass )
			writer.WriteLine( "copyLatestRevisions( conditions, isLongRunning );" );

		if( hasModTable ) {
			writer.WriteLine(
				"var modTableInsert = new InlineInsertWithSelect( \"{0}\", new[] {{ {1} }}, \"{2}\" );".FormatWith(
					tableName + DatabaseOps.GetModificationTableSuffix( database ),
					StringTools.ConcatenateWithDelimiter( ", ", columns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ) ),
					tableName ) );
			foreach( var i in columns.KeyColumns )
				writer.WriteLine( "modTableInsert.AddSelectExpression( \"{0}\" );".FormatWith( i.DelimitedIdentifier.EscapeForLiteral() ) );
			writer.WriteLine( "modTableInsert.AddConditions( conditions.Select( i => i.CommandCondition ) );" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "modTableInsert.AddConditions( getLatestRevisionsCondition().ToCollection() );" );
			writer.WriteLine( "modTableInsert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		}

		writer.WriteLine( "var delete = new InlineDelete( \"" + tableName + "\" );" );
		writer.WriteLine( "delete.AddConditions( conditions.Select( i => i.CommandCondition ) );" );
		if( isRevisionHistoryClass )
			writer.WriteLine( "delete.AddConditions( getLatestRevisionsCondition().ToCollection() );" );

		writer.WriteLine( "try {" );
		writer.WriteLine( "return delete.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine( "}" ); // try
		writer.WriteLine( "catch( System.Exception e ) {" );
		writer.WriteLine( "rethrowAsDataModificationExceptionIfNecessary( e );" );
		writer.WriteLine( "throw;" );
		writer.WriteLine( "}" ); // catch

		if( hasModTable || isRevisionHistoryClass )
			writer.WriteLine( "} );" ); // ExecuteInTransaction
		writer.WriteLine( "}" );
	}

	private static string getPostDeleteCallClassName( DatabaseConnection cn, string tableName ) =>
		"PostDeleteCall<IEnumerable<" + database.SecondaryDatabaseName + "TableRetrieval." + TableRetrievalStatics.GetClassName( cn, tableName ) + ".Row>>";

	private static void writeCreateForInsertMethod(
		DatabaseConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Creates a modification object in insert mode, which can be used to do a piecemeal insert of a new row in the " + tableName + " table." );
		writer.WriteLine(
			"public static " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForInsert" + methodNameSuffix + "() {" );
		writer.WriteLine(
			"return new " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " { modType = ModificationType.Insert };" );
		writer.WriteLine( "}" );
	}

	private static void writeCreateForUpdateMethod(
		DatabaseConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Creates a modification object in update mode with the specified conditions, which can be used to do a piecemeal update of the " + tableName +
			" table." );
		writer.WriteLine(
			"public static " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForUpdate" + methodNameSuffix + "( " +
			getConditionParameterDeclarations( cn, tableName ) + " ) {" );


		// body

		writer.WriteLine(
			"var mod = new " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) +
			" { modType = ModificationType.Update, conditions = getConditionList( requiredCondition, additionalConditions ) };" );

		// Set column values that correspond to modification conditions to the values of those conditions. One reason this is important is so the primary
		// key can be retrieved in a consistent way regardless of whether the modification object is an insert or an update.
		writer.WriteLine( "foreach( var condition in mod.conditions ) {" );
		var prefix = "if";
		foreach( var column in columns.AllColumnsExceptRowVersion ) {
			writer.WriteLine(
				"{0}( condition is {1} {2} )".FormatWith(
					prefix,
					DataAccessStatics.GetEqualityConditionClassName( cn, database, tableName, column ),
					EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) ) );
			writer.WriteLine( "mod.{0}.Value = {1}.Value;".FormatWith( getColumnFieldName( column ), EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) ) );
			prefix = "else if";
		}
		writer.WriteLine( "}" );
		writer.WriteLine( writer.NewLine + "mod.markColumnValuesUnchanged();" );

		writer.WriteLine( "return mod;" );
		writer.WriteLine( "}" );
	}

	private static void writeGetConditionListMethod( DatabaseConnection cn, string tableName ) {
		writer.WriteLine(
			"private static List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "> getConditionList( " +
			getConditionParameterDeclarations( cn, tableName ) + " ) {" );
		writer.WriteLine( "var conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + ">();" );
		writer.WriteLine( "conditions.Add( requiredCondition );" );
		writer.WriteLine( "foreach( var condition in additionalConditions )" );
		writer.WriteLine( "conditions.Add( condition );" );
		writer.WriteLine( "return conditions;" );
		writer.WriteLine( "}" );
	}

	private static string getConditionParameterDeclarations( DatabaseConnection cn, string tableName ) =>
		"" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + " requiredCondition, params " +
		DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + "[] additionalConditions";

	private static void writeCreateForSingleRowUpdateMethod(
		DatabaseConnection cn, string tableName, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Creates a modification object in single-row update mode with the specified current data. All column values in this object will have HasChanged = false, despite being initialized. This object can then be used to do a piecemeal update of the " +
			tableName + " table." );
		writer.Write(
			"public static " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForSingleRowUpdate" + methodNameSuffix +
			"( " );
		writeColumnParameterDeclarations( columns.AllColumnsExceptRowVersion );
		writer.WriteLine( " ) {" );


		// body

		writer.WriteLine(
			"var mod = new " + GetClassName( cn, tableName, isRevisionHistoryTable, isRevisionHistoryClass ) + " { modType = ModificationType.Update };" );

		// Use the values of key columns as conditions.
		writer.WriteLine( "mod.conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + ">();" );
		foreach( var column in columns.KeyColumns )
			writer.WriteLine(
				"mod.conditions.Add( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, tableName, column ) + "( " +
				EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) + " ) );" );

		writeColumnValueAssignmentsFromParameters( columns.AllColumnsExceptRowVersion, "mod" );
		writer.WriteLine( "mod.markColumnValuesUnchanged();" );
		writer.WriteLine( "return mod;" );
		writer.WriteLine( "}" );
	}

	private static void writeFieldsAndPropertiesForColumn( Column column ) {
		var columnIsReadOnly = !columns.DataColumns.Contains( column );

		writer.WriteLine(
			"private readonly DataValue<" + column.DataTypeName + "> " + getColumnFieldName( column ) + " = new DataValue<" + column.DataTypeName + ">();" );
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Gets " + ( columnIsReadOnly ? "" : "or sets " ) + "the value for the " + column.Name +
			" column. Throws an exception if the value has not been initialized. " + getComment( column ) );
		var propertyDeclarationBeginning = "public " + column.DataTypeName + " " + EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) +
		                                   " { get { return " + getColumnFieldName( column ) + ".Value; } ";
		if( columnIsReadOnly )
			writer.WriteLine( propertyDeclarationBeginning + "}" );
		else {
			writer.WriteLine( propertyDeclarationBeginning + "set { " + getColumnFieldName( column ) + ".Value = value; } }" );

			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Indicates whether or not the value for the " + column.Name +
				" has been set since object creation or the last call to Execute, whichever was latest." );
			writer.WriteLine(
				"public bool " + EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle + "HasChanged" ) + " { get { return " +
				getColumnFieldName( column ) + ".Changed; } }" );
		}
	}

	private static void writeSetAllDataMethod() {
		// header
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
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
		return column.AllowsNull && !column.NullValueExpression.Any() ? "Object allows null." : "Object does not allow null.";
	}

	private static void writeColumnParameterDeclarations( IEnumerable<Column> columns ) {
		writer.Write(
			StringTools.ConcatenateWithDelimiter(
				", ",
				columns.Select( i => i.DataTypeName + " " + EwlStatics.GetCSharpIdentifier( i.CamelCasedName ) ).ToArray() ) );
	}

	private static void writeColumnValueAssignmentsFromParameters( IEnumerable<Column> columns, string modObjectName ) {
		foreach( var column in columns )
			writer.WriteLine( modObjectName + "." + getColumnFieldName( column ) + ".Value = " + EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) + ";" );
	}

	private static void writeExecuteMethod( string tableName ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Executes this " + tableName +
			" modification, persisting all changes. Executes any pre-insert, pre-update, post-insert, or post-update logic that may exist in the class." );
		writer.WriteLine( "public void Execute( bool isLongRunning = false ) {" );
		writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( delegate {" );

		// The mod type may change during execute.
		writer.WriteLine( "var frozenModType = modType;" );

		writer.WriteLine( "if( frozenModType == ModificationType.Insert )" );
		writer.WriteLine( "preInsert();" );
		writer.WriteLine( "else if( frozenModType == ModificationType.Update )" );
		writer.WriteLine( "preUpdate();" );

		writer.WriteLine( "executeInsertOrUpdate( isLongRunning );" );

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
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Executes this " + tableName +
			" modification, persisting all changes. Does not execute pre-insert, pre-update, post-insert, or post-update logic that may exist in the class." );
		writer.WriteLine( "public void ExecuteWithoutAdditionalLogic( bool isLongRunning = false ) {" );
		writer.WriteLine( "executeInsertOrUpdate( isLongRunning );" );
		writer.WriteLine( "markColumnValuesUnchanged();" );
		writer.WriteLine( "}" );
	}

	private static void writeExecuteInsertOrUpdateMethod(
		DatabaseConnection cn, string tableName, Column? identityColumn, bool hasModTable, bool isRevisionHistoryClass ) {
		writer.WriteLine( "private void executeInsertOrUpdate( bool isLongRunning ) {" );
		if( hasModTable || isRevisionHistoryClass )
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );
		writer.WriteLine( "try {" );


		// insert

		writer.WriteLine( "if( modType == ModificationType.Insert ) {" );

		// If this is a revision history table, write code to insert a new revision when a row is inserted into this table.
		if( isRevisionHistoryClass ) {
			writer.WriteLine( "var revisionHistorySetup = RevisionHistoryStatics.SystemProvider;" );
			writer.WriteLine( getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn! ) + ".Value = revisionHistorySetup.GetNextMainSequenceValue();" );
			writer.WriteLine(
				"revisionHistorySetup.InsertRevision( System.Convert.ToInt32( " + getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn! ) +
				".Value ), System.Convert.ToInt32( " + getColumnFieldName( columns.PrimaryKeyAndRevisionIdColumn! ) + ".Value ), " +
				DataAccessStatics.GetConnectionExpression( database ) + ".GetUserTransactionId() );" );
		}

		writer.WriteLine( "var insert = new InlineInsert( \"" + tableName + "\" );" );
		writer.WriteLine( "insert.AddColumnModifications( getColumnModificationValues() );" );
		if( identityColumn != null )
			// One reason the ChangeType call is necessary: SQL Server identities always come back as decimal, and you can't cast a boxed decimal to an int.
			writer.WriteLine(
				"{0}.Value = {1};".FormatWith(
					getColumnFieldName( identityColumn ),
					identityColumn.GetIncomingValueConversionExpression(
						"EwlStatics.ChangeType( insert.Execute( {0}, isLongRunning: isLongRunning ), typeof( {1} ) )".FormatWith(
							DataAccessStatics.GetConnectionExpression( database ),
							identityColumn.UnconvertedDataTypeName ) ) ) );
		else
			writer.WriteLine( "insert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );

		if( hasModTable ) {
			writer.WriteLine( "var modTableInsert = new InlineInsert( \"{0}\" );".FormatWith( tableName + DatabaseOps.GetModificationTableSuffix( database ) ) );
			writer.WriteLine(
				"modTableInsert.AddColumnModifications( new[] {{ {0} }} );".FormatWith(
					StringTools.ConcatenateWithDelimiter(
						", ",
						columns.KeyColumns.Select( i => i.GetCommandColumnValueExpression( EwlStatics.GetCSharpIdentifier( i.PascalCasedNameExceptForOracle ) ) ) ) ) );
			writer.WriteLine( "modTableInsert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		}

		if( columns.HasKeyColumns ) {
			// Future calls to Execute should perform updates, not inserts. Use the values of key columns as conditions.
			writer.WriteLine( "modType = ModificationType.Update;" );
			writer.WriteLine( "conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) + ">();" );
			foreach( var column in columns.KeyColumns )
				writer.WriteLine(
					"conditions.Add( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, tableName, column ) + "( " +
					EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) + " ) );" );
		}

		writer.WriteLine( "}" ); // if insert


		// update

		writer.WriteLine( "else {" );
		writer.WriteLine( "var modificationValues = getColumnModificationValues();" );
		writer.WriteLine( "if( modificationValues.Any() ) {" );

		if( isRevisionHistoryClass )
			writer.WriteLine( "copyLatestRevisions( conditions!, isLongRunning );" );

		if( hasModTable ) {
			writer.WriteLine(
				"var modTableInsert = new InlineInsertWithSelect( \"{0}\", new[] {{ {1} }}, \"{2}\" );".FormatWith(
					tableName + DatabaseOps.GetModificationTableSuffix( database ),
					StringTools.ConcatenateWithDelimiter( ", ", columns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ) ),
					tableName ) );
			foreach( var i in columns.KeyColumns )
				writer.WriteLine( "modTableInsert.AddSelectExpression( \"{0}\" );".FormatWith( i.DelimitedIdentifier.EscapeForLiteral() ) );
			writer.WriteLine( "modTableInsert.AddConditions( conditions!.Select( i => i.CommandCondition ) );" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "modTableInsert.AddConditions( getLatestRevisionsCondition().ToCollection() );" );
			writer.WriteLine( "modTableInsert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );

			// If any primary-key columns are changing, insert the new key(s) into the modification table.
			var nonIdentityKeyColumns = columns.KeyColumns.Where( i => !i.IsIdentity ).Materialize();
			if( nonIdentityKeyColumns.Any() ) {
				writer.WriteLine(
					"if( {0} ) {{".FormatWith(
						StringTools.ConcatenateWithDelimiter( " || ", nonIdentityKeyColumns.Select( i => "{0}.Changed".FormatWith( getColumnFieldName( i ) ) ) ) ) );
				writer.WriteLine(
					"var modTableNewKeyInsert = new InlineInsertWithSelect( \"{0}\", new[] {{ {1} }}, \"{2}\" );".FormatWith(
						tableName + DatabaseOps.GetModificationTableSuffix( database ),
						StringTools.ConcatenateWithDelimiter( ", ", columns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ) ),
						tableName ) );
				foreach( var column in columns.KeyColumns )
					if( column.IsIdentity )
						writer.WriteLine( "modTableNewKeyInsert.AddSelectExpression( \"{0}\" );".FormatWith( column.DelimitedIdentifier.EscapeForLiteral() ) );
					else {
						writer.WriteLine(
							"if( {0}.Changed ) modTableNewKeyInsert.AddSelectValue( {1} );".FormatWith(
								getColumnFieldName( column ),
								column.GetCommandParameterValueExpression( EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) ) ) );
						writer.WriteLine( "else modTableNewKeyInsert.AddSelectExpression( \"{0}\" );".FormatWith( column.DelimitedIdentifier.EscapeForLiteral() ) );
					}
				writer.WriteLine( "modTableNewKeyInsert.AddConditions( conditions!.Select( i => i.CommandCondition ) );" );
				if( isRevisionHistoryClass )
					writer.WriteLine( "modTableNewKeyInsert.AddConditions( getLatestRevisionsCondition().ToCollection() );" );
				writer.WriteLine(
					"modTableNewKeyInsert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
				writer.WriteLine( "}" );
			}
		}

		writer.WriteLine( "var update = new InlineUpdate( \"" + tableName + "\" );" );
		writer.WriteLine( "update.AddColumnModifications( modificationValues );" );
		writer.WriteLine( "update.AddConditions( conditions!.Select( i => i.CommandCondition ) );" );
		if( isRevisionHistoryClass )
			writer.WriteLine( "update.AddConditions( getLatestRevisionsCondition().ToCollection() );" );
		writer.WriteLine( "update.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );

		writer.WriteLine( "}" ); // if modificationValues
		writer.WriteLine( "}" ); // else


		writer.WriteLine( "}" ); // try

		writer.WriteLine( "catch( System.Exception e ) {" );
		writer.WriteLine( "rethrowAsDataModificationExceptionIfNecessary( e );" );
		writer.WriteLine( "throw;" );
		writer.WriteLine( "}" ); // catch

		if( hasModTable || isRevisionHistoryClass )
			writer.WriteLine( "} );" ); // ExecuteInTransaction
		writer.WriteLine( "}" ); // method
	}

	private static void writeGetColumnModificationValuesMethod( IEnumerable<Column> nonIdentityColumns ) {
		writer.WriteLine( "private IReadOnlyCollection<InlineDbCommandColumnValue> getColumnModificationValues() {" );
		writer.WriteLine( "var values = new List<InlineDbCommandColumnValue>();" );
		foreach( var column in nonIdentityColumns ) {
			writer.WriteLine( "if( " + getColumnFieldName( column ) + ".Changed )" );
			writer.WriteLine(
				"values.Add( {0} );".FormatWith( column.GetCommandColumnValueExpression( EwlStatics.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle ) ) ) );
		}
		writer.WriteLine( "return values;" );
		writer.WriteLine( "}" );
	}

	private static void writeCopyLatestRevisionsMethod( DatabaseConnection cn, string tableName, IEnumerable<Column> nonIdentityColumns, bool hasModTable ) {
		writer.WriteLine(
			"private static void copyLatestRevisions( List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, tableName ) +
			"> conditions, bool isLongRunning ) {" );

		writer.WriteLine( "var revisionHistorySetup = RevisionHistoryStatics.SystemProvider;" );

		writer.WriteLine(
			"var command = new InlineSelect( \"" + columns.PrimaryKeyAndRevisionIdColumn!.DelimitedIdentifier.EscapeForLiteral() + "\".ToCollection(), \"FROM " +
			tableName + "\", false );" );
		writer.WriteLine( "command.AddConditions( conditions.Select( i => i.CommandCondition ) );" );
		writer.WriteLine( "command.AddConditions( getLatestRevisionsCondition().ToCollection() );" );
		writer.WriteLine( "var latestRevisionIds = new List<int>();" );
		writer.WriteLine(
			"command.Execute( {0}, r => {{ while( r.Read() ) latestRevisionIds.Add( System.Convert.ToInt32( r[0] ) ); }}, isLongRunning: isLongRunning );".FormatWith(
				DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine( "foreach( var latestRevisionId in latestRevisionIds ) {" );

		// Get the latest revision.
		writer.WriteLine( "var latestRevision = revisionHistorySetup.GetRevision( latestRevisionId );" );

		// If this condition is true, we've already modified the row in this transaction. If we were to copy it, we'd end up with two revisions of the same entity
		// in the same user transaction, which we don't support.
		writer.WriteLine( "if( latestRevision.UserTransactionId == " + DataAccessStatics.GetConnectionExpression( database ) + ".GetUserTransactionId() )" );
		writer.WriteLine( "continue;" );

		// Update the latest revision with a new user transaction.
		writer.WriteLine(
			"revisionHistorySetup.UpdateRevision( latestRevisionId, latestRevisionId, " + DataAccessStatics.GetConnectionExpression( database ) +
			".GetUserTransactionId(), latestRevisionId );" );

		// Insert a copy of the latest revision with a new ID. This will represent the revision of the data before it was changed.
		writer.WriteLine( "var copiedRevisionId = revisionHistorySetup.GetNextMainSequenceValue();" );
		writer.WriteLine( "revisionHistorySetup.InsertRevision( copiedRevisionId, latestRevisionId, latestRevision.UserTransactionId );" );

		// Insert a copy of the data row and make it correspond to the copy of the latest revision.
		writer.WriteLine(
			"var copyCommand = new InlineInsertWithSelect( \"{0}\", new[] {{ {1} }}, \"{0}\" );".FormatWith(
				tableName,
				StringTools.ConcatenateWithDelimiter( ", ", nonIdentityColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ) ) ) );
		foreach( var column in nonIdentityColumns )
			writer.WriteLine(
				column == columns.PrimaryKeyAndRevisionIdColumn
					? "copyCommand.AddSelectValue( {0} );".FormatWith( column.GetCommandParameterValueExpression( "copiedRevisionId" ) )
					: "copyCommand.AddSelectExpression( \"{0}\" );".FormatWith( column.DelimitedIdentifier.EscapeForLiteral() ) );
		writer.WriteLine(
			"copyCommand.AddConditions( new EqualityCondition( new InlineDbCommandColumnValue( \"{0}\", new DbParameterValue( latestRevisionId ) ) ).ToCollection() );"
				.FormatWith( columns.PrimaryKeyAndRevisionIdColumn.Name ) );
		writer.WriteLine( "copyCommand.Execute( {0} );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );

		if( hasModTable ) {
			writer.WriteLine( "var modTableInsert = new InlineInsert( \"{0}\" );".FormatWith( tableName + DatabaseOps.GetModificationTableSuffix( database ) ) );
			writer.WriteLine(
				"modTableInsert.AddColumnModifications( {0}.ToCollection() );".FormatWith(
					columns.PrimaryKeyAndRevisionIdColumn.GetCommandColumnValueExpression( "copiedRevisionId" ) ) );
			writer.WriteLine( "modTableInsert.Execute( {0} );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		}

		writer.WriteLine( "}" ); // foreach
		writer.WriteLine( "}" ); // method
	}

	private static void writeRethrowAsEwfExceptionIfNecessary() {
		writer.WriteLine( "private static void rethrowAsDataModificationExceptionIfNecessary( System.Exception e ) {" );
		writer.WriteLine( "var constraintNamesToViolationErrorMessages = new Dictionary<string,string>();" );
		writer.WriteLine( "populateConstraintNamesToViolationErrorMessages( constraintNamesToViolationErrorMessages );" );
		writer.WriteLine( "foreach( var pair in constraintNamesToViolationErrorMessages )" );
		writer.WriteLine( "if( e.GetBaseException().Message.ToLower().Contains( pair.Key.ToLower() ) ) throw new DataModificationException( pair.Value );" );
		writer.WriteLine( "}" ); // method
	}

	private static void writeMarkColumnValuesUnchangedMethod() {
		writer.WriteLine( "private void markColumnValuesUnchanged() {" );
		foreach( var column in columns.AllColumnsExceptRowVersion )
			writer.WriteLine( getColumnFieldName( column ) + ".ClearChanged();" );
		writer.WriteLine( "}" );
	}

	private static string getColumnFieldName( Column column ) => EwlStatics.GetCSharpIdentifier( column.CamelCasedName + "ColumnValue" );

	internal static string GetClassName( DatabaseConnection cn, string table, bool isRevisionHistoryTable, bool isRevisionHistoryClass ) =>
		EwlStatics.GetCSharpIdentifier(
			isRevisionHistoryTable && !isRevisionHistoryClass
				? "Direct" + table.TableNameToPascal( cn ) + "ModificationWithRevisionBypass"
				: table.TableNameToPascal( cn ) + "Modification" );
}