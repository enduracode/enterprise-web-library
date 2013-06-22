using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace RedStapler.StandardLibrary.DataAccess {
	public class DataAccessState {
		private static Func<DataAccessState> getter;

		internal static void Init( Func<DataAccessState> mainDataAccessStateGetter ) {
			getter = mainDataAccessStateGetter;
		}

		/// <summary>
		/// Gets the current main data-access state. In EWF web applications, this will throw an exception when called from the worker threads used by parallel
		/// programming tools such as PLINQ and the Task Parallel Library. While it would be possible for us to create an implementation that returns a separate
		/// data-access state object for each thread, we've decided against it because we feel it's a leaky abstraction. Each thread would silently have its own
		/// database transactions and its own cache, and not being aware of this fact could be extremely frustrating. Therefore we require developers to manually
		/// create data-access state objects in worker threads.
		/// </summary>
		public static DataAccessState Main {
			get {
				if( getter == null )
					throw new ApplicationException( "No main data-access state getter was specified during application initialization." );
				var mainDataAccessState = getter();
				if( mainDataAccessState == null )
					throw new ApplicationException( "No main data-access state exists at this time." );
				return mainDataAccessState;
			}
		}

		private DBConnection primaryConnection;
		private readonly Dictionary<string, DBConnection> secondaryConnectionsByName = new Dictionary<string, DBConnection>();
		private readonly Action<DBConnection> connectionInitializer;

		private bool cacheEnabled;
		private object cache;

		/// <summary>
		/// This should only be used for two purposes. First, to create objects that will be returned by the mainDataAccessStateGetter argument of AppTools.Init.
		/// Second, to create supplemental data-access state objects, which you may need if you want to communicate with a database outside of the main transaction.
		/// When using a supplemental data-access state, don't forget to actually pass it to the data-access methods you call.
		/// </summary>
		/// <param name="databaseConnectionInitializer">A method that is called whenever a database connection is requested. Can be used to initialize the
		/// connection.</param>
		public DataAccessState( Action<DBConnection> databaseConnectionInitializer = null ) {
			connectionInitializer = databaseConnectionInitializer ?? ( connection => { } );
		}

		/// <summary>
		/// Gets the connection to the primary database.
		/// </summary>
		public DBConnection PrimaryDatabaseConnection {
			get {
				// This can be removed after EWF [web] applications stop unconditionally grabbing the primary database connection.
				if( EwfApp.Instance != null && EwfApp.Instance.RequestState != null && !AppTools.DatabaseExists )
					return null;

				return initConnection( primaryConnection ?? ( primaryConnection = new DBConnection( AppTools.InstallationConfiguration.PrimaryDatabaseInfo ) ) );
			}
		}

		/// <summary>
		/// Gets the connection to the specified secondary database.
		/// </summary>
		public DBConnection GetSecondaryDatabaseConnection( string databaseName ) {
			DBConnection connection;
			secondaryConnectionsByName.TryGetValue( databaseName, out connection );
			if( connection == null )
				secondaryConnectionsByName.Add( databaseName, connection = new DBConnection( AppTools.InstallationConfiguration.GetSecondaryDatabaseInfo( databaseName ) ) );
			return initConnection( connection );
		}

		private DBConnection initConnection( DBConnection connection ) {
			connectionInitializer( connection );
			return connection;
		}

		/// <summary>
		/// Gets the cache object.
		/// </summary>
		public T GetCache<T>() where T: class, new() {
			if( !cacheEnabled )
				return new T();

			if( cache == null )
				cache = new T();
			return (T)cache;
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

		internal void ResetCache() {
			cacheEnabled = true;
			cache = null;
		}

		internal void DisableCache() {
			cacheEnabled = false;
		}
	}
}