using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using Oracle.ManagedDataAccess.Client;

namespace EnterpriseWebLibrary.OracleDatabase;

public class OracleDatabaseProvider: ExternalOracleDatabaseProvider {
	DbConnection ExternalOracleDatabaseProvider.CreateConnection( string connectionString, bool allowVersion11Authentication ) {
		var connection = new OracleConnection( connectionString );
		if( allowVersion11Authentication )
			connection.SqlNetAllowedLogonVersionClient = OracleAllowedLogonVersionClient.Version11;
		return connection;
	}

	DbCommand ExternalOracleDatabaseProvider.CreateCommand() {
		var command = new OracleCommand();

		// This property would be important if we screwed up the order of parameter adding later on.
		command.BindByName = true;

		// Tell the data reader to retrieve LOB data along with the rest of the row rather than making a separate request when GetValue is called.
		// Unfortunately, as of 17 July 2014 there is an Oracle bug that prevents us from setting the property to -1. See
		// http://stackoverflow.com/q/9006773/35349, https://community.oracle.com/thread/3548124, and Oracle bugs 14279177 and 17869834.
		//c.InitialLOBFetchSize = -1;
		command.InitialLOBFetchSize = 1024;

		return command;
	}

	DbParameter ExternalOracleDatabaseProvider.CreateParameter() => new OracleParameter();

	string ExternalOracleDatabaseProvider.GetDbTypeString( object databaseSpecificType ) => ( (OracleDbType)databaseSpecificType ).ToString();

	void ExternalOracleDatabaseProvider.SetParameterType( DbParameter parameter, string dbTypeString ) {
		( (OracleParameter)parameter ).OracleDbType = dbTypeString.ToEnum<OracleDbType>();
	}
}