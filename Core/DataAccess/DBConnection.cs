using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DataAccess.RevisionHistory;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Humanizer;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace EnterpriseWebLibrary.DataAccess {
	/// <summary>
	/// Provides a connection to a database.  Capable of nested transactions.
	/// </summary>
	public class DBConnection {
		private const string saveName = "child";

		private readonly DatabaseInfo databaseInfo;
		private readonly ProfiledDbConnection cn;

		// transaction-related fields
		private int nestLevel;
		private ProfiledDbTransaction tx;
		private DbTransaction innerTx;
		private List<Func<string>> commitTimeValidationMethods;
		private int? userTransactionId;

		// This is true only if SQL returned an error indicating rollback outside a transaction at the database level (error 3903).
		private bool transactionDead;

		/// <summary>
		/// Creates a database connection based on the specified database information object.
		/// </summary>
		internal DBConnection( DatabaseInfo databaseInfo ) {
			this.databaseInfo = databaseInfo;

			// Build the connection string.
			string connectionString;
			if( databaseInfo is SqlServerInfo ) {
				var sqlServerInfo = databaseInfo as SqlServerInfo;
				connectionString = "Data Source=" + ( sqlServerInfo.Server ?? "(local)" );
				if( sqlServerInfo.LoginName != null ) {
					connectionString += "; User ID=" + sqlServerInfo.LoginName;
					connectionString += "; Password='{0}'".FormatWith( sqlServerInfo.Password );
				}
				else
					connectionString += "; Integrated Security=SSPI";
				connectionString += "; Initial Catalog=" + sqlServerInfo.Database;
				if( !sqlServerInfo.SupportsConnectionPooling )
					connectionString += "; Pooling=false";
			}
			else if( databaseInfo is MySqlInfo ) {
				var mySqlInfo = databaseInfo as MySqlInfo;
				connectionString = "Host=localhost; User Id=root; Password=password; Initial Catalog=" + mySqlInfo.Database;
				if( !mySqlInfo.SupportsConnectionPooling )
					connectionString += "; Pooling=false";
			}
			else if( databaseInfo is OracleInfo ) {
				var oracleInfo = databaseInfo as OracleInfo;
				connectionString = "Data Source=" + oracleInfo.DataSource + "; User Id=" + oracleInfo.UserAndSchema + "; Password=" + oracleInfo.Password +
				                   ( oracleInfo.UserAndSchema == "sys" ? "; DBA Privilege=SYSDBA" : "" );
				if( !oracleInfo.SupportsConnectionPooling )
					connectionString = StringTools.ConcatenateWithDelimiter( "; ", connectionString, "Pooling=false" );
			}
			else
				throw new ApplicationException( "Invalid database information object type." );

			cn = new ProfiledDbConnection( databaseInfo.CreateConnection( connectionString ), MiniProfiler.Current );
		}

		/// <summary>
		/// This should only be used by internal tools.
		/// </summary>
		public DatabaseInfo DatabaseInfo { get { return databaseInfo; } }

		/// <summary>
		/// Opens the connection, executes the specified method, and closes the connection.
		/// </summary>
		public void ExecuteWithConnectionOpen( Action method ) {
			Open();
			try {
				method();
			}
			finally {
				Close();
			}
		}

		/// <summary>
		/// Opens the connection, executes the specified method, and closes the connection.
		/// </summary>
		public T ExecuteWithConnectionOpen<T>( Func<T> method ) {
			Open();
			try {
				return method();
			}
			finally {
				Close();
			}
		}

		/// <summary>
		/// Opens the database connection.
		/// </summary>
		public void Open() {
			try {
				// NOTE: On 10-11 April 2011 this line appears to have blocked for over 22 hours, for an Oracle connection. Around the same time, some other executions of
				// this line for the same database produced ORA-257 errors, indicating that the blocking may have occurred because there was not enough disk space to
				// store the redo log. We don't want this line to ever block for that amount of time again. Setting a connection timeout in the connection string will
				// probably not fix the issue since the OracleConnection default is documented as 15 seconds, and that clearly didn't work. We should investigate ways
				// to abort this line ourselves if it hangs.
				cn.Open();

				if( databaseInfo is OracleInfo ) {
					// Make Oracle case-insensitive, like SQL Server.
					if( ( databaseInfo as OracleInfo ).SupportsLinguisticIndexes ) {
						executeText( "ALTER SESSION SET NLS_COMP = LINGUISTIC" );
						executeText( "ALTER SESSION SET NLS_SORT = BINARY_CI" );
					}

					// This tells Oracle that times passed in should be interpreted as EST or EDT, depending on the time of the year. By default, the Oracle client uses a
					// "-05:00" zone with no daylight savings time, and this is not what we want.
					executeText( "ALTER SESSION SET TIME_ZONE = 'US/Eastern'" );

					// This makes Oracle blow up if times during a "fall back" hour, when the eastern US switches from EDT to EST, are passed in. These times are ambiguous.
					executeText( "ALTER SESSION SET ERROR_ON_OVERLAP_TIME = TRUE" );
				}
			}
			catch( Exception e ) {
				throw createConnectionException( "opening a connection to", e );
			}
		}

		/// <summary>
		/// Closes the database connection.
		/// </summary>
		public void Close() {
			try {
				cn.Close();
			}
			catch( Exception e ) {
				throw createConnectionException( "closing a connection to", e );
			}
		}

		/// <summary>
		/// Executes the given block of code inside a transaction using the given database connection.  Does not
		/// create, open, or close a database connection.
		/// This overload allows you to throw a DoNotCommitException, which will gracefully not commit the transaction.
		/// </summary>
		public void ExecuteInTransaction( Action method ) {
			BeginTransaction();
			try {
				method();
				CommitTransaction();
			}
			catch( DoNotCommitException ) {
				RollbackTransaction();
			}
			catch {
				RollbackTransaction();
				throw;
			}
		}

		/// <summary>
		/// Executes the given block of code inside a transaction using the given database connection.  Does not
		/// create, open, or close a database connection.
		/// This overload does not handle DoNotCommitExceptions for you.
		/// </summary>
		public T ExecuteInTransaction<T>( Func<T> method ) {
			BeginTransaction();
			try {
				var result = method();
				CommitTransaction();
				return result;
			}
			catch {
				RollbackTransaction();
				throw;
			}
		}

		/// <summary>
		/// Begins a new transaction.
		/// </summary>
		public void BeginTransaction() {
			try {
				if( nestLevel == 0 ) {
					transactionDead = false;

					if( databaseInfo is SqlServerInfo )
						innerTx = cn.WrappedConnection.BeginTransaction( IsolationLevel.Snapshot );
					else if( databaseInfo is OracleInfo )
						innerTx = cn.WrappedConnection.BeginTransaction( IsolationLevel.Serializable );
					else
						innerTx = cn.WrappedConnection.BeginTransaction();
					tx = new ProfiledDbTransaction( innerTx, cn );

					commitTimeValidationMethods = new List<Func<string>>();
				}
				else
					saveTransaction();
				nestLevel++;
			}
			catch( Exception e ) {
				throw createConnectionException( "beginning a transaction for", e );
			}
		}

		private void saveTransaction() {
			if( databaseInfo is SqlServerInfo )
				( (SqlTransaction)innerTx ).Save( saveName + nestLevel );
			else if( databaseInfo is MySqlInfo )
				executeText( "SAVEPOINT {0}".FormatWith( saveName + nestLevel ) );
			else {
				var saveMethod = innerTx.GetType().GetMethod( "Save" );
				saveMethod.Invoke( innerTx, new object[] { saveName + nestLevel } );
			}
		}

		/// <summary>
		/// Rolls back all commands since the last call to BeginTransaction.
		/// </summary>
		public void RollbackTransaction() {
			try {
				if( nestLevel == 0 )
					throw new ApplicationException( "Cannot rollback without a matching begin." );

				nestLevel--;
				try {
					if( nestLevel == 0 ) {
						if( !transactionDead )
							tx.Rollback();
						resetTransactionFields();
					}
					else {
						if( !transactionDead ) {
							if( databaseInfo is SqlServerInfo )
								( (SqlTransaction)innerTx ).Rollback( saveName + nestLevel );
							else if( databaseInfo is MySqlInfo )
								executeText( "ROLLBACK TO SAVEPOINT {0}".FormatWith( saveName + nestLevel ) );
							else {
								var rollbackMethod = innerTx.GetType().GetMethod( "Rollback", new[] { typeof( string ) } );
								rollbackMethod.Invoke( innerTx, new object[] { saveName + nestLevel } );
							}
						}
					}
				}
				catch( SqlException e ) {
					//Explanation of why we need this hack:
					//SQL Server will sometimes rollback a transaction on its own when it
					//encounters a "serious" error. These seem to include any kind of command param
					//error or any of our trigger errors with severity of 11 or higher. When
					//we detect the error, we attempt to rollback the transaction. But if SQL Server
					//has already done that, we will get an error 3903. Therefore, we catch it below
					//to make sure the RollbackTransaction call (this method) does not throw
					//an exception that blocks out the real exception that occurred.

					// We set transactionDead to true so that we do not accumulate additional
					// exceptions while the client attempts to rollback all nest levels
					if( e.Number == 3903 )
						transactionDead = true;
					else
						throw;
				}
				catch( InvalidOperationException ) {
					// This means that the transaction had already been rolled back (by SQL, due to high error severity, as above).
					transactionDead = true;
				}
			}
			catch( Exception e ) {
				throw createConnectionException( "rolling back a transaction for", e );
			}
		}

		private void executeText( string commandText ) {
			var command = databaseInfo.CreateCommand();
			command.CommandText = commandText;
			ExecuteNonQueryCommand( command );
		}

		/// <summary>
		/// Executes all commit-time validation methods that are currently in the connection. They will not be executed again when the transaction is committed. Do
		/// not call this method when the transaction is in a state such that additional modifications may need to execute before all validation methods can be
		/// successful.
		/// </summary>
		internal void PreExecuteCommitTimeValidationMethods() {
			executeCommitTimeValidationMethods();
			commitTimeValidationMethods.Clear();
		}

		/// <summary>
		/// Commits all commands since the last call to BeginTransaction.
		/// </summary>
		public void CommitTransaction() {
			try {
				if( nestLevel == 0 )
					throw new ApplicationException( "Cannot commit without a matching begin." );
				if( nestLevel == 1 ) {
					executeCommitTimeValidationMethods();
					tx.Commit();
					resetTransactionFields();
				}
				nestLevel--;
			}
			catch( Exception e ) {
				throw createConnectionException( "committing a transaction for", e );
			}
		}

		/// <summary>
		/// Throws an exception if any commit-time validation methods return error messages. This usually means business rules were violated.
		/// </summary>
		private void executeCommitTimeValidationMethods() {
			var errors = new List<string>();
			foreach( var method in commitTimeValidationMethods ) {
				var result = method();
				if( result.Length > 0 )
					errors.Add( result );
			}
			if( errors.Any() )
				throw new DataModificationException( errors.ToArray() );
		}

		private void resetTransactionFields() {
			userTransactionId = null;
			commitTimeValidationMethods = null;
			tx = null;
			innerTx = null;
		}

		private Exception createConnectionException( string action, Exception innerException ) {
			return DataAccessMethods.CreateDbConnectionException( databaseInfo, action, innerException );
		}

		/// <summary>
		/// Execute a command and return number of rows affected.
		/// </summary>
		/// <param name="cmd">Command to execute</param>
		/// <returns>Number of rows affected.</returns>
		public int ExecuteNonQueryCommand( DbCommand cmd ) {
			try {
				cmd.Connection = cn;
				if( tx != null )
					cmd.Transaction = tx;
				return cmd.ExecuteNonQuery();
			}
			catch( Exception e ) {
				throw createCommandException( cmd, e );
			}
		}

		/// <summary>
		/// Executes a scalar command and returns the first column of the first row
		/// in the query result.
		/// </summary>
		/// <param name="cmd">Command to execute</param>
		/// <returns>First column of the first row returned by the query. Null if there were no results.</returns>
		public object ExecuteScalarCommand( DbCommand cmd ) {
			try {
				cmd.Connection = cn;
				if( tx != null )
					cmd.Transaction = tx;
				return cmd.ExecuteScalar();
			}
			catch( Exception e ) {
				throw createCommandException( cmd, e );
			}
		}

		/// <summary>
		/// Executes the specified command to get a data reader and then executes the specified method with the reader.
		/// </summary>
		public void ExecuteReaderCommand( DbCommand cmd, Action<DbDataReader> readerMethod ) {
			executeReaderCommand( cmd, CommandBehavior.Default, readerMethod );
		}

		/// <summary>
		/// Executes the specified command with SchemaOnly behavior to get a data reader and then executes the specified method with the reader.
		/// </summary>
		public void ExecuteReaderCommandWithSchemaOnlyBehavior( DbCommand cmd, Action<DbDataReader> readerMethod ) {
			executeReaderCommand( cmd, CommandBehavior.SchemaOnly, readerMethod );
		}

		/// <summary>
		/// Executes the specified command with SchemaOnly and KeyInfo behavior to get a data reader and then executes the specified method with the reader.
		/// </summary>
		public void ExecuteReaderCommandWithKeyInfoBehavior( DbCommand cmd, Action<DbDataReader> readerMethod ) {
			executeReaderCommand( cmd, CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo, readerMethod );
		}

		private void executeReaderCommand( DbCommand command, CommandBehavior behavior, Action<DbDataReader> readerMethod ) {
			try {
				command.Connection = cn;
				if( tx != null )
					command.Transaction = tx;
				using( var reader = command.ExecuteReader( behavior ) )
					readerMethod( reader );
			}
			catch( Exception e ) {
				throw createCommandException( command, e );
			}
		}

		private Exception createCommandException( DbCommand command, Exception innerException ) {
			if( databaseInfo is SqlServerInfo && innerException is SqlException ) {
				var errorNumber = ( (SqlException)innerException ).Number;

				// 1205 is the code for deadlock; 3960 is the code for a snapshot optimistic concurrency error; 3961 is the code for a snapshot concurrency error due to
				// a DDL statement in another transaction.
				if( errorNumber == 1205 || errorNumber == 3960 || errorNumber == 3961 )
					return new DbConcurrencyException( getCommandExceptionMessage( command, "A concurrency error occurred." ), innerException );

				// Failed to update database * because the database is read-only. This happens when you try to make a change to a live installation on a standby server.
				if( errorNumber == 3906 && Configuration.ConfigurationStatics.MachineIsStandbyServer )
					return DataAccessMethods.CreateStandbyServerModificationException();

				// -2 is the code for a timeout.
				if( errorNumber == -2 )
					return new DbCommandTimeoutException( getCommandExceptionMessage( command, "A command timeout occurred." ), innerException );

				// We also handle this error at the connection level.
				if( errorNumber == 233 ) {
					const string m =
						"The connection with the server has probably been severed. This likely happened because we did not disable connection pooling and a connection was taken from the pool that was no longer valid.";
					return new ApplicationException( getCommandExceptionMessage( command, m ), innerException );
				}

				return new ApplicationException( getCommandExceptionMessage( command, "Error number: " + errorNumber + "." ), innerException );
			}

			if( databaseInfo is OracleInfo ) {
				// ORA-00060 is the code for deadlock. ORA-08177 happens when we attempt to update a row that has changed since the transaction began.
				if( new[] { "ORA-00060", "ORA-08177" }.Any( i => innerException.Message.Contains( i ) ) )
					return new DbConcurrencyException( getCommandExceptionMessage( command, "A concurrency error occurred." ), innerException );

				// This has happened on RLE servers when Dave Foss has manually shut down Oracle.
				if( innerException.Message.Contains( "ORA-01109" ) )
					return new DbConnectionFailureException( getCommandExceptionMessage( command, "Failed to connect to Oracle." ), innerException );
			}

			return new ApplicationException( getCommandExceptionMessage( command, "" ), innerException );
		}

		private string getCommandExceptionMessage( DbCommand command, string customMessage ) {
			using( var sw = new StringWriter() ) {
				sw.WriteLine(
					"Failed to execute a command against the " + DataAccessMethods.GetDbName( databaseInfo ) + " database. " + customMessage + " Command details:" );
				sw.WriteLine();
				sw.WriteLine( "Type: " + command.CommandType );
				sw.WriteLine( "Text: " + command.CommandText );
				sw.WriteLine();
				sw.WriteLine( "Parameters:" );
				foreach( DbParameter p in command.Parameters )
					sw.WriteLine( p.ParameterName + ": " + p.Value );
				return sw.ToString();
			}
		}

		/// <summary>
		/// Adds a commit-time validation method to the connection. These will be executed immediately before the transaction is committed, in the order they were
		/// added.
		/// </summary>
		public void AddCommitTimeValidationMethod( Func<string> method ) {
			if( nestLevel == 0 )
				throw new ApplicationException( "A commit-time validation method can only be added during a database transaction." );
			commitTimeValidationMethods.Add( method );
		}

		/// <summary>
		/// Returns the user transaction ID for the current database connection. Inserts a new user transaction row if necessary.
		/// Do not cache this value for any reason.
		/// </summary>
		public int GetUserTransactionId() {
			if( nestLevel == 0 )
				throw new ApplicationException( "A user transaction can only be created or retrieved during a database transaction." );
			if( !userTransactionId.HasValue ) {
				int? userId = null;
				if( AppTools.User != null )
					userId = AppTools.User.UserId;

				var revisionHistorySetup = RevisionHistoryStatics.SystemProvider;
				userTransactionId = revisionHistorySetup.GetNextMainSequenceValue();
				revisionHistorySetup.InsertUserTransaction( userTransactionId.Value, DateTime.Now, userId );
			}
			return userTransactionId.Value;
		}

		/// <summary>
		/// Returns true if the specified user transaction ID matches the current user transaction ID.
		/// </summary>
		public bool UserTransactionIsCurrent( int userTransactionId ) {
			return userTransactionId == this.userTransactionId;
		}

		/// <summary>
		/// Returns schema information about the database. For Red Stapler Information System use only.
		/// </summary>
		public DataTable GetSchema( string collectionName, params string[] restrictionValues ) {
			return cn.GetSchema( collectionName, restrictionValues );
		}
	}
}