using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems;

internal static class QueryRetrievalStatics {
	private static DatabaseInfo info = null!;

	internal static void Generate(
		DBConnection cn, TextWriter writer, string baseNamespace, Database database, EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		if( configuration.queries == null )
			return;

		info = cn.DatabaseInfo;
		writer.WriteLine( "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "Retrieval {" );

		foreach( var query in configuration.queries ) {
			List<Column> columns;
			try {
				columns = validateQueryAndGetColumns( cn, query );
			}
			catch( Exception e ) {
				throw new UserCorrectableException( "Column retrieval failed for the " + query.name + " query.", e );
			}

			CodeGenerationStatics.AddSummaryDocComment( writer, "This object holds the values returned from a " + query.name + " query." );
			var className = "{0}Retrieval".FormatWith( query.name );
			writer.WriteLine( "public static partial class " + className + " {" );

			// Write nested classes.
			DataAccessStatics.WriteRowClasses( writer, columns, _ => {}, _ => {} );
			writeCacheClass( writer, database, query );

			writer.WriteLine( "private const string selectFromClause = @\"" + AppStatics.NormalizeLineEndingsFromXml( query.selectFromClause ) + " \";" );
			foreach( var postSelectFromClause in query.postSelectFromClauses )
				writeQueryMethod( writer, database, query, postSelectFromClause );
			writer.WriteLine( "static partial void updateSingleRowCaches( Row row );" );

			DataAccessStatics.WriteRevisionDeltaExtensionMethods( writer, className, columns.Where( i => !i.IsRowVersion ) );

			writer.WriteLine( "}" ); // class
		}
		writer.WriteLine( "}" ); // namespace
	}

	private static List<Column> validateQueryAndGetColumns( DBConnection cn, EnterpriseWebLibrary.Configuration.SystemDevelopment.Query query ) {
		// Attempt to query with every postSelectFromClause to ensure validity.
		foreach( var postSelectFromClause in query.postSelectFromClauses )
			cn.ExecuteReaderCommandWithSchemaOnlyBehavior(
				DataAccessStatics.GetCommandFromRawQueryText( cn, query.selectFromClause + postSelectFromClause.ValueNonNullable.PrependDelimiter( " " ) ),
				_ => {} );

		return Column.GetColumnsInQueryResults( cn, query.selectFromClause, false, false );
	}

	private static void writeCacheClass( TextWriter writer, Database database, EnterpriseWebLibrary.Configuration.SystemDevelopment.Query query ) {
		writer.WriteLine( "private partial class Cache {" );
		writer.WriteLine(
			"internal static Cache Current { get { return DataAccessState.Current.GetCacheValue( \"" + database.SecondaryDatabaseName + query.name +
			"QueryRetrieval\", () => new Cache() ); } }" );
		foreach( var i in query.postSelectFromClauses ) {
			var type = getQueryCacheType( query, i );
			writer.WriteLine( "internal readonly " + type + " " + getQueryCacheName( query, i ) + " = new " + type + "();" );
		}
		writer.WriteLine( "private Cache() {}" );
		writer.WriteLine( "}" );
	}

	private static string getQueryCacheType(
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Query query,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause ) {
		return DataAccessStatics.GetNamedParamList( info, query.selectFromClause + postSelectFromClause.ValueNonNullable.PrependDelimiter( " " ) ).Any()
			       ? "QueryRetrievalQueryCache<Row>"
			       : "ParameterlessQueryCache<Row>";
	}

	private static void writeQueryMethod(
		TextWriter writer, Database database, EnterpriseWebLibrary.Configuration.SystemDevelopment.Query query,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment( writer, "Queries the database and returns the full results collection immediately." );
		writer.WriteLine(
			"public static IEnumerable<Row> GetRows" + postSelectFromClause.name + "( " + DataAccessStatics.GetMethodParamsFromCommandText(
				info,
				query.selectFromClause + postSelectFromClause.ValueNonNullable.PrependDelimiter( " " ) ) + " ) {" );


		// body

		var namedParamList = DataAccessStatics.GetNamedParamList( info, query.selectFromClause + postSelectFromClause.ValueNonNullable.PrependDelimiter( " " ) );
		var getResultSetFirstArg = namedParamList.Any() ? "new[] { " + StringTools.ConcatenateWithDelimiter( ", ", namedParamList.ToArray() ) + " }, " : "";
		writer.WriteLine( "return Cache.Current." + getQueryCacheName( query, postSelectFromClause ) + ".GetResultSet( " + getResultSetFirstArg + "() => {" );

		writer.WriteLine( "var cmd = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
		writer.WriteLine( "cmd.CommandText = selectFromClause + @\"" + AppStatics.NormalizeLineEndingsFromXml( postSelectFromClause.ValueNonNullable ) + "\";" );
		DataAccessStatics.WriteAddParamBlockFromCommandText(
			writer,
			"cmd",
			info,
			query.selectFromClause + postSelectFromClause.ValueNonNullable.PrependDelimiter( " " ),
			database );
		writer.WriteLine( "var results = new List<Row>();" );
		writer.WriteLine(
			DataAccessStatics.GetConnectionExpression( database ) +
			".ExecuteReaderCommand( cmd, r => { while( r.Read() ) results.Add( new Row( new BasicRow( r ) ) ); } );" );

		// Update single-row caches.
		writer.WriteLine( "foreach( var i in results )" );
		writer.WriteLine( "updateSingleRowCaches( i );" );

		writer.WriteLine( "return results;" );

		writer.WriteLine( "} );" );
		writer.WriteLine( "}" );
	}

	private static string getQueryCacheName(
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Query query,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause ) {
		return "Rows" + postSelectFromClause.name +
		       ( DataAccessStatics.GetNamedParamList( info, query.selectFromClause + postSelectFromClause.ValueNonNullable.PrependDelimiter( " " ) ).Any()
			         ? "Queries"
			         : "Query" );
	}
}