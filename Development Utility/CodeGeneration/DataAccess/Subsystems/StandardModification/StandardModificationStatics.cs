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
		IEnumerable<( DatabaseTable tableName, bool hasModTable, bool isRevisionHistoryTable )> tables,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		StandardModificationStatics.writer = writer;
		StandardModificationStatics.database = database;

		var subsystemName = "{0}Modification".FormatWith( database.SecondaryDatabaseName );

		foreach( var filePath in IoMethods.GetFilePathsInFolder(
			        EwlStatics.CombinePaths( templateBasePath, subsystemName ),
			        searchPattern: "*" + DataAccessStatics.CSharpTemplateFileExtension,
			        searchOption: SearchOption.AllDirectories ) )
			IoMethods.DeleteFile( filePath );

		foreach( var table in tables ) {
			var subsystemNamespace = $"namespace {baseNamespace}.{subsystemName}{DataAccessStatics.GetSchemaNamespaceSuffix( database, table.tableName )}";
			writer.WriteLine( $$"""{{subsystemNamespace}} {""" );

			writeClass( cn, table.tableName, table.isRevisionHistoryTable, table.hasModTable, false );
			if( table.isRevisionHistoryTable )
				writeClass( cn, table.tableName, true, table.hasModTable, true );

			writer.WriteLine( "}" );

			// We do not create templates for direct modification classes.
			var templateClassName = GetClassName(
				cn,
				table.tableName.Name,
				table.isRevisionHistoryTable,
				table.isRevisionHistoryTable,
				omitAtSignPrefixIfNotRequired: true );

			var templateFilePath = EwlStatics.CombinePaths(
				templateBasePath,
				subsystemName,
				DataAccessStatics.GetSchemaFolderName( database, table.tableName ),
				templateClassName );

			// If a real file exists, don’t create a template.
			if( File.Exists( templateFilePath + ".cs" ) )
				continue;

			using var templateWriter = IoMethods.GetTextWriterForWrite( templateFilePath + DataAccessStatics.CSharpTemplateFileExtension, true );
			templateWriter.WriteLine( "{0};".FormatWith( subsystemNamespace ) );
			templateWriter.WriteLine();
			templateWriter.WriteLine( "partial class {0} {{".FormatWith( templateClassName ) );
			templateWriter.WriteLine(
				"	// IMPORTANT: Change extension from \"{0}\" to \".cs\" before editing.".FormatWith( DataAccessStatics.CSharpTemplateFileExtension ) );
			templateWriter.WriteLine( "}" );
		}
	}

	private static void writeClass( DatabaseConnection cn, DatabaseTable table, bool isRevisionHistoryTable, bool hasModTable, bool isRevisionHistoryClass ) {
		columns = new TableColumns( cn, table, isRevisionHistoryClass );

		writer.WriteLine( "public partial class " + GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass ) + " {" );

		var revisionHistorySuffix = GetRevisionHistorySuffix( isRevisionHistoryClass );

		writeInsertRowMethod( table, revisionHistorySuffix, "" );
		writeInsertRowMethod( table, revisionHistorySuffix, "WithoutAdditionalLogic" );
		writeUpdateRowsMethod( cn, table, revisionHistorySuffix, "", false );
		writeUpdateRowsMethod( cn, table, revisionHistorySuffix, "", true );
		writeUpdateRowsMethod( cn, table, revisionHistorySuffix, "WithoutAdditionalLogic", false );
		writeUpdateRowsMethod( cn, table, revisionHistorySuffix, "WithoutAdditionalLogic", true );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "", false, false );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "", false, true );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "", true, false );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "", true, true );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "WithoutAdditionalLogic", false, false );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "WithoutAdditionalLogic", false, true );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "WithoutAdditionalLogic", true, false );
		writeDeleteRowsMethod( cn, table, revisionHistorySuffix, "WithoutAdditionalLogic", true, true );
		writePrivateDeleteRowsMethod( cn, table, hasModTable, isRevisionHistoryClass );
		writer.WriteLine(
			"static partial void preDelete( List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + "> conditions, " +
			getPostDeleteExecutorClassName() + " postDeleteExecutor );" );

		writeCreateForInsertMethod( cn, table, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
		writeCreateForUpdateMethod( cn, table, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );
		writeGetConditionListMethod( cn, table );
		if( columns.HasKeyColumns && columns.DataColumns.Any() )
			writeCreateForSingleRowUpdateMethod( cn, table, isRevisionHistoryTable, isRevisionHistoryClass, revisionHistorySuffix );

		writer.WriteLine( "private ModificationType modType;" );
		writer.WriteLine( $"private List<{DataAccessStatics.GetTableConditionInterfaceName( cn, database, table )}>? conditions;" );
		foreach( var column in columns.AllColumnsExceptRowVersion ) {
			CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
			writer.WriteLine( $"private readonly DataValue<{column.DataTypeName}> {getColumnFieldName( column )};" );
		}

		writer.WriteLine( $"private {GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass )}( ModificationType modType ) {{" );
		writer.WriteLine( "this.modType = modType;" );
		foreach( var column in columns.AllColumnsExceptRowVersion )
			writer.WriteLine( $"{getColumnFieldName( column )} = new DataValue<{column.DataTypeName}>( modType == ModificationType.Update );" );
		writer.WriteLine( "}" );

		foreach( var column in columns.AllColumnsExceptRowVersion )
			writePropertiesForColumn( column );

		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Gets whether the value for any column has been set since object creation or the last call to Execute, whichever was latest." );
		writer.WriteLine(
			"public bool AnyColumnValueHasChanged => {0};".FormatWith(
				StringTools.ConcatenateWithDelimiter(
					" || ",
					columns.AllColumnsExceptRowVersion.Select( i => "{0}.HasChanged".FormatWith( getColumnFieldName( i ) ) ) ) ) );

		if( columns.DataColumns.Any() )
			writeSetAllDataMethod();

		foreach( var column in columns.DataColumns )
			new ModificationFormItemMethodWriter( column.GetModificationField() ).WriteFormItemGetters( writer );

		// Write execute methods and helpers.
		writeExecuteMethod( table.QualifiedName );
		writer.WriteLine( "partial void preInsert();" );
		writer.WriteLine( "partial void preUpdate();" );
		writeExecuteWithoutAdditionalLogicMethod( table.QualifiedName );
		writeExecuteInsertOrUpdateMethod( cn, table, columns.IdentityColumn, hasModTable, isRevisionHistoryClass );
		writeGetColumnModificationValuesMethod( columns.AllNonIdentityColumnsExceptRowVersion );
		if( isRevisionHistoryClass ) {
			writeCopyLatestRevisionsMethod( cn, table, columns.AllNonIdentityColumnsExceptRowVersion, hasModTable );
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

	private static void writeInsertRowMethod( DatabaseTable table, string revisionHistorySuffix, string additionalLogicSuffix ) {
		Column? returnColumn = null;
		var returnComment = "";
		if( columns.HasKeyColumns && columns.KeyColumns.Count == 1 && !columns.DataColumns.Contains( columns.KeyColumns.Single() ) ) {
			returnColumn = columns.KeyColumns.Single();
			returnComment = " Returns the value of the " + returnColumn.Name + " column.";
		}

		// header
		CodeGenerationStatics.AddSummaryDocComment( writer, "Inserts a row into the " + table.QualifiedName + " table." + returnComment );
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
			writer.WriteLine( "return mod." + returnColumn.PascalCasedName + ";" );
		writer.WriteLine( "}" );
	}

	private static void writeUpdateRowsMethod(
		DatabaseConnection cn, DatabaseTable table, string revisionHistorySuffix, string additionalLogicSuffix, bool includeIsLongRunningParameter ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Updates rows in the " + table.QualifiedName + " table that match the specified conditions with the specified data." );
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
					getConditionParameterDeclarations( cn, table ) ) ) );

		// body
		writer.WriteLine( "var mod = CreateForUpdate" + revisionHistorySuffix + "( requiredCondition, additionalConditions );" );
		writeColumnValueAssignmentsFromParameters( columns.DataColumns, "mod" );
		writer.WriteLine( "mod.Execute{0}( isLongRunning: {1} );".FormatWith( additionalLogicSuffix, includeIsLongRunningParameter ? "isLongRunning" : "false" ) );
		writer.WriteLine( "}" );
	}

	private static void writeDeleteRowsMethod(
		DatabaseConnection cn, DatabaseTable table, string revisionHistorySuffix, string additionalLogicSuffix, bool omitConditionParameters,
		bool includeIsLongRunningParameter ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			$"<para>Deletes {( omitConditionParameters ? "all rows" : "the rows that match the specified conditions" )} and returns the number of rows deleted.</para>" +
			"<para>WARNING: After calling this method, delete referenced rows in other tables that are no longer needed.</para>" );
		var parameters = StringTools.ConcatenateWithDelimiter(
			", ",
			includeIsLongRunningParameter ? "bool isLongRunning" : "",
			omitConditionParameters ? "" : getConditionParameterDeclarations( cn, table ) );
		writer.WriteLine(
			$$"""public static int Delete{{( omitConditionParameters ? "All" : "" )}}Rows{{revisionHistorySuffix + additionalLogicSuffix}}( {{parameters}} ) {""" );
		if( additionalLogicSuffix.Length is 0 )
			writer.WriteLine( "return " + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );

		var conditionsExpression = omitConditionParameters
			                           ? $"new List<{DataAccessStatics.GetTableConditionInterfaceName( cn, database, table )}>()"
			                           : "getConditionList( requiredCondition, additionalConditions )";
		writer.WriteLine( $"var conditions = {conditionsExpression};" );

		if( additionalLogicSuffix.Length is 0 ) {
			writer.WriteLine( $"var postDeleteExecutor = new {getPostDeleteExecutorClassName()}();" );
			writer.WriteLine( "preDelete( conditions, postDeleteExecutor );" );
		}

		writer.WriteLine( "var rowsDeleted = deleteRows( conditions, {0} );".FormatWith( includeIsLongRunningParameter ? "isLongRunning" : "false" ) );

		if( additionalLogicSuffix.Length is 0 )
			writer.WriteLine( "postDeleteExecutor.Execute();" );

		writer.WriteLine( "return rowsDeleted;" );

		if( additionalLogicSuffix.Length is 0 )
			writer.WriteLine( "} );" ); // cn.ExecuteInTransaction
		writer.WriteLine( "}" );
	}

	private static void writePrivateDeleteRowsMethod( DatabaseConnection cn, DatabaseTable table, bool hasModTable, bool isRevisionHistoryClass ) {
		// NOTE: For revision history tables, we should have the delete method automatically clean up the revisions table (but not user transactions) for us when doing direct-with-revision-bypass deletions.

		writer.WriteLine(
			"private static int deleteRows( List<{0}> conditions, bool isLongRunning ) {{".FormatWith(
				DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) ) );
		if( hasModTable || isRevisionHistoryClass )
			writer.WriteLine( "return " + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );

		if( isRevisionHistoryClass )
			writer.WriteLine( "copyLatestRevisions( conditions, isLongRunning );" );

		if( hasModTable ) {
			writer.WriteLine(
				"var modTableInsert = new InlineInsertWithSelect( \"{0}\", new[] {{ {1} }}, \"{2}\" );".FormatWith(
					DatabaseOps.GetModificationTableQualifiedName( database, table ),
					StringTools.ConcatenateWithDelimiter( ", ", columns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ) ),
					table.QualifiedName ) );
			foreach( var i in columns.KeyColumns )
				writer.WriteLine( "modTableInsert.AddSelectExpression( \"{0}\" );".FormatWith( i.DelimitedIdentifier.EscapeForLiteral() ) );
			writer.WriteLine( "modTableInsert.AddConditions( conditions.Select( i => i.CommandCondition ) );" );
			if( isRevisionHistoryClass )
				writer.WriteLine( "modTableInsert.AddConditions( getLatestRevisionsCondition().ToCollection() );" );
			writer.WriteLine( "modTableInsert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		}

		writer.WriteLine( "var delete = new InlineDelete( \"" + table.QualifiedName + "\" );" );
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

	private static string getPostDeleteExecutorClassName() => "PostDeleteExecutor";

	private static void writeCreateForInsertMethod(
		DatabaseConnection cn, DatabaseTable table, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Creates a modification object in insert mode, which can be used to do a piecemeal insert of a new row in the " + table.QualifiedName + " table." );
		writer.WriteLine(
			"public static " + GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForInsert" + methodNameSuffix + "() {" );
		writer.WriteLine( "return new " + GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass ) + "( ModificationType.Insert );" );
		writer.WriteLine( "}" );
	}

	private static void writeCreateForUpdateMethod(
		DatabaseConnection cn, DatabaseTable table, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Creates a modification object in update mode with the specified conditions, which can be used to do a piecemeal update of the " + table.QualifiedName +
			" table." );
		writer.WriteLine(
			"public static " + GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForUpdate" + methodNameSuffix + "( " +
			getConditionParameterDeclarations( cn, table ) + " ) {" );


		// body

		writer.WriteLine(
			"var mod = new " + GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass ) +
			"( ModificationType.Update ) { conditions = getConditionList( requiredCondition, additionalConditions ) };" );

		// Set column values that correspond to modification conditions to the values of those conditions. One reason this is important is so the primary
		// key can be retrieved in a consistent way regardless of whether the modification object is an insert or an update.
		writer.WriteLine( "foreach( var condition in mod.conditions ) {" );
		var prefix = "if";
		foreach( var column in columns.AllColumnsExceptRowVersion ) {
			writer.WriteLine(
				"{0}( condition is {1} {2} )".FormatWith(
					prefix,
					DataAccessStatics.GetEqualityConditionClassName( cn, database, table, column ),
					EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) ) );
			writer.WriteLine( "mod.{0}.Value = {1}.Value;".FormatWith( getColumnDataValueName( column ), EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) ) );
			prefix = "else if";
		}
		writer.WriteLine( "}" );
		writer.WriteLine( writer.NewLine + "mod.markColumnValuesUnchanged();" );

		writer.WriteLine( "return mod;" );
		writer.WriteLine( "}" );
	}

	private static void writeGetConditionListMethod( DatabaseConnection cn, DatabaseTable table ) {
		writer.WriteLine(
			"private static List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + "> getConditionList( " +
			getConditionParameterDeclarations( cn, table ) + " ) {" );
		writer.WriteLine( "var conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + ">();" );
		writer.WriteLine( "conditions.Add( requiredCondition );" );
		writer.WriteLine( "foreach( var condition in additionalConditions )" );
		writer.WriteLine( "conditions.Add( condition );" );
		writer.WriteLine( "return conditions;" );
		writer.WriteLine( "}" );
	}

	private static string getConditionParameterDeclarations( DatabaseConnection cn, DatabaseTable table ) =>
		"" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + " requiredCondition, params " +
		DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + "[] additionalConditions";

	private static void writeCreateForSingleRowUpdateMethod(
		DatabaseConnection cn, DatabaseTable table, bool isRevisionHistoryTable, bool isRevisionHistoryClass, string methodNameSuffix ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Creates a modification object in single-row update mode with the specified current data. All column values in this object will have HasChanged = false, despite being initialized. This object can then be used to do a piecemeal update of the " +
			table.QualifiedName + " table." );
		writer.Write(
			"public static " + GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass ) + " CreateForSingleRowUpdate" + methodNameSuffix +
			"( " );
		writeColumnParameterDeclarations( columns.AllColumnsExceptRowVersion );
		writer.WriteLine( " ) {" );


		// body

		writer.WriteLine( "var mod = new " + GetClassName( cn, table.Name, isRevisionHistoryTable, isRevisionHistoryClass ) + "( ModificationType.Update );" );

		// Use the values of key columns as conditions.
		writer.WriteLine( "mod.conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + ">();" );
		foreach( var column in columns.KeyColumns )
			writer.WriteLine(
				"mod.conditions.Add( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, table, column ) + "( " +
				EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) + " ) );" );

		writeColumnValueAssignmentsFromParameters( columns.AllColumnsExceptRowVersion, "mod" );
		writer.WriteLine( "mod.markColumnValuesUnchanged();" );
		writer.WriteLine( "return mod;" );
		writer.WriteLine( "}" );
	}

	private static void writePropertiesForColumn( Column column ) {
		var columnIsReadOnly = !columns.DataColumns.Contains( column );

		writer.WriteLine( $"private AbstractDataValue<{column.DataTypeName}> {getColumnDataValueName( column )} => {getColumnFieldName( column )};" );

		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Gets " + ( columnIsReadOnly ? "" : "or sets " ) + "the value for the " + column.Name +
			$" column, which {column.GetNullabilityPhrase()}. Throws an exception if the value has not been initialized." );
		var propertyDeclarationBeginning = "public " + column.DataTypeName + " " + EwlStatics.GetCSharpIdentifier( column.PascalCasedName ) +
		                                   " { get { return this." + getColumnDataValueName( column ) + ".Value; } ";
		if( columnIsReadOnly )
			writer.WriteLine( propertyDeclarationBeginning + "}" );
		else {
			writer.WriteLine( propertyDeclarationBeginning + "set { this." + getColumnDataValueName( column ) + ".Value = value; } }" );

			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Indicates whether or not the value for the " + column.Name +
				" has been set since object creation or the last call to Execute, whichever was latest." );
			writer.WriteLine(
				"public bool " + EwlStatics.GetCSharpIdentifier( column.PascalCasedName + "HasChanged" ) + " { get { return " + getColumnFieldName( column ) +
				".HasChanged; } }" );
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
			CodeGenerationStatics.AddParamDocComment(
				writer,
				column.CamelCasedName,
				$"The value for the {column.Name} column, which {column.GetNullabilityPhrase()}." );
	}

	private static void writeColumnParameterDeclarations( IEnumerable<Column> columns ) {
		writer.Write(
			StringTools.ConcatenateWithDelimiter(
				", ",
				columns.Select( i => i.DataTypeName + " " + EwlStatics.GetCSharpIdentifier( i.CamelCasedName ) ).ToArray() ) );
	}

	private static void writeColumnValueAssignmentsFromParameters( IEnumerable<Column> columns, string modObjectName ) {
		foreach( var column in columns )
			writer.WriteLine( $"{modObjectName}.{getColumnDataValueName( column )}.Value = {EwlStatics.GetCSharpIdentifier( column.CamelCasedName )};" );
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
		DatabaseConnection cn, DatabaseTable table, Column? identityColumn, bool hasModTable, bool isRevisionHistoryClass ) {
		writer.WriteLine( "private void executeInsertOrUpdate( bool isLongRunning ) {" );
		if( hasModTable || isRevisionHistoryClass )
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( () => {" );
		writer.WriteLine( "try {" );


		// insert

		writer.WriteLine( "if( modType == ModificationType.Insert ) {" );

		// If this is a revision history table, write code to insert a new revision when a row is inserted into this table.
		if( isRevisionHistoryClass ) {
			writer.WriteLine( "var revisionHistorySetup = RevisionHistoryStatics.SystemProvider;" );
			var revisionIdProperty = $"this.{getColumnDataValueName( columns.PrimaryKeyAndRevisionIdColumn! )}";
			writer.WriteLine( revisionIdProperty + ".Value = revisionHistorySetup.GetNextMainSequenceValue();" );
			writer.WriteLine(
				"revisionHistorySetup.InsertRevision( global::System.Convert.ToInt32( " + revisionIdProperty + ".Value ), global::System.Convert.ToInt32( " +
				revisionIdProperty + ".Value ), " + DataAccessStatics.GetConnectionExpression( database ) + ".GetUserTransactionId() );" );
		}

		writer.WriteLine( "var insert = new InlineInsert( \"" + table.QualifiedName + "\" );" );
		writer.WriteLine( "insert.AddColumnModifications( getColumnModificationValues() );" );
		if( identityColumn != null )
			// One reason the ChangeType call is necessary: SQL Server identities always come back as decimal, and you can’t cast a boxed decimal to an int.
			writer.WriteLine(
				"this.{0}.Value = {1};".FormatWith(
					getColumnDataValueName( identityColumn ),
					identityColumn.GetIncomingValueConversionExpression(
						"EwlStatics.ChangeType( insert.Execute( {0}, isLongRunning: isLongRunning )!, typeof( {1} ) )".FormatWith(
							DataAccessStatics.GetConnectionExpression( database ),
							identityColumn.UnconvertedDataTypeName ) ) ) );
		else
			writer.WriteLine( "insert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );

		if( hasModTable ) {
			writer.WriteLine( "var modTableInsert = new InlineInsert( \"{0}\" );".FormatWith( DatabaseOps.GetModificationTableQualifiedName( database, table ) ) );
			writer.WriteLine(
				"modTableInsert.AddColumnModifications( new[] {{ {0} }} );".FormatWith(
					StringTools.ConcatenateWithDelimiter(
						", ",
						columns.KeyColumns.Select( i => i.GetCommandColumnValueExpression( EwlStatics.GetCSharpIdentifier( i.PascalCasedName ) ) ) ) ) );
			writer.WriteLine( "modTableInsert.Execute( {0}, isLongRunning: isLongRunning );".FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		}

		if( columns.HasKeyColumns ) {
			// Future calls to Execute should perform updates, not inserts. Use the values of key columns as conditions.
			writer.WriteLine( "modType = ModificationType.Update;" );
			writer.WriteLine( "conditions = new List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) + ">();" );
			foreach( var column in columns.KeyColumns )
				writer.WriteLine(
					"conditions.Add( new " + DataAccessStatics.GetEqualityConditionClassName( cn, database, table, column ) + "( " +
					EwlStatics.GetCSharpIdentifier( column.PascalCasedName ) + " ) );" );
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
					DatabaseOps.GetModificationTableQualifiedName( database, table ),
					StringTools.ConcatenateWithDelimiter( ", ", columns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ) ),
					table.QualifiedName ) );
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
						StringTools.ConcatenateWithDelimiter( " || ", nonIdentityKeyColumns.Select( i => "{0}.HasChanged".FormatWith( getColumnFieldName( i ) ) ) ) ) );
				writer.WriteLine(
					"var modTableNewKeyInsert = new InlineInsertWithSelect( \"{0}\", new[] {{ {1} }}, \"{2}\" );".FormatWith(
						DatabaseOps.GetModificationTableQualifiedName( database, table ),
						StringTools.ConcatenateWithDelimiter( ", ", columns.KeyColumns.Select( i => "\"{0}\"".FormatWith( i.Name ) ) ),
						table.QualifiedName ) );
				foreach( var column in columns.KeyColumns )
					if( column.IsIdentity )
						writer.WriteLine( "modTableNewKeyInsert.AddSelectExpression( \"{0}\" );".FormatWith( column.DelimitedIdentifier.EscapeForLiteral() ) );
					else {
						writer.WriteLine(
							"if( {0}.HasChanged ) modTableNewKeyInsert.AddSelectValue( {1} );".FormatWith(
								getColumnFieldName( column ),
								column.GetCommandParameterValueExpression( EwlStatics.GetCSharpIdentifier( column.PascalCasedName ) ) ) );
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

		writer.WriteLine( "var update = new InlineUpdate( \"" + table.QualifiedName + "\" );" );
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

	private static string getColumnDataValueName( Column column ) => EwlStatics.GetCSharpIdentifier( column.CamelCasedName ) + "DataValue";

	private static void writeGetColumnModificationValuesMethod( IEnumerable<Column> nonIdentityColumns ) {
		writer.WriteLine( "private IReadOnlyCollection<InlineDbCommandColumnValue> getColumnModificationValues() {" );
		writer.WriteLine( "var values = new List<InlineDbCommandColumnValue>();" );
		foreach( var column in nonIdentityColumns ) {
			writer.WriteLine( "if( " + getColumnFieldName( column ) + ".HasChanged )" );
			writer.WriteLine( "values.Add( {0} );".FormatWith( column.GetCommandColumnValueExpression( EwlStatics.GetCSharpIdentifier( column.PascalCasedName ) ) ) );
		}
		writer.WriteLine( "return values;" );
		writer.WriteLine( "}" );
	}

	private static void writeCopyLatestRevisionsMethod( DatabaseConnection cn, DatabaseTable table, IEnumerable<Column> nonIdentityColumns, bool hasModTable ) {
		writer.WriteLine(
			"private static void copyLatestRevisions( List<" + DataAccessStatics.GetTableConditionInterfaceName( cn, database, table ) +
			"> conditions, bool isLongRunning ) {" );

		writer.WriteLine( "var revisionHistorySetup = RevisionHistoryStatics.SystemProvider;" );

		writer.WriteLine(
			"var command = new InlineSelect( \"" + columns.PrimaryKeyAndRevisionIdColumn!.DelimitedIdentifier.EscapeForLiteral() + "\".ToCollection(), \"FROM " +
			table.QualifiedName + "\", false );" );
		writer.WriteLine( "command.AddConditions( conditions.Select( i => i.CommandCondition ) );" );
		writer.WriteLine( "command.AddConditions( getLatestRevisionsCondition().ToCollection() );" );
		writer.WriteLine( "var latestRevisionIds = new List<int>();" );
		writer.WriteLine(
			"command.Execute( {0}, r => {{ while( r.Read() ) latestRevisionIds.Add( global::System.Convert.ToInt32( r[0] ) ); }}, isLongRunning: isLongRunning );"
				.FormatWith( DataAccessStatics.GetConnectionExpression( database ) ) );
		writer.WriteLine( "foreach( var latestRevisionId in latestRevisionIds ) {" );

		// Get the latest revision.
		writer.WriteLine( "var latestRevision = revisionHistorySetup.GetRevision( latestRevisionId );" );

		// If this condition is true, we’ve already modified the row in this transaction. If we were to copy it, we’d end up with two revisions of the same entity
		// in the same user transaction, which we don’t support.
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
				table.QualifiedName,
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
			writer.WriteLine( "var modTableInsert = new InlineInsert( \"{0}\" );".FormatWith( DatabaseOps.GetModificationTableQualifiedName( database, table ) ) );
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
			writer.WriteLine( getColumnFieldName( column ) + ".NotifyPersisted();" );
		writer.WriteLine( "}" );
	}

	private static string getColumnFieldName( Column column ) => EwlStatics.GetCSharpIdentifier( "__" + column.CamelCasedName );

	internal static string GetClassName(
		DatabaseConnection cn, string table, bool isRevisionHistoryTable, bool isRevisionHistoryClass, bool omitAtSignPrefixIfNotRequired = false ) =>
		EwlStatics.GetCSharpIdentifier(
			isRevisionHistoryTable && !isRevisionHistoryClass
				? "Direct" + table.TableNameToPascal( cn ) + "ModificationWithRevisionBypass"
				: table.TableNameToPascal( cn ) + "Modification",
			omitAtSignPrefixIfNotRequired: omitAtSignPrefixIfNotRequired );
}