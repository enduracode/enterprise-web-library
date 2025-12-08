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
				_ => throw new ApplicationException( "Invalid database information object type." )
			};

	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public static string GetDatabaseNounPhrase( Database database ) =>
		"{0} database".FormatWith( database.SecondaryDatabaseName.Any() ? "{0} secondary".FormatWith( database.SecondaryDatabaseName ) : "primary" );

	public static void ExportDatabaseToFile( Database database, string dataPackageFolderPath ) {
		if( database is not NoDatabase )
			database.ExportToFile( getDatabaseFilePath( dataPackageFolderPath, database ) );
	}

	internal static void DeleteAndReCreateDatabaseFromFile( Database database, bool databaseHasMinimumDataRevision, string dataPackageFolderPath ) {
		if( database is NoDatabase )
			return;

		var filePath = getDatabaseFilePath( dataPackageFolderPath, database );
		if( !File.Exists( filePath ) )
			filePath = "";

		if( databaseHasMinimumDataRevision && !filePath.Any() )
			throw new UserCorrectableException(
				"Failed to re-create the {0} because the data package did not exist, or did not contain a file.".FormatWith( GetDatabaseNounPhrase( database ) ) );
		database.DeleteAndReCreateFromFile( filePath );
		if( !filePath.Any() )
			Log.Information( "Created a new {0} because the data package did not exist, or did not contain a file.".FormatWith( GetDatabaseNounPhrase( database ) ) );
	}

	private static string getDatabaseFilePath( string dataPackageFolderPath, Database database ) =>
		EwlStatics.CombinePaths( dataPackageFolderPath, ( database.SecondaryDatabaseName.Length > 0 ? database.SecondaryDatabaseName : "Primary" ) + ".bak" );

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
	public static IEnumerable<( string name, bool hasModTable )> GetDatabaseTables( Database database ) {
		var tableNames = database.GetTables().Materialize();

		var modTableSuffix = GetModificationTableSuffix( database );
		bool isModTable( string table ) => table.EndsWithIgnoreCase( modTableSuffix );
		var modTables = tableNames.Where( isModTable ).ToImmutableHashSet( StringComparer.Ordinal );

		return database.GetTables().Where( i => !isModTable( i ) ).OrderBy( i => i ).Select( i => ( i, modTables.Contains( i + modTableSuffix ) ) );
	}

	public static string GetModificationTableSuffix( Database database ) =>
		database switch
			{
				MySql => "_table_{0}_modifications".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() ),
				Oracle => "_TABLE_{0}_MODIFICATIONS".FormatWith( EwlStatics.EwlInitialism.ToUpperInvariant() ),
				_ => "Table{0}Modifications".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() )
			};

	public static void ClearModificationTables( Database database ) {
		foreach( var table in GetDatabaseTables( database ).Where( i => i.hasModTable ).Select( i => i.name ) )
			database.ExecuteDbMethod( connection => {
				var command = connection.DatabaseInfo.CreateCommand();
				command.CommandText = "DELETE FROM {0}".FormatWith( table + GetModificationTableSuffix( database ) );
				connection.ExecuteNonQueryCommand( command );
			} );
	}
}