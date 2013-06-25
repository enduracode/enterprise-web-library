using System;
using System.Collections.Generic;
using System.IO;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class QueryRetrievalStatics {
		private static DatabaseInfo info;

		internal static void Generate( DBConnection cn, TextWriter writer, string baseNamespace, Database database,
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
				DataAccessStatics.WriteRowClass( writer, columns, cn.DatabaseInfo );

				writer.WriteLine( "private const string selectFromClause = @\"" + query.selectFromClause + " \";" );
				foreach( var postSelectFromClause in query.postSelectFromClauses )
					writeQueryMethod( writer, database, query, postSelectFromClause );
				writer.WriteLine( "}" ); // class
			}
			writer.WriteLine( "}" ); // namespace
		}

		private static List<Column> validateQueryAndGetColumns( DBConnection cn, RedStapler.StandardLibrary.Configuration.SystemDevelopment.Query query ) {
			// Attempt to query with every postSelectFromClause to ensure validity.
			foreach( var postSelectFromClause in query.postSelectFromClauses ) {
				cn.ExecuteReaderCommandWithSchemaOnlyBehavior(
					DataAccessStatics.GetCommandFromRawQueryText( cn, query.selectFromClause + " " + postSelectFromClause.Value ), r => { } );
			}

			return Column.GetColumnsInQueryResults( cn, query.selectFromClause, false );
		}

		private static void writeQueryMethod( TextWriter writer, Database database, RedStapler.StandardLibrary.Configuration.SystemDevelopment.Query query,
		                                      RedStapler.StandardLibrary.Configuration.SystemDevelopment.QueryPostSelectFromClause postSelectFromClause ) {
			// header
			CodeGenerationStatics.AddSummaryDocComment( writer, "Queries the database and returns the full results collection immediately." );
			writer.WriteLine( "public static IEnumerable<Row> GetRows" + postSelectFromClause.name + "( " +
			                  DataAccessStatics.GetMethodParamsFromCommandText( info, query.selectFromClause + " " + postSelectFromClause.Value ) + " ) {" );

			// body
			writer.WriteLine( "var cmd = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
			writer.WriteLine( "cmd.CommandText = selectFromClause + @\"" + postSelectFromClause.Value + "\";" );
			DataAccessStatics.WriteAddParamBlockFromCommandText( writer, "cmd", info, query.selectFromClause + " " + postSelectFromClause.Value, database );
			writer.WriteLine( "var results = new List<Row>();" );
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) +
			                  ".ExecuteReaderCommand( cmd, r => { while( r.Read() ) results.Add( new Row( r ) ); } );" );
			writer.WriteLine( "return results;" );
			writer.WriteLine( "}" ); // GetRows

			// NOTE: Delete this after 30 September 2013.
			writer.WriteLine( "[ System.Obsolete( \"Guaranteed through 30 September 2013. Please use the overload without the DBConnection parameter.\" ) ]" );
			writer.WriteLine( "public static IEnumerable<Row> GetRows" + postSelectFromClause.name + "( " +
			                  getOldMethodParamsFromCommandText( info, query.selectFromClause + " " + postSelectFromClause.Value ) + " ) {" );
			writer.WriteLine( "var cmd = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
			writer.WriteLine( "cmd.CommandText = selectFromClause + @\"" + postSelectFromClause.Value + "\";" );
			DataAccessStatics.WriteAddParamBlockFromCommandText( writer, "cmd", info, query.selectFromClause + " " + postSelectFromClause.Value, database );
			writer.WriteLine( "var results = new List<Row>();" );
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) +
			                  ".ExecuteReaderCommand( cmd, r => { while( r.Read() ) results.Add( new Row( r ) ); } );" );
			writer.WriteLine( "return results;" );
			writer.WriteLine( "}" ); // GetRows
		}

		// NOTE: Delete this after 30 September 2013.
		private static string getOldMethodParamsFromCommandText( DatabaseInfo info, string commandText ) {
			var methodParams = "DBConnection cn";
			foreach( var param in DataAccessStatics.GetNamedParamList( info, commandText ) )
				methodParams += ", " + "object " + param;
			return methodParams;
		}
	}
}