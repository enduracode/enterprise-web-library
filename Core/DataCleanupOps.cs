using System.ComponentModel;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;
using NodaTime;
using NodaTime.Text;

namespace EnterpriseWebLibrary;

/// <summary>
/// EWL use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public static class DataCleanupOps {
	/// <summary>
	/// EWL use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static void CleanUpData() {
		if( UserManagementStatics.UserManagementEnabled )
			if( ConfigurationStatics.DatabaseExists )
				DataAccessState.Current.PrimaryDatabaseConnection.ExecuteWithConnectionOpen(
					() => DataAccessState.Current.PrimaryDatabaseConnection.ExecuteInTransaction( cleanUpUserRequests ) );
			else
				cleanUpUserRequests();

		var cutoffTime = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays( 14 );
		foreach( var app in ConfigurationStatics.InstallationConfiguration.WebApplications ) {
			var filePath = app.DiagnosticLogFilePath;
			if( !File.Exists( filePath ) )
				continue;
			var timeStampPattern = OffsetDateTimePattern.CreateWithInvariantCulture( "uuuu'-'MM'-'dd HH:mm:ss.FFFFFFFFF o<m>" );
			File.WriteAllLines(
				filePath,
				File.ReadAllLines( filePath )
					.SkipWhile(
						line => {
							var timeStamp = line[ ..line.IndexOf( " [", StringComparison.Ordinal ) ];
							return timeStampPattern.Parse( timeStamp ).GetValueOrThrow().ToInstant() < cutoffTime;
						} ),
				Encoding.UTF8 );
		}
	}

	private static void cleanUpUserRequests() {
		var provider = UserManagementStatics.SystemProvider;
		var latestRequests = provider.GetUserRequests().GroupBy( i => i.UserId, ( _, requests ) => requests.MaxBy( i => i.RequestTime )! ).Materialize();
		provider.ClearUserRequests();
		foreach( var i in latestRequests )
			provider.InsertUserRequest( i.UserId, i.RequestTime );
	}
}