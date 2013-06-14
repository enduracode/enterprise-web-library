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
		/// Gets the current main data-access state.
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

		// This can be removed when all types of EWL apps (Windows services, console apps, etc.) support the main data-access state.
		public static T GetCacheWebOnly<T>() where T: class, new() {
			return EwfApp.Instance != null && EwfApp.Instance.RequestState != null ? Main.GetCache<T>() : new T();
		}

		private DBConnection primaryConnection;
		private readonly Dictionary<string, DBConnection> secondaryConnectionsByName = new Dictionary<string, DBConnection>();
		private readonly Action<DBConnection, string> connectionInitializer;

		private bool cacheEnabled;
		private object cache;

		/// <summary>
		/// This should only be used to create objects that will be returned by the mainDataAccessStateGetter argument of AppTools.Init.
		/// </summary>
		public DataAccessState( Action<DBConnection, string> databaseConnectionInitializer ) {
			connectionInitializer = databaseConnectionInitializer;
		}

		/// <summary>
		/// Gets the connection to the primary database.
		/// </summary>
		public DBConnection PrimaryDatabaseConnection {
			get {
				// This can be removed after EWF [web] applications stop unconditionally grabbing the primary database connection.
				if( EwfApp.Instance != null && EwfApp.Instance.RequestState != null && !AppTools.DatabaseExists )
					return null;

				// Do not store the connection unless it is successfully initialized.
				return primaryConnection ?? ( primaryConnection = initConnection( new DBConnection( AppTools.InstallationConfiguration.PrimaryDatabaseInfo ), "" ) );
			}
		}

		/// <summary>
		/// Gets the connection to the specified secondary database.
		/// </summary>
		public DBConnection GetSecondaryDatabaseConnection( string databaseName ) {
			DBConnection connection;
			secondaryConnectionsByName.TryGetValue( databaseName, out connection );
			if( connection == null ) {
				// Do not store the connection unless it is successfully initialized.
				connection = initConnection( new DBConnection( AppTools.InstallationConfiguration.GetSecondaryDatabaseInfo( databaseName ) ), databaseName );
				secondaryConnectionsByName.Add( databaseName, connection );
			}
			return connection;
		}

		/// <summary>
		/// Returns the specified connection only if it is successfully initialized.
		/// </summary>
		private DBConnection initConnection( DBConnection connection, string secondaryDatabaseName ) {
			connectionInitializer( connection, secondaryDatabaseName );
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