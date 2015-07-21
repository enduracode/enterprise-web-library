using System;
using System.IO;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class CustomModificationStatics {
		private static DatabaseInfo info;

		internal static void Generate(
			DBConnection cn, TextWriter writer, string baseNamespace, Database database, Configuration.SystemDevelopment.Database configuration ) {
			info = cn.DatabaseInfo;
			if( configuration.customModifications != null ) {
				writer.WriteLine( "namespace " + baseNamespace + " {" );

				testQueries( cn, configuration.customModifications );

				CodeGenerationStatics.AddSummaryDocComment( writer, "Contains custom modification operations." );
				writer.WriteLine( "public static class " + database.SecondaryDatabaseName + "CustomModifications {" );

				foreach( var mod in configuration.customModifications )
					writeMethod( writer, database, mod );

				writer.WriteLine( "}" ); // class
				writer.WriteLine( "}" ); // namespace
			}
		}

		private static void testQueries( DBConnection cn, Configuration.SystemDevelopment.CustomModification[] mods ) {
			// We don't test commands in Oracle because:
			// 1. There's no good junk value to pass in.
			// 2. The only way to keep the commands from actually modifying the database is with a transaction rollback, and we don't want to do that unless absolutely necessary.
			// And we don't test commands in MySQL because of reason 2 above.
			if( cn.DatabaseInfo is MySqlInfo || cn.DatabaseInfo is OracleInfo )
				return;

			foreach( var mod in mods ) {
				foreach( var command in mod.commands ) {
					var cmd = DataAccessStatics.GetCommandFromRawQueryText( cn, command );
					try {
						cn.ExecuteReaderCommandWithSchemaOnlyBehavior( cmd, r => { } );
					}
					catch( Exception e ) {
						throw new UserCorrectableException( "Custom modification " + mod.name + " failed.", e );
					}
				}
			}
		}

		private static void writeMethod( TextWriter writer, Database database, Configuration.SystemDevelopment.CustomModification mod ) {
			writer.WriteLine(
				"public static void " + mod.name + "( " +
				DataAccessStatics.GetMethodParamsFromCommandText( info, StringTools.ConcatenateWithDelimiter( "; ", mod.commands ) ) + " ) {" );

			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( delegate {" );
			var cnt = 0;
			foreach( var command in mod.commands ) {
				var commandVariableName = "cmd" + cnt++;
				writer.WriteLine( "DbCommand " + commandVariableName + " = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
				writer.WriteLine( commandVariableName + ".CommandText = @\"" + command + "\";" );
				DataAccessStatics.WriteAddParamBlockFromCommandText( writer, commandVariableName, info, command, database );
				writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteNonQueryCommand( " + commandVariableName + " );" );
			}
			writer.WriteLine( "} );" ); // execute in transaction call

			writer.WriteLine( "}" ); // method
		}
	}
}