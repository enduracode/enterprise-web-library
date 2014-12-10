using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;
using RedStapler.StandardLibrary.EnterpriseWebFramework;

namespace RedStapler.StandardLibrary.DataAccess {
	/// <summary>
	/// A collection of static methods related to data access.
	/// </summary>
	public static class DataAccessMethods {
		/// <summary>
		/// Retries the given operation in the case of a deadlock (when using pessimistic concurrency) or snapshot isolation error
		/// (when using optimistic concurrency) until it succeeds.
		/// NEVER EVER use this inside a transaction because SQL will automatically kill the transaction in the case
		/// of a deadlock, and it's unusable after that point.  In the case of Snapshots, the same snapshot from the beginning of the transaction
		/// will be used every single time, and so the operation will fail every single time.  So, again, DO NOT use this inside a transaction.
		/// When using this method, it is important that DeadLockableMethod does not produce any side effects
		/// (changing state, sending an email, etc.).  Undoing/resetting side effects at the beginning of the block of code
		/// to be retried is an acceptable approach here (there is no way to undo certain operations, such as sending an email, obviously).
		/// </summary>
		public static void RetryOnDeadlock( Action method ) {
			while( true ) {
				try {
					method();
					break;
				}
				catch( DbConcurrencyException ) {
					Thread.Sleep( 1000 );
				}
			}
		}

		/// <summary>
		/// Standard Library and Red Stapler Information System use only.
		/// </summary>
		public static Exception CreateDbConnectionException( DatabaseInfo databaseInfo, string action, Exception innerException ) {
			var generalMessage = "An exception occurred while " + action + " the " + GetDbName( databaseInfo ) + " database.";
			var customMessage = "";

			if( databaseInfo is SqlServerInfo ) {
				int? errorNumber = null;
				if( innerException is SqlException )
					errorNumber = ( innerException as SqlException ).Number;
				else {
					if( innerException.Message.Contains( "Could not open a connection to SQL Server [2]" ) )
						errorNumber = 2;
					if( innerException.Message.Contains( "Msg 4060" ) )
						errorNumber = 4060;

					if( !errorNumber.HasValue && innerException.Message.Contains( "Unable to complete login process due to delay in opening server connection" ) )
						customMessage = "Failed to connect to SQL Server.";
				}

				if( errorNumber.HasValue ) {
					// Failed to update database * because the database is read-only. This happens when you try to make a change to a live installation on a standby server.
					// NOTE: We may want to use a different type of exception. It's important that this gets displayed in the GUI for standby web apps.
					if( errorNumber == 3906 && Configuration.Machine.MachineConfiguration.GetIsStandbyServer() )
						return new DataModificationException( "You cannot make changes to standby versions of a system." );

					if( errorNumber.Value == 2 )
						customMessage = "Failed to connect to SQL Server. Make sure the services are running.";
					if( errorNumber.Value == 6005 )
						customMessage = "Failed to connect to SQL Server because the service is in the process of shutting down.";

					// -2 is the code for a timeout.
					if( errorNumber.Value == -2 )
						customMessage = "Failed to connect to SQL Server because of a connection timeout.";

					if( errorNumber.Value == 258 ) {
						customMessage =
							"Failed to connect to SQL Server because of a connection timeout. SQL Server may be in the process of doing something else that is preventing the connection for some reason. Please try again.";
					}

					if( errorNumber.Value == 4060 )
						customMessage = "The " + ( databaseInfo as SqlServerInfo ).Database + " database does not exist. You may need to execute an Update Data operation.";

					// We also handle this error at the command level.
					if( errorNumber.Value == 233 ) {
						customMessage =
							"The connection with the server has probably been severed. This likely happened because we did not disable connection pooling and a connection was taken from the pool that was no longer valid.";
					}

					if( !customMessage.Any() )
						generalMessage += " Error number: {0}.".FormatWith( errorNumber.Value );
				}
			}

			if( databaseInfo is MySqlInfo ) {
				if( innerException.Message.Contains( "Unable to connect to any of the specified MySQL hosts" ) )
					customMessage = "Failed to connect to MySQL. Make sure the service is running.";
				if( innerException.Message.Contains( "ERROR 2013" ) ) {
					customMessage =
						"Failed to connect to MySQL. MySQL may be in the process of doing something else that is preventing the connection for some reason. Please try again.";
				}
				if( innerException.Message.Contains( "Timeout expired" ) )
					customMessage = "Failed to connect to MySQL because of a connection timeout.";
			}

			if( databaseInfo is OracleInfo ) {
				if( innerException.Message.Contains( "ORA-12154" ) )
					customMessage = "Failed to connect to Oracle. There may be a problem with your network connection to the server.";
				if( innerException.Message.Contains( "ORA-12541" ) )
					customMessage = "Failed to connect to Oracle. Make sure the listener service is running.";
				if( innerException.Message.Contains( "ORA-12514" ) )
					customMessage = "Failed to connect to Oracle. Restart the main service and try again.";
				if( innerException.Message.Contains( "ORA-12528" ) )
					customMessage = "Failed to connect to Oracle. The service may be in the process of starting up.";
				if( new[] { "ORA-01033", "ORA-1033" }.Any( i => innerException.Message.Contains( i ) ) )
					// This error has only been seen on RLE machines when Dave Foss has manually shut down Oracle.
					customMessage = "Failed to connect to Oracle. Somebody may be in the process of manually shutting down Oracle.";
				if( innerException.Message.Contains( "ORA-12518" ) )
					// We received this once on the integration server while trying to build a system at the same time we were restarting the Oracle services.
					customMessage = "Failed to connect to Oracle. Oracle may be in the process of restarting.";
				if( innerException.Message.Contains( "ORA-12170" ) )
					// There are many causes of this error and it is difficult to be more specific in the message.
					customMessage = "Failed to connect to Oracle because of a connection timeout. Check the Oracle configuration on the machine and in this system.";
				if( new[] { "ORA-01017", "ORA-1017" }.Any( i => innerException.Message.Contains( i ) ) )
					customMessage = "Failed to connect to Oracle as " + ( databaseInfo as OracleInfo ).UserAndSchema + ". You may need to execute an Update Data operation.";
				if( new[] { "ORA-03114", "ORA-12571" }.Any( i => innerException.Message.Contains( i ) ) ) {
					customMessage =
						"Failed to connect to Oracle or connection to Oracle was lost. This should not happen often and may be caused by a bug in the data access components or the database.";
				}

				// This error only happens when using tnsnames and only MIT servers do this.
				if( innerException.Message.Contains( "ORA-12504" ) )
					customMessage = "Failed to connect to Oracle because of a problem in the tnsnames.ora file.";
			}

			return customMessage.Length > 0
				       ? new DbConnectionFailureException( generalMessage + " " + customMessage, innerException )
				       : new ApplicationException( generalMessage, innerException );
		}

		internal static string GetDbName( DatabaseInfo databaseInfo ) {
			return databaseInfo.SecondaryDatabaseName.Length == 0 ? "primary" : databaseInfo.SecondaryDatabaseName + " secondary";
		}

		internal static int CompareCommandConditionTypes( InlineDbCommandCondition x, InlineDbCommandCondition y ) {
			return StringComparer.InvariantCulture.Compare( x.GetType().Name, y.GetType().Name );
		}
	}
}