using System.Data;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class ProcedureStatics {
		internal static void Generate( DBConnection cn, TextWriter writer, string baseNamespace, Database database ) {
			writer.WriteLine( "namespace " + baseNamespace + " {" );
			writer.WriteLine( "public static class " + database.SecondaryDatabaseName + "Procedures {" );
			foreach( var procedure in database.GetProcedures() ) {
				var parameters = database.GetProcedureParameters( procedure );

				// header
				CodeGenerationStatics.AddSummaryDocComment( writer, "Executes the " + procedure + " procedure." );
				var parameterDeclarations =
					parameters.Select(
						i => ( i.Direction == ParameterDirection.Output ? "out " : i.Direction == ParameterDirection.InputOutput ? "ref " : "" ) + i.DataTypeName + " " + i.Name );
				writer.WriteLine( "public static void " + procedure + "( " + StringTools.ConcatenateWithDelimiter( ", ", parameterDeclarations.ToArray() ) + " ) {" );

				// body
				writer.WriteLine( "var cmd = new SprocExecution( \"" + procedure + "\" );" );
				foreach( var parameter in parameters ) {
					if( parameter.Direction == ParameterDirection.Input )
						writer.WriteLine( "cmd.AddParameter( " + getDbCommandParameterCreationExpression( parameter ) + " );" );
					else {
						writer.WriteLine( "var " + parameter.Name + "Parameter = " + getDbCommandParameterCreationExpression( parameter ) + ";" );
						writer.WriteLine(
							parameter.Name + "Parameter.GetAdoDotNetParameter( " + DataAccessStatics.GetConnectionExpression( database ) +
							".DatabaseInfo ).Direction = ParameterDirection." + parameter.Direction + ";" );
						writer.WriteLine( "cmd.AddParameter( " + parameter.Name + "Parameter );" );
					}
				}
				foreach( var parameter in parameters.Where( parameter => parameter.Direction != ParameterDirection.Input ) )
					writer.WriteLine( parameter.DataTypeName + " " + parameter.Name + "Local = " + parameter.DataTypeDefaultValueExpression + ";" );
				writer.WriteLine( "cmd.ExecuteReader( " + DataAccessStatics.GetConnectionExpression( database ) + ", r => {" );
				foreach( var parameter in parameters.Where( parameter => parameter.Direction != ParameterDirection.Input ) ) {
					// NOTE: This is a hack. We would like to use a simple cast to convert the value of the database parameter to the method parameter's type, but we
					// can't because the types in Oracle.DataAccess.Types, like OracleDecimal, do not support any kind of conversion to .NET types when they are boxed.
					writer.WriteLine(
						parameter.Name + "Local = (" + parameter.DataTypeName + ")StandardLibraryMethods.ChangeType( " + parameter.Name + "Parameter.GetAdoDotNetParameter( " +
						DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo ).Value.ToString(), typeof( " + parameter.DataTypeName + " ) );" );
					//writer.WriteLine( parameter.Name + "Local = (" + parameter.DataTypeName + ")" + parameter.Name + "Parameter.GetAdoDotNetParameter( " +
					//                  DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo ).Value;" );
				}
				writer.WriteLine( "} );" );
				foreach( var parameter in parameters.Where( parameter => parameter.Direction != ParameterDirection.Input ) )
					writer.WriteLine( parameter.Name + " = " + parameter.Name + "Local;" );
				writer.WriteLine( "}" );
			}
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private static string getDbCommandParameterCreationExpression( ProcedureParameter parameter ) {
			return "new DbCommandParameter( \"" + parameter.Name + "\", new DbParameterValue( " +
			       ( parameter.Direction != ParameterDirection.Output ? parameter.Name : "null" ) + ", \"" + parameter.DbTypeString + "\" ) )";
		}
	}
}