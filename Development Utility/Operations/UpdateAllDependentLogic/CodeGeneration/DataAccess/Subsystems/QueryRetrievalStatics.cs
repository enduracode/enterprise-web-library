using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class QueryRetrievalStatics {
		private static DatabaseInfo info;

		internal static void Generate(
			DBConnection cn, TextWriter writer, string baseNamespace, Database database,
			RedStapler.StandardLibrary.Configuration.SystemDevelopment.Database configuration ) {
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
				writer.WriteLine( "public static partial class " + query.name + "Retrieval {" );

				// Write nested classes.
				DataAccessStatics.WriteRowClasses( writer, columns, localWriter => { } );
				writeCacheClass( writer, database, query );

				writer.WriteLine( "private const string selectFromClause = @\"" + query.selectFromClause + " \";" );
				foreach( var postSelectFromClause in query.postSelectFromClauses )
					writeQueryMethod( writer, database, query, postSelectFromClause );
				writer.WriteLine( "static partial void updateSingleRowCaches( Row row );" );
				writer.WriteLine( "}" ); // class
			}
			writer.WriteLine( "}" ); // namespace
		}

		private static List<Column> validateQueryAndGetColumns( DBConnection cn, RedStapler.StandardLibrary.Configuration.SystemDevelopment.Query query ) {
			// Attempt to query with every postSelectFromClause to ensure validity.
			foreach( var postSelectFromClause in query.postSelectFromClauses ) {
				cn.ExecuteReaderCommandWithSchemaOnlyBehavior(
					DataAccessStatics.GetCommandFromRawQueryText( cn, query.selectFromClause + " " + postSelectFromClause.Value ),
					r => { } );
			}

			return Column.GetColumnsInQueryResults( cn, query.selectFromClause, false );
		}

		private static void writeCacheClass( TextWriter writer, Database database, RedStapler.StandardLibrary.Configuration.SystemDevelopment.Query query ) {
			writer.WriteLine( "private partial class Cache {" );
			writer.WriteLine(
				"internal static Cache Current { get { return DataAccessState.Current.GetCacheValue( \"" + database.SecondaryDatabaseName + query.name +
				"QueryRetrieval\", () => new Cache() ); } }" );
			foreach( var i in query.postSelectFromClauses ) {
				var type = getQueryCacheType( query, i );
				writer.WriteLine( "private readonly " + type + " " + getQueryCacheName( query, i, true ) + " = new " + type + "();" );
			}
			writer.WriteLine( "private Cache() {}" );
			foreach( var i in query.postSelectFromClauses ) {
				var type = getQueryCacheType( query, i );
				writer.WriteLine( "internal " + type + " " + getQueryCacheName( query, i, false ) + " { get { return " + getQueryCacheName( query, i, true ) + "; } }" );
			}
			writer.WriteLine( "}" );
		}

		private static string getQueryCacheType(
			RedStapler.StandardLibrary.Configuration.SystemDevelopment.Query query,
			RedStapler.StandardLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause ) {
			return DataAccessStatics.GetNamedParamList( info, query.selectFromClause + " " + postSelectFromClause.Value ).Any()
				       ? "QueryRetrievalQueryCache<Row>"
				       : "ParameterlessQueryCache<Row>";
		}

		private static void writeQueryMethod(
			TextWriter writer, Database database, RedStapler.StandardLibrary.Configuration.SystemDevelopment.Query query,
			RedStapler.StandardLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer, "Queries the database and returns the full results collection immediately." );
			writer.WriteLine(
				"public static IEnumerable<Row> GetRows" + postSelectFromClause.name + "( " +
				DataAccessStatics.GetMethodParamsFromCommandText( info, query.selectFromClause + " " + postSelectFromClause.Value ) + " ) {" );


			// body

			var namedParamList = DataAccessStatics.GetNamedParamList( info, query.selectFromClause + " " + postSelectFromClause.Value );
			var getResultSetFirstArg = namedParamList.Any() ? "new[] { " + StringTools.ConcatenateWithDelimiter( ", ", namedParamList.ToArray() ) + " }, " : "";
			writer.WriteLine( "return Cache.Current." + getQueryCacheName( query, postSelectFromClause, false ) + ".GetResultSet( " + getResultSetFirstArg + "() => {" );

			writer.WriteLine( "var cmd = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
			writer.WriteLine( "cmd.CommandText = selectFromClause + @\"" + postSelectFromClause.Value + "\";" );
			DataAccessStatics.WriteAddParamBlockFromCommandText( writer, "cmd", info, query.selectFromClause + " " + postSelectFromClause.Value, database );
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
			RedStapler.StandardLibrary.Configuration.SystemDevelopment.Query query,
			RedStapler.StandardLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause, bool getFieldName ) {
			return ( getFieldName ? "rows" : "Rows" ) + postSelectFromClause.name +
			       ( DataAccessStatics.GetNamedParamList( info, query.selectFromClause + " " + postSelectFromClause.Value ).Any() ? "Queries" : "Query" );
		}
	}
}