#nullable disable
using System.Collections.Immutable;
using System.Threading;
using EnterpriseWebLibrary.Collections;
using EnterpriseWebLibrary.Configuration;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess;

[ PublicAPI ]
public class DataAccessState {
	private static Func<DataAccessState> mainStateGetter;
	private static AsyncLocal<ImmutableStack<DataAccessState>> mainStateOverrideStack;
	private static bool useLongTimeouts;

	internal static void Init( Func<DataAccessState> mainDataAccessStateGetter, bool useLongTimeouts ) {
		mainStateGetter = mainDataAccessStateGetter;
		mainStateOverrideStack = new AsyncLocal<ImmutableStack<DataAccessState>>();
		DataAccessState.useLongTimeouts = useLongTimeouts;
	}

	/// <summary>
	/// Gets the current data-access state. Do not allow multiple threads to use the same state at the same time.
	/// </summary>
	public static DataAccessState Current {
		get {
			if( mainStateOverrideStack.Value is {} overrideStack && overrideStack.Any() )
				return overrideStack.Peek();
			if( mainStateGetter == null )
				throw new ApplicationException( "No main data-access state getter was specified during application initialization." );
			var mainDataAccessState = mainStateGetter();
			if( mainDataAccessState == null )
				throw new ApplicationException( "No main data-access state exists at this time." );
			return mainDataAccessState;
		}
	}

	private DBConnection primaryConnection;
	private readonly Dictionary<string, DBConnection> secondaryConnectionsByName = new();
	private readonly Action<DBConnection> connectionInitializer;

	private bool cacheEnabled;
	private Cache<string, object> cache;

	/// <summary>
	/// This should only be used for two purposes. First, to create objects that will be returned by the mainDataAccessStateGetter argument of
	/// GlobalInitializationOps.InitStatics. Second, to create supplemental data-access state objects, which you may need if you want to communicate with a
	/// database outside of the main transaction.
	/// </summary>
	/// <param name="databaseConnectionInitializer">A method that is called whenever a database connection is requested. Can be used to initialize the
	/// connection.</param>
	public DataAccessState( Action<DBConnection> databaseConnectionInitializer = null ) {
		connectionInitializer = databaseConnectionInitializer ?? ( connection => {} );
	}

	/// <summary>
	/// Gets the connection to the primary database.
	/// </summary>
	public DBConnection PrimaryDatabaseConnection =>
		initConnection(
			primaryConnection ?? ( primaryConnection = new DBConnection(
				                       ConfigurationStatics.InstallationConfiguration.PrimaryDatabaseInfo,
				                       useLongTimeouts: useLongTimeouts ) ) );

	/// <summary>
	/// Gets the connection to the specified secondary database.
	/// </summary>
	public DBConnection GetSecondaryDatabaseConnection( string databaseName ) {
		secondaryConnectionsByName.TryGetValue( databaseName, out var connection );
		if( connection == null )
			secondaryConnectionsByName.Add(
				databaseName,
				connection = new DBConnection(
					ConfigurationStatics.InstallationConfiguration.GetSecondaryDatabaseInfo( databaseName ),
					useLongTimeouts: useLongTimeouts ) );
		return initConnection( connection );
	}

	private DBConnection initConnection( DBConnection connection ) {
		connectionInitializer( connection );
		return connection;
	}

	/// <summary>
	/// Gets the cache value associated with the specified key. If no value exists, adds one by executing the specified creator function.
	/// </summary>
	public T GetCacheValue<T>( string key, Func<T> valueCreator ) {
		if( !cacheEnabled )
			return valueCreator();
		return (T)cache.GetOrAdd( key, () => valueCreator() );
	}

	/// <summary>
	/// Executes the specified method with the cache enabled. Supports nested calls by leaving the cache alone if it is already enabled. Do not modify data in
	/// the method; this could cause a stale cache and lead to data integrity problems!
	/// </summary>
	public void ExecuteWithCache( Action method ) {
		if( cacheEnabled )
			method();
		else {
			ResetCache();
			try {
				method();
			}
			finally {
				DisableCache();
			}
		}
	}

	/// <summary>
	/// Executes the specified method with the cache enabled. Supports nested calls by leaving the cache alone if it is already enabled. Do not modify data in
	/// the method; this could cause a stale cache and lead to data integrity problems!
	/// </summary>
	public T ExecuteWithCache<T>( Func<T> method ) {
		if( cacheEnabled )
			return method();
		ResetCache();
		try {
			return method();
		}
		finally {
			DisableCache();
		}
	}

	internal void ResetCache() {
		cacheEnabled = true;
		cache = new Cache<string, object>( false );
	}

	internal void DisableCache() {
		cacheEnabled = false;
	}

	/// <summary>
	/// Executes the specified method with this as the current data-access state. Only necessary when using supplemental data-access state objects.
	/// </summary>
	public void ExecuteWithThis( Action method ) {
		mainStateOverrideStack.Value = mainStateOverrideStack.Value is {} stack ? stack.Push( this ) : ImmutableStack.Create( this );
		try {
			method();
		}
		finally {
			mainStateOverrideStack.Value = mainStateOverrideStack.Value.Pop();
		}
	}

	/// <summary>
	/// Executes the specified method with this as the current data-access state. Only necessary when using supplemental data-access state objects.
	/// </summary>
	public T ExecuteWithThis<T>( Func<T> method ) {
		mainStateOverrideStack.Value = mainStateOverrideStack.Value is {} stack ? stack.Push( this ) : ImmutableStack.Create( this );
		try {
			return method();
		}
		finally {
			mainStateOverrideStack.Value = mainStateOverrideStack.Value.Pop();
		}
	}
}