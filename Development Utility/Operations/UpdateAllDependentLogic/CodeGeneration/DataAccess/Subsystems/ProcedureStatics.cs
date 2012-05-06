using System.Data;
using System.IO;
using System.Linq;
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
				writer.Write( "public static void " + procedure + "( DBConnection cn" );
				foreach( var parameter in parameters ) {
					var parameterType = "";
					if( parameter.Direction == ParameterDirection.Output )
						parameterType = "out ";
					else if( parameter.Direction == ParameterDirection.InputOutput )
						parameterType = "ref ";
					writer.Write( ", " + parameterType + parameter.DataTypeName + " " + parameter.Name );
				}
				writer.WriteLine( " ) { " );

				// body
				writer.WriteLine( "var cmd = new SprocExecution( \"" + procedure + "\" );" );
				foreach( var parameter in parameters ) {
					if( parameter.Direction == ParameterDirection.Input )
						writer.WriteLine( "cmd.AddParameter( " + getDbCommandParameterCreationExpression( parameter ) + " );" );
					else {
						writer.WriteLine( "var " + parameter.Name + "Parameter = " + getDbCommandParameterCreationExpression( parameter ) + ";" );
						writer.WriteLine( parameter.Name + "Parameter.GetAdoDotNetParameter( cn.DatabaseInfo ).Direction = ParameterDirection." + parameter.Direction + ";" );
						writer.WriteLine( "cmd.AddParameter( " + parameter.Name + "Parameter );" );
					}
				}
				foreach( var parameter in parameters.Where( parameter => parameter.Direction != ParameterDirection.Input ) )
					writer.WriteLine( parameter.DataTypeName + " " + parameter.Name + "Local = " + parameter.DataTypeDefaultValueExpression + ";" );
				writer.WriteLine( "cmd.ExecuteReader( cn, r => {" );
				foreach( var parameter in parameters.Where( parameter => parameter.Direction != ParameterDirection.Input ) ) {
					// NOTE: This is a hack. We would like to use a simple cast to convert the value of the database parameter to the method parameter's type, but we
					// can't because the types in Oracle.DataAccess.Types, like OracleDecimal, do not support any kind of conversion to .NET types when they are boxed.
					writer.WriteLine( parameter.Name + "Local = (" + parameter.DataTypeName + ")StandardLibraryMethods.ChangeType( " + parameter.Name +
					                  "Parameter.GetAdoDotNetParameter( cn.DatabaseInfo ).Value.ToString(), typeof( " + parameter.DataTypeName + " ) );" );
					//writer.WriteLine( parameter.Name + "Local = (" + parameter.DataTypeName + ")" + parameter.Name +
					//                  "Parameter.GetAdoDotNetParameter( cn.DatabaseInfo ).Value;" );
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