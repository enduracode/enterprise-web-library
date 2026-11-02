using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;

static class QueryRetrievalStatics {
	private sealed record QueryData(
		string QueryName,
		string SelectFromClause,
		IReadOnlyCollection<Column> Columns,
		IReadOnlyCollection<( EnterpriseWebLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause clause, IReadOnlyCollection<ValueContainer> parameters
			)> PostSelectFromClauses );

	internal static void Generate(
		DatabaseConnection cn, TextWriter writer, string baseNamespace, Database database,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		if( configuration.queries == null )
			return;

		writer.WriteLine( "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "Retrieval {" );

		foreach( var query in configuration.queries ) {
			QueryData data;
			try {
				data = getQueryData( cn, database, query );
			}
			catch( Exception e ) {
				throw new UserCorrectableException( "Failed to get columns and parameters for the " + query.name + " query.", e );
			}

			CodeGenerationStatics.AddSummaryDocComment( writer, "This object holds the values returned from a " + data.QueryName + " query." );
			var className = "{0}Retrieval".FormatWith( data.QueryName );
			writer.WriteLine( "public static partial class " + className + " {" );

			// Write nested classes.
			RetrievalStatics.WriteRowClasses( writer, data.Columns, null, _ => {}, _ => {} );
			writeCacheClass( writer, database, data );

			writer.WriteLine(
				$"""private const string selectFromClause = "{AppStatics.NormalizeLineEndingsFromXml( data.SelectFromClause ).EscapeForLiteral()} ";""" );
			foreach( var postSelectFromClause in data.PostSelectFromClauses )
				writeQueryMethod( writer, database, postSelectFromClause.clause, postSelectFromClause.parameters );
			writer.WriteLine( "static partial void updateSingleRowCaches( Row row );" );

			DataAccessStatics.WriteRevisionDeltaExtensionMethods( writer, className, data.Columns.Where( i => !i.IsRowVersion ) );

			writer.WriteLine( "}" ); // class
		}
		writer.WriteLine( "}" ); // namespace
	}

	private static QueryData getQueryData( DatabaseConnection cn, Database database, EnterpriseWebLibrary.Configuration.SystemDevelopment.Query query ) {
		var columns = Column.GetColumnsInQueryResults( cn, database, query.selectFromClause, false );

		// Attempt to query with every postSelectFromClause to ensure validity.
		var clauses = query.postSelectFromClauses.Select( clause => {
				var parameters = DataAccessStatics.ExecuteWithCommandFromRawQueryText(
					cn,
					database,
					query.selectFromClause + clause.ValueNonNullable.PrependDelimiter( " " ),
					command => cn.ExecuteReaderCommandWithSchemaOnlyBehavior( command, _ => {} ) );
				return ( clause, parameters );
			} )
			.Materialize();

		return new QueryData( query.name, query.selectFromClause, columns, clauses );
	}

	private static void writeCacheClass( TextWriter writer, Database database, QueryData data ) {
		writer.WriteLine( "private partial class Cache {" );
		writer.WriteLine(
			"internal static Cache Current { get { return DataAccessState.Current.GetCacheValue( \"" + database.SecondaryDatabaseName + data.QueryName +
			"QueryRetrieval\", () => new Cache() ); } }" );
		foreach( var i in data.PostSelectFromClauses ) {
			var type = getQueryCacheType( i.parameters );
			writer.WriteLine( "internal readonly " + type + " " + getQueryCacheName( i.clause, i.parameters ) + " = new " + type + "();" );
		}
		writer.WriteLine( "private Cache() {}" );
		writer.WriteLine( "}" );
	}

	private static string getQueryCacheType( IReadOnlyCollection<ValueContainer> parameters ) =>
		parameters.Any() ? "QueryRetrievalQueryCache<Row>" : "ParameterlessQueryCache<Row>";

	private static void writeQueryMethod(
		TextWriter writer, Database database, EnterpriseWebLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause,
		IReadOnlyCollection<ValueContainer> parameters ) {
		// header
		CodeGenerationStatics.AddSummaryDocComment( writer, "Queries the database and returns the full results collection immediately." );
		writer.WriteLine( "public static IEnumerable<Row> GetRows" + postSelectFromClause.name + "( " + DataAccessStatics.GetMethodParams( parameters ) + " ) {" );


		// body

		var getResultSetFirstArg =
			parameters.Any() ? "new object?[] { " + StringTools.ConcatenateWithDelimiter( ", ", parameters.Select( i => i.Name ) ) + " }, " : "";
		writer.WriteLine( "return Cache.Current." + getQueryCacheName( postSelectFromClause, parameters ) + ".GetResultSet( " + getResultSetFirstArg + "() => {" );

		writer.WriteLine( "var cmd = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
		writer.WriteLine(
			$"""cmd.CommandText = selectFromClause + "{AppStatics.NormalizeLineEndingsFromXml( postSelectFromClause.ValueNonNullable ).EscapeForLiteral()}";""" );
		DataAccessStatics.WriteAddParamBlock( writer, "cmd", parameters, database );
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
		EnterpriseWebLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause, IReadOnlyCollection<ValueContainer> parameters ) =>
		"Rows" + postSelectFromClause.name + ( parameters.Any() ? "Queries" : "Query" );
}