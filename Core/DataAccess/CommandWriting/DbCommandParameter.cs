﻿using System.Data.Common;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;

namespace EnterpriseWebLibrary.DataAccess.CommandWriting;

/// <summary>
/// A parameter for a database command.
/// </summary>
public class DbCommandParameter {
	private readonly string name;
	private readonly DbParameterValue value;
	private DbParameter? parameter;

	/// <summary>
	/// Creates a command parameter.
	/// </summary>
	public DbCommandParameter( string name, DbParameterValue value ) {
		this.name = name;
		this.value = value;
	}

	/// <summary>
	/// Returns true if the value of this parameter is null.
	/// </summary>
	internal bool ValueIsNull => value.Value == null;

	public string GetNameForCommandText( DatabaseInfo databaseInfo ) => databaseInfo.ParameterPrefix + name;

	/// <summary>
	/// Returns the ADO.NET parameter object for this parameter. The ADO.NET parameter object is created on the first call to this method.
	/// </summary>
	public DbParameter GetAdoDotNetParameter( DatabaseInfo databaseInfo ) {
		if( parameter != null )
			return parameter;
		parameter = databaseInfo.CreateParameter();

		// SQL Server requires the prefix here. Although Oracle requires it in the command text, it does not require it here and it's questionable whether it is
		// even allowed. We do not know whether MySQL requires it here, but the examples we've seen do include it.
		parameter.ParameterName = ( databaseInfo is OracleInfo ? "" : databaseInfo.ParameterPrefix ) + name;

		parameter.Value = value.Value ?? DBNull.Value;
		if( value.DbTypeString != null )
			databaseInfo.SetParameterType( parameter, value.DbTypeString );
		return parameter;
	}
}