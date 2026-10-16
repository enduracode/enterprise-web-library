using System.Data;
using System.Data.Common;
using System.Globalization;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.ExternalFunctionality;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators.MySql;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.MySql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace EnterpriseWebLibrary.MySql;

public class MySqlProvider: ExternalMySqlProvider {
	private class Processor: MySql8Processor {
		private bool sessionInitialized;

		public Processor(
			MySqlDbFactory factory, MySql8Generator generator, ILogger<MySql8Processor> logger, IOptionsSnapshot<ProcessorOptions> options,
			IConnectionStringAccessor connectionStringAccessor ): base( factory, generator, logger, options, connectionStringAccessor ) {}

		protected override void EnsureConnectionIsOpen() {
			if( Connection.State != ConnectionState.Open )
				sessionInitialized = false;

			base.EnsureConnectionIsOpen();

			if( sessionInitialized )
				return;

			using var command = CreateCommand( MySqlInfo.SqlModeInitializationStatement );
			command.ExecuteNonQuery();
			sessionInitialized = true;
		}
	}

	private class Quoter: MySqlQuoter {
		// Mirrors GenericQuoter.QuoteValue in FluentMigrator 8.0.1, bypassing MySqlQuoter’s backslash doubling. Compare dispatch when upgrading FM.
		public override string QuoteValue( object value ) =>
			value switch
				{
					null or DBNull => FormatNull(),
					string text => FormatNationalString( text ),
					NonUnicodeString text => FormatAnsiString( text.ToString() ),
					char character => FormatChar( character ),
					bool boolean => FormatBool( boolean ),
					Guid guid => FormatGuid( guid ),
					DateTime dateTime => FormatDateTime( dateTime ),
					DateTimeOffset dateTimeOffset => FormatDateTimeOffset( dateTimeOffset ),
					double number => number.ToString( CultureInfo.InvariantCulture ),
					float number => number.ToString( CultureInfo.InvariantCulture ),
					SystemMethods method => FormatSystemMethods( method ),
					Enum enumeration => FormatEnum( enumeration ),
					decimal number => number.ToString( CultureInfo.InvariantCulture ),
					int number => number.ToString( CultureInfo.InvariantCulture ),
					RawSql sql => sql.Value,
					byte[] bytes => FormatByteArray( bytes ),
					TimeSpan timeSpan => FromTimeSpan( timeSpan ),
					_ => value.ToString()!
				};

		public override string FormatChar( char value ) => FormatAnsiString( value.ToString() );
	}

	string ExternalMySqlProvider.GetConnectionString(
		string server, string userId, string password, string database, bool supportsConnectionPooling, uint timeout ) {
		var builder = new MySqlConnectionStringBuilder();

		builder.Server = server.Length > 0 ? server : "localhost";
		builder.UserID = userId;
		builder.Password = password;
		if( server.Length > 0 )
			builder.SslMode = MySqlSslMode.VerifyFull;
		builder.Database = database;
		if( !supportsConnectionPooling )
			builder.Pooling = false;
		builder.ConnectionTimeout = timeout;
		builder.NoBackslashEscapes = true;

		return builder.ConnectionString;
	}

	DbProviderFactory ExternalMySqlProvider.GetDbProviderFactory() => MySqlConnectorFactory.Instance;

	Exception ExternalMySqlProvider.CreateCommandException( Exception originalException, Func<string, string> messageGetter ) {
		if( originalException is not MySqlException original )
			return new Exception( messageGetter( "" ), originalException );

		if( original.ErrorCode is MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout )
			return new DbConcurrencyException( messageGetter( "A concurrency error occurred." ), original );

		if( original.ErrorCode == MySqlErrorCode.CommandTimeoutExpired )
			return new DbCommandTimeoutException( messageGetter( "A command timeout occurred." ), original );

		if( original.ErrorCode is MySqlErrorCode.ColumnCannotBeNull or MySqlErrorCode.DuplicateKeyEntry or MySqlErrorCode.NoReferencedRow
			    or MySqlErrorCode.NoReferencedRow2 or MySqlErrorCode.RowIsReferenced or MySqlErrorCode.RowIsReferenced2 or MySqlErrorCode.WarningDataOutOfRange
			    or MySqlErrorCode.DataTooLong or MySqlErrorCode.DataOutOfRange ||
		    original.Number == 3819 /* CHECK constraint violation, not named in MySqlErrorCode */ )
			return new DbCommandExecutionException( messageGetter( "Error number: " + original.Number + "." ), original );

		return new Exception( messageGetter( "Error number: " + original.Number + "." ), original );
	}

	bool ExternalMySqlProvider.RollbackExceptionIsLostTransaction( Exception exception ) =>
		exception is MySqlException { ErrorCode: MySqlErrorCode.StoredProcedureDoesNotExist /* confusing name, but means missing savepoint in this context */ };

	void ExternalMySqlProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddMySql8();
		builder.Services.AddScoped<MySql8Processor, Processor>();
		builder.Services.AddScoped<MySqlQuoter, Quoter>();
	}
}