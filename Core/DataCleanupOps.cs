using System.ComponentModel;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;

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
	}

	private static void cleanUpUserRequests() {
		var provider = UserManagementStatics.SystemProvider;
		var latestRequests = provider.GetUserRequests().GroupBy( i => i.UserId, ( _, requests ) => requests.MaxBy( i => i.RequestTime )! ).Materialize();
		provider.ClearUserRequests();
		foreach( var i in latestRequests )
			provider.InsertUserRequest( i.UserId, i.RequestTime );
	}
}