namespace EnterpriseWebLibrary.DataAccess;

/// <summary>
/// Manages transactions and cleanup for automatically-opened database connections in a data-access state object.
/// </summary>
public class AutomaticDatabaseConnectionManager {
	private static Func<AutomaticDatabaseConnectionManager>? currentManagerGetter;

	internal static void Init( Func<AutomaticDatabaseConnectionManager>? currentManagerGetter ) {
		AutomaticDatabaseConnectionManager.currentManagerGetter = currentManagerGetter;
	}

	/// <summary>
	/// Gets the current connection manager.
	/// </summary>
	internal static AutomaticDatabaseConnectionManager Current => currentManagerGetter!();

	/// <summary>
	/// Queues the specified non-transactional modification method to be executed after database transactions are committed.
	/// </summary>
	public static void AddNonTransactionalModificationMethod( Action modificationMethod ) {
		var current = Current;
		if( !current.inModificationTransaction )
			throw new Exception( "You can only queue a non-transactional modification method at a time when normal modifications are enabled." );
		current.nonTransactionalModificationMethods.Add( modificationMethod );
	}

	private readonly DataAccessState dataAccessState;
	private bool primaryDatabaseConnectionInitialized;
	private readonly List<string> secondaryDatabasesWithInitializedConnections = new();
	private readonly List<Action> nonTransactionalModificationMethods = new();
	private bool modTransactionIncludesPrimaryDatabase;
	private int? modTransactionSecondaryDatabaseCount;
	private int modTransactionNonTransactionalMethodIndex;
	private bool transactionsMarkedForRollback;

