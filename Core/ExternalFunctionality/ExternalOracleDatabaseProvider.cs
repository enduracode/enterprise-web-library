﻿using System.Data.Common;

namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External Oracle Database logic.
/// </summary>
public interface ExternalOracleDatabaseProvider {
	DbConnection CreateConnection( string connectionString, bool allowVersion11Authentication );

	DbCommand CreateCommand();

	DbParameter CreateParameter();

	string GetDbTypeString( object databaseSpecificType );

	void SetParameterType( DbParameter parameter, string dbTypeString );
}