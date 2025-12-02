using System.ComponentModel;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.SystemSpecificLogic;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;
using NodaTime;
using NodaTime.Text;

namespace EnterpriseWebLibrary;

/// <summary>
/// Generated code use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public static class DataCleanupOps {
	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static void CleanUpData() {
		if( UserManagementStatics.UserManagementEnabled )
			if( ConfigurationStatics.DatabaseExists && !AutomaticDatabaseConnectionManager.HasCurrent )
				DataAccessState.Current.PrimaryDatabaseConnection.ExecuteWithConnectionOpen( () =>
					DataAccessState.Current.PrimaryDatabaseConnection.ExecuteInTransaction( cleanUpUserRequests ) );
			else
				cleanUpUserRequests();

		var cutoffTime = Clock.TransactionTime - Duration.FromDays( 14 );
		var debugCutoffTime = InstantPattern.CreateWithInvariantCulture( TelemetryStatics.DebugLogTimeFormat )
			.Format( Clock.TransactionTime - Duration.FromDays( 1 ) );
		foreach( var app in ConfigurationStatics.InstallationConfiguration.WebApplications ) {
			var filePath = app.DiagnosticLogFilePath;
			if( File.Exists( filePath ) ) {
				var timeStampPattern = OffsetDateTimePattern.CreateWithInvariantCulture( "uuuu'-'MM'-'dd HH:mm:ss.FFFFFFFFF o<m>" );
				try {
					File.WriteAllLines(
						filePath,
						File.ReadAllLines( filePath )
							.SkipWhile( line => {
								var endIndex = line.IndexOf( " [", StringComparison.Ordinal );
								if( endIndex < 0 )
									return true;

								var timeStamp = line[ ..endIndex ];
								var parseResult = timeStampPattern.Parse( timeStamp );
								if( !parseResult.TryGetValue( default, out var time ) )
									return true;

								return time.ToInstant() < cutoffTime;
							} )
							.Materialize(),
						Encoding.UTF8 );
				}
				catch( IOException ) {
					TelemetryStatics.ReportFault(
						$"Failed to clean up the diagnostic log for {app.Name} because the application is running. The file size is {FormattingMethods.GetFormattedBytes( new FileInfo( filePath ).Length )}." );
				}
			}

			if( File.Exists( app.DebugLogFilePath ) ) {
				DatabaseInfo dbInfo = new SqliteInfo( "Debug Log", app.DebugLogFilePath, false );

				var deleteCommand = dbInfo.CreateCommand();
				var parameter = new DbCommandParameter( "cutoff", new DbParameterValue( debugCutoffTime ) );
				deleteCommand.CommandText = $"DELETE FROM Events WHERE Timestamp < {parameter.GetNameForCommandText( dbInfo )}";
				deleteCommand.Parameters.Add( parameter.GetAdoDotNetParameter( dbInfo ) );

				var vacuumCommand = dbInfo.CreateCommand();
				vacuumCommand.CommandText = "VACUUM";

				var connection = new DatabaseConnection( dbInfo );
				connection.ExecuteWithConnectionOpen( () => {
					connection.ExecuteNonQueryCommand( deleteCommand );
					connection.ExecuteNonQueryCommand( vacuumCommand );
				} );
			}
		}

		SystemSpecificLogicStatics.GeneralProvider.CleanUpData( AutomaticDatabaseConnectionManager.HasCurrent );
	}

	private static void cleanUpUserRequests() {
		var provider = UserManagementStatics.SystemProvider;
		var latestRequests = provider.GetUserRequests().GroupBy( i => i.UserId, ( _, requests ) => requests.MaxBy( i => i.RequestTime )! ).Materialize();
		provider.ClearUserRequests();
		foreach( var i in latestRequests )
			provider.InsertUserRequest( i.UserId, i.RequestTime );
	}
}