	internal AutomaticDatabaseConnectionManager() {
		dataAccessState = new DataAccessState(
			databaseConnectionInitializer: connection => {
				if( connection.DatabaseInfo.SecondaryDatabaseName.Any()
					    ? secondaryDatabasesWithInitializedConnections.Contains( connection.DatabaseInfo.SecondaryDatabaseName )
					    : primaryDatabaseConnectionInitialized )
					return;

				connection.Open();
				if( DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
					connection.BeginTransaction();

				if( connection.DatabaseInfo.SecondaryDatabaseName.Any() )
					secondaryDatabasesWithInitializedConnections.Add( connection.DatabaseInfo.SecondaryDatabaseName );
				else
					primaryDatabaseConnectionInitialized = true;
			} );
	}

	internal DataAccessState DataAccessState => dataAccessState;

	internal void ExecuteWithModificationsEnabled( Action modificationMethod ) {
		EnableModifications();
		dataAccessState.DisableCache();
		try {
			modificationMethod();
		}
		catch {
			RollbackModifications();
			throw;
		}
		finally {
			dataAccessState.ResetCache();
		}
		CommitModifications();
	}

	internal void EnableModifications() {
		if( inModificationTransaction )
			throw new InvalidOperationException();

		foreach( var connection in getConnectionsWithTransaction( false ) ) {
			connection.BeginTransaction( createSavepointIfAlreadyInTransaction: true );
			modTransactionIncludesPrimaryDatabase = true;
		}

		var secondaryConnections = getConnectionsWithTransaction( true ).Materialize();
		foreach( var connection in secondaryConnections )
			connection.BeginTransaction( createSavepointIfAlreadyInTransaction: true );
		modTransactionSecondaryDatabaseCount = secondaryConnections.Count;

		modTransactionNonTransactionalMethodIndex = nonTransactionalModificationMethods.Count;
	}

	internal void PreExecuteCommitTimeValidationMethods() {
		foreach( var connection in getConnectionsWithTransaction( false ).Concat( getConnectionsWithTransaction( true ) ) )
			connection.PreExecuteCommitTimeValidationMethods();
	}

	internal void CommitModifications() {
		if( !inModificationTransaction )
			throw new InvalidOperationException();

		foreach( var connection in getConnectionsWithTransaction( false )
			        .Where( _ => modTransactionIncludesPrimaryDatabase )
			        .Concat( getConnectionsWithTransaction( true ).Take( modTransactionSecondaryDatabaseCount!.Value ) ) )
			connection.CommitTransaction();

		modTransactionIncludesPrimaryDatabase = false;
		modTransactionSecondaryDatabaseCount = null;
	}

	internal void RollbackModifications() {
		if( !inModificationTransaction )
			throw new InvalidOperationException();

		transactionsMarkedForRollback = true;

		foreach( var connection in getConnectionsWithTransaction( false ) )
			if( modTransactionIncludesPrimaryDatabase )
				connection.RollbackTransaction();
			else
				cleanUpConnection( connection );

		var secondaryConnections = getConnectionsWithTransaction( true ).Materialize();
		foreach( var connection in secondaryConnections.Take( modTransactionSecondaryDatabaseCount!.Value ) )
			connection.RollbackTransaction();
		foreach( var connection in secondaryConnections.Skip( modTransactionSecondaryDatabaseCount.Value ) )
			cleanUpConnection( connection );

		transactionsMarkedForRollback = false;

		nonTransactionalModificationMethods.RemoveRange(
			modTransactionNonTransactionalMethodIndex,
			nonTransactionalModificationMethods.Count - modTransactionNonTransactionalMethodIndex );

		modTransactionIncludesPrimaryDatabase = false;
		modTransactionSecondaryDatabaseCount = null;
	}

	private IEnumerable<DatabaseConnection> getConnectionsWithTransaction( bool forSecondaryDatabases ) {
		if( forSecondaryDatabases ) {
			foreach( var connection in secondaryDatabasesWithInitializedConnections.Select( dataAccessState.GetSecondaryDatabaseConnection ) )
				if( DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
					yield return connection;
		}
		else if( primaryDatabaseConnectionInitialized ) {
			var connection = dataAccessState.PrimaryDatabaseConnection;
			if( DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
				yield return connection;
		}
	}

	internal void CommitTransactionsAndExecuteNonTransactionalModificationMethods( bool cacheEnabled ) {
		CleanUpConnectionsAndExecuteNonTransactionalModificationMethods( cacheEnabled );
	}

	internal void RollbackTransactions( bool cacheEnabled ) {
		transactionsMarkedForRollback = true;
		CleanUpConnectionsAndExecuteNonTransactionalModificationMethods( cacheEnabled );
	}

	internal void CleanUpConnectionsAndExecuteNonTransactionalModificationMethods( bool cacheEnabled, bool skipNonTransactionalModificationMethods = false ) {
		if( inModificationTransaction )
			throw new InvalidOperationException();

		var methods = new List<Action>();
		if( primaryDatabaseConnectionInitialized )
			methods.Add( () => cleanUpConnection( dataAccessState.PrimaryDatabaseConnection ) );
		foreach( var databaseName in secondaryDatabasesWithInitializedConnections )
			methods.Add( () => cleanUpConnection( dataAccessState.GetSecondaryDatabaseConnection( databaseName ) ) );
		methods.Add(
			() => {
				try {
					if( !skipNonTransactionalModificationMethods && !transactionsMarkedForRollback ) {
						if( cacheEnabled ) {
							dataAccessState.DisableCache();
							try {
								foreach( var i in nonTransactionalModificationMethods )
									i();
							}
							finally {
								dataAccessState.ResetCache();
							}
						}
						else
							foreach( var i in nonTransactionalModificationMethods )
								i();
					}
				}
				finally {
					nonTransactionalModificationMethods.Clear();
				}
			} );
		EwlStatics.CallEveryMethod( methods.ToArray() );
		transactionsMarkedForRollback = false;
	}

	private void cleanUpConnection( DatabaseConnection connection ) {
		// Keep the connection initialized during cleanup to accommodate commit-time validation methods.
		try {
			try {
				if( !DataAccessStatics.DatabaseShouldHaveAutomaticTransactions( connection.DatabaseInfo ) )
					return;

				try {
					if( !transactionsMarkedForRollback )
						connection.CommitTransaction();
				}
				catch {
					// Modifying this boolean here means that the order in which connections are cleaned up matters. Not modifying it here means
					// possibly committing things to secondary databases that shouldn't be committed. We've decided that the primary connection
					// is the most likely to have these errors, and is cleaned up first, so modifying this boolean here will yield the best results
					// until we implement a true distributed transaction model with two-phase commit.
					transactionsMarkedForRollback = true;

					throw;
				}
				finally {
					if( transactionsMarkedForRollback )
						connection.RollbackTransaction();
				}
			}
			finally {
				connection.Close();
			}
		}
		finally {
			if( connection.DatabaseInfo.SecondaryDatabaseName.Any() )
				secondaryDatabasesWithInitializedConnections.Remove( connection.DatabaseInfo.SecondaryDatabaseName );
			else
				primaryDatabaseConnectionInitialized = false;
		}
	}

	private bool inModificationTransaction => modTransactionSecondaryDatabaseCount.HasValue;
}