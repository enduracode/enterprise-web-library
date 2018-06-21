using System.IO;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class SequenceStatics {
		internal static void Generate( DBConnection cn, TextWriter writer, string baseNamespace, Database database ) {
			writer.WriteLine( "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "Sequences {" );

			var cmd = cn.DatabaseInfo.CreateCommand();
			cmd.CommandText = "SELECT * FROM USER_SEQUENCES";
			cn.ExecuteReaderCommand( cmd,
			                         reader => {
				                         while( reader.Read() ) {
					                         var sequenceName = reader[ "SEQUENCE_NAME" ].ToString();
					                         writer.WriteLine();
					                         writer.WriteLine( "public class " + sequenceName + " {" );
					                         writer.WriteLine( "public static decimal GetNextValue() {" );
					                         writer.WriteLine( "DbCommand cmd = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
					                         writer.WriteLine( "cmd.CommandText = \"SELECT " + sequenceName + ".NEXTVAL FROM DUAL\";" );
					                         writer.WriteLine( "return (decimal)" + DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteScalarCommand( cmd );" );
					                         writer.WriteLine( "}" );
					                         writer.WriteLine( "}" );
				                         }
			                         } );

			writer.WriteLine();
			writer.WriteLine( "}" );
		}
	}
}