using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Databases;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction {
	public static class DatabaseOps {
		internal static Database CreateDatabase( DatabaseInfo databaseInfo ) {
			if( databaseInfo == null )
				return new NoDatabase();
			if( databaseInfo is SqlServerInfo )
				return new SqlServer( databaseInfo as SqlServerInfo, "Data", "Log" );
			if( databaseInfo is MySqlInfo )
				return new MySql( databaseInfo as MySqlInfo );
			if( databaseInfo is OracleInfo )
				return new Databases.Oracle( databaseInfo as OracleInfo );
			throw new ApplicationException( "Invalid database information object type." );
		}

		/// <summary>
		/// Installation Support Utility use only.
		/// </summary>
		public static string GetDatabaseNounPhrase( Database database ) =>
			"{0} database".FormatWith( database.SecondaryDatabaseName.Any() ? "{0} secondary".FormatWith( database.SecondaryDatabaseName ) : "primary" );

		public static void UpdateDatabaseLogicIfUpdateFileExists( Database database, string databaseUpdateFilePath, bool allFailuresUserCorrectable ) {
			var linesInScriptOnHd = GetNumberOfLinesInDatabaseScript( databaseUpdateFilePath );

			// We don't want to ask the database for the line number if there is no script.
			if( linesInScriptOnHd == null )
				return;

			int lineMarker;
			try {
				lineMarker = database.GetLineMarker();
			}
			catch( Exception e ) {
				const string message = "Failed to get line marker.";
				if( allFailuresUserCorrectable )
					throw new UserCorrectableException( message, e );
				throw UserCorrectableException.CreateSecondaryException( message, e );
			}

			// We don't want to execute blank scripts against the database because this will cause an error with read-only databases.
			if( lineMarker == linesInScriptOnHd )
				return;

			using( var sw = new StringWriter() ) {
				// If the string writer's value is not the empty string, it will end with the line terminator string.
				using( var tr = new StreamReader( File.OpenRead( databaseUpdateFilePath ) ) ) {
					// Read and discard all text before the marker line.
					for( var i = 0; i < lineMarker; i++ )
						tr.ReadLine();

					// Store all text on and after the marker line and move the marker to the end of the file.
					for( string lineText; ( lineText = tr.ReadLine() ) != null; lineMarker += 1 )
						sw.WriteLine( lineText );
				}

				try {
					database.ExecuteSqlScriptInTransaction( sw.ToString() );
				}
				catch( Exception e ) {
					const string message = "Failed to update database logic.";
					if( allFailuresUserCorrectable )
						throw new UserCorrectableException( message, e );
					throw UserCorrectableException.CreateSecondaryException( message, e );
				}
			}
			database.UpdateLineMarker( lineMarker );
		}

		public static void ExportDatabaseToFile( Database database, string dataPackageFolderPath ) {
			if( !( database is NoDatabase ) )
				database.ExportToFile( getDatabaseFilePath( dataPackageFolderPath, database ) );
		}

		public static void DeleteAndReCreateDatabaseFromFile( Database database, bool databaseHasMinimumDataRevision, string dataPackageFolderPath ) {
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
				StatusStatics.SetStatus(
					"Created a new {0} because the data package did not exist, or did not contain a file.".FormatWith( GetDatabaseNounPhrase( database ) ) );
		}

		private static string getDatabaseFilePath( string dataPackageFolderPath, Database database ) {
			return EwlStatics.CombinePaths(
				dataPackageFolderPath,
				( database.SecondaryDatabaseName.Length > 0 ? database.SecondaryDatabaseName : "Primary" ) + ".bak" );
		}

		/// <summary>
		/// SQL Server takes awhile to recover to a usable state after restoring.  Wait until it is.
		/// </summary>
		public static void WaitForDatabaseRecovery( Database database ) {
			if( database is NoDatabase )
				return;
			StatusStatics.SetStatus( "Waiting for database to be ready..." );
			ActionTools.Retry( () => database.GetLineMarker(), "Database failed to be ready." );
			StatusStatics.SetStatus( "Database is ready." );
		}

		/// <summary>
		/// Gets the tables in the specified database, ordered by name.
		/// </summary>
		public static IEnumerable<string> GetDatabaseTables( Database database ) {
			return database.GetTables().OrderBy( i => i );
		}

		/// <summary>
		/// Returns null if no database script exists on the hard drive.
		/// </summary>
		public static int? GetNumberOfLinesInDatabaseScript( string databaseUpdateFilePath ) {
			if( !File.Exists( databaseUpdateFilePath ) )
				return null;

			var lines = 0;
			using( var reader = new StreamReader( File.OpenRead( databaseUpdateFilePath ) ) ) {
				while( reader.ReadLine() != null )
					lines++;
			}
			return lines;
		}
	}
}