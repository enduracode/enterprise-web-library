using System.Collections.Immutable;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;
using JetBrains.Annotations;
using Serilog;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

[ PublicAPI ]
public static class DatabaseOps {
	internal static Database CreateDatabase( DatabaseInfo? databaseInfo ) =>
		databaseInfo switch
			{
				null => new NoDatabase(),
				SqlServerInfo info => new SqlServer( info, "Data", "Log" ),
				MySqlInfo info => new MySql( info ),
				OracleInfo info => new Oracle( info ),
				FolderDatabaseInfo info => new FolderDatabase( info ),
				_ => throw new ApplicationException( "Invalid database information object type." )
			};

	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public static string GetDatabaseNounPhrase( Database database ) =>
		"{0} database".FormatWith( database.SecondaryDatabaseName.Any() ? "{0} secondary".FormatWith( database.SecondaryDatabaseName ) : "primary" );

	/// <summary>
	/// SQL Server takes awhile to recover to a usable state after restoring.  Wait until it is.
	/// </summary>
	internal static void WaitForDatabaseRecovery( Database database ) {
		if( database is NoDatabase )
			return;
		Log.Information( "Waiting for database to be ready..." );
		ExceptionHandlingTools.Retry( () => database.GetLineMarker(), "Database failed to be ready." );
		Log.Information( "Database is ready." );
	}

	/// <summary>
	/// Gets the tables in the specified database, ordered by name.
	/// </summary>
	public static IEnumerable<( DatabaseTable tableName, bool hasModTable )> GetDatabaseTables( Database database ) {
		var tables = database.GetTables().Materialize();

		var modTableSuffix = getModificationTableSuffix( database );
		bool isModTable( DatabaseTable table ) => table.Name.EndsWithIgnoreCase( modTableSuffix );
		var modTables = tables.Where( isModTable ).Select( i => i.QualifiedName ).ToImmutableHashSet( StringComparer.Ordinal );

		return tables.Where( i => !isModTable( i ) )
			.OrderBy( i => i.Schema )
			.ThenBy( i => i.Name )
			.Select( i => ( i, modTables.Contains( ( i with { Name = i.Name + modTableSuffix } ).QualifiedName ) ) );
	}

	public static string GetModificationTableQualifiedName( Database database, DatabaseTable table ) =>
		( table with { Name = table.Name + getModificationTableSuffix( database ) } ).QualifiedName;

	private static string getModificationTableSuffix( Database database ) =>
		database switch
			{
				MySql => "_table_{0}_modifications".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() ),
				Oracle => "_TABLE_{0}_MODIFICATIONS".FormatWith( EwlStatics.EwlInitialism.ToUpperInvariant() ),
				_ => "Table{0}Modifications".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() )
			};

	public static void ClearModificationTables( Database database ) {
		foreach( var table in GetDatabaseTables( database ).Where( i => i.hasModTable ).Select( i => i.tableName ) )
			database.ExecuteDbMethod( connection => {
				var command = connection.DatabaseInfo.CreateCommand();
				command.CommandText = "DELETE FROM {0}".FormatWith( GetModificationTableQualifiedName( database, table ) );
				connection.ExecuteNonQueryCommand( command );
			} );
	}
}