using System.Data.Common;

namespace RedStapler.StandardLibrary.DatabaseSpecification {
	/// <summary>
	/// Contains information about a database.
	/// </summary>
	public interface DatabaseInfo {
		/// <summary>
		/// If this is a secondary database, gets the name. Returns the empty string if this is the primary database.
		/// </summary>
		string SecondaryDatabaseName { get; }

		/// <summary>
		/// Returns the prefix used for parameters in SQL queries (@, :, etc.).
		/// </summary>
		string ParameterPrefix { get; }

		/// <summary>
		/// Creates an ADO.NET database connection to the database.
		/// </summary>
		DbConnection CreateConnection( string connectionString );

		/// <summary>
		/// Creates an ADO.NET command for the database.
		/// </summary>
		DbCommand CreateCommand();

		/// <summary>
		/// Creates an ADO.NET command parameter for the database.
		/// </summary>
		DbParameter CreateParameter();

		/// <summary>
		/// Gets the string that represents the specified database-specific type.
		/// </summary>
		string GetDbTypeString( object databaseSpecificType );

		/// <summary>
		/// Sets the specified parameter's database-specific type to the type represented by the specified string.
		/// </summary>
		void SetParameterType( DbParameter parameter, string dbTypeString );
	}
}