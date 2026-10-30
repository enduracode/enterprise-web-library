using System.Data;
using System.Data.Common;
using System.Runtime.Loader;
using System.Threading;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using FluentMigrator.Expressions;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators.Postgres;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace EnterpriseWebLibrary.Postgres;

public sealed class PostgresProvider: ExternalPostgresProvider {
	private static readonly Dictionary<string, NpgsqlDataSource> dataSources = new( StringComparer.Ordinal );
	private static readonly Lock dataSourceCreationLock = new();

	private sealed class Processor: Postgres15_0Processor {
		public Processor(
			PostgresDbFactory factory, Postgres15_0Generator generator, ILogger<Postgres15_0Processor> logger, IOptionsSnapshot<ProcessorOptions> options,
			IConnectionStringAccessor accessor, PostgresOptions postgresOptions, NpgsqlDataSource source ): base(
			factory,
			generator,
			logger,
			options,
			accessor,
			postgresOptions ) {
			Connection = source.CreateConnection();
		}

		protected override void Process( string sql ) => executeWithIdentifierTruncationCheck( () => base.Process( sql ) );

		public override void Process( PerformDBOperationExpression expression ) => executeWithIdentifierTruncationCheck( () => base.Process( expression ) );

		private void executeWithIdentifierTruncationCheck( Action method ) {
			PostgresNotice? truncation = null;

			var connection = (NpgsqlConnection)Connection;
			connection.Notice += handleNotice;
			try {
				method();
				if( truncation is not null )
					throw new Exception( "PostgreSQL truncated an identifier: " + truncation.MessageText );
			}
			finally {
				connection.Notice -= handleNotice;
			}
			return;

			void handleNotice( object? sender, NpgsqlNoticeEventArgs e ) {
				if( e.Notice.SqlState == PostgresErrorCodes.NameTooLong )
					truncation ??= e.Notice;
			}
		}
	}

	private sealed class Quoter(): PostgresQuoter( null ) {
		public override bool IsQuoted( string name ) => false;
	}

	void ExternalPostgresProvider.InitStatics() {
		AppContext.SetSwitch( "Npgsql.DisableDateTimeInfinityConversions", true );
	}

	void ExternalPostgresProvider.CleanUpStatics() {
		// ReSharper disable once InconsistentlySynchronizedField
		foreach( var source in dataSources.Values )
			source.Dispose();
	}

	string ExternalPostgresProvider.GetConnectionString( string host, ushort? port, string database, bool supportsConnectionPooling, int timeout ) {
		var builder = new NpgsqlConnectionStringBuilder();

		builder.Host = host.Length > 0 ? host : "localhost";
		if( port.HasValue )
			builder.Port = port.Value;
		builder.Database = database;
		builder.Pooling = supportsConnectionPooling;
		builder.Timeout = timeout;
		builder.SearchPath = "public";
		builder.Options = "-c standard_conforming_strings=on";
		builder.Enlist = false;
		builder.ApplicationName = ConfigurationStatics.AppName;

		return builder.ConnectionString;
	}

	DbConnection ExternalPostgresProvider.CreateConnection( string connectionString ) => getDataSource( connectionString ).CreateConnection();

	DbCommand ExternalPostgresProvider.CreateCommand() => new NpgsqlCommand();

	DbParameter ExternalPostgresProvider.CreateParameter() => new NpgsqlParameter();

	void ExternalPostgresProvider.SetParameterType( DbParameter parameter, string dbTypeString ) =>
		( (NpgsqlParameter)parameter ).NpgsqlDbType = dbTypeString.ToEnum<NpgsqlDbType>();

	string ExternalPostgresProvider.GetDbTypeString( object databaseSpecificType ) => getDbTypeString( (NpgsqlDbType)databaseSpecificType );

	IReadOnlyCollection<InferredParameter> ExternalPostgresProvider.InferStatementParameterTypes( DatabaseConnection connection, string statement ) {
		using var command = new NpgsqlCommand( statement );
		connection.ExecuteCommandOperation( command, () => NpgsqlCommandBuilder.DeriveParameters( command ) );

		var typesByName = command.Connection!.GetSchema( "DataTypes" ).Rows.Cast<DataRow>().ToDictionary( i => (string)i[ "TypeName" ], StringComparer.Ordinal );
		return command.Parameters.Select( parameter => {
				var typeName = (string)typesByName[ parameter.DataTypeName! ][ "DataType" ];
				return new InferredParameter(
					parameter.ParameterName,
					AssemblyLoadContext.Default.Assemblies.Select( i => i.GetType( typeName ) ).Distinct().Single( i => i is not null )!,
					getDbTypeString( parameter.NpgsqlDbType ),
					0,
					null );
			} )
			.Materialize();
	}

	private string getDbTypeString( NpgsqlDbType dbType ) => dbType.ToString();

	Exception ExternalPostgresProvider.CreateConnectionException( Exception original, Func<string, string> messageGetter ) {
		if( original is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected } )
			return new DbConcurrencyException( messageGetter( "A concurrency error occurred." ), original );

		if( original is NpgsqlException { InnerException: TimeoutException } )
			return new DbConnectionFailureException( messageGetter( "A connection or command timeout occurred." ), original );
		if( original is NpgsqlException { InnerException: IOException or System.Net.Sockets.SocketException } ||
		    ( original is TewlContrib.ProcessTools.ProgramFailureException programFailure &&
		      programFailure.Message.Contains( "connection to server", StringComparison.Ordinal ) &&
		      programFailure.Message.Contains( "failed:", StringComparison.Ordinal ) ) )
			return new DbConnectionFailureException( messageGetter( "Communication with PostgreSQL failed." ), original );

		var message = messageGetter( original is PostgresException e ? $"SQLSTATE: {e.SqlState}." + ( e.ConstraintName is {} c ? $" Constraint: {c}." : "" ) : "" );
		if( original is PostgresException { SqlState: var sqlState } && sqlState.StartsWith( "08", StringComparison.Ordinal ) )
			return new DbConnectionFailureException( message, original );

		return new Exception( message, original );
	}

	Exception ExternalPostgresProvider.CreateCommandException( Exception originalException, Func<string, string> messageGetter ) {
		if( originalException is not NpgsqlException original )
			return new Exception( messageGetter( "" ), originalException );

		if( original is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected } )
			return new DbConcurrencyException( messageGetter( "A concurrency error occurred." ), original );

		if( original is { InnerException: TimeoutException } )
			return new DbCommandTimeoutException( messageGetter( "A command timeout occurred." ), original );

		var message = messageGetter( original is PostgresException e ? $"SQLSTATE: {e.SqlState}." + ( e.ConstraintName is {} c ? $" Constraint: {c}." : "" ) : "" );
		if( original is PostgresException { SqlState: var sqlState } && ( sqlState.StartsWith( "23", StringComparison.Ordinal ) ||
		                                                                  sqlState is PostgresErrorCodes.StringDataRightTruncation
			                                                                  or PostgresErrorCodes.NumericValueOutOfRange ) )
			return new DbCommandExecutionException( message, original );

		return new Exception( message, original );
	}

	bool ExternalPostgresProvider.RollbackExceptionIsLostTransaction( Exception exception ) =>
		exception is PostgresException { SqlState: PostgresErrorCodes.NoActiveSqlTransaction or PostgresErrorCodes.InvalidSavepointSpecification };

	void ExternalPostgresProvider.RegisterDependencyInjectionServicesForMigration( IMigrationRunnerBuilder builder ) {
		builder.AddPostgres();
		builder.Services.AddScoped<Postgres15_0Processor>( services => ActivatorUtilities.CreateInstance<Processor>(
			services,
			getDataSource( services.GetRequiredService<IConnectionStringAccessor>().ConnectionString ) ) );
		builder.Services.AddScoped<PostgresQuoter, Quoter>();
	}

	private NpgsqlDataSource getDataSource( string connectionString ) {
		lock( dataSourceCreationLock ) {
			if( !dataSources.TryGetValue( connectionString, out var source ) )
				dataSources.Add( connectionString, source = new NpgsqlDataSourceBuilder( connectionString ).UseNodaTime().Build() );
			return source;
		}
	}
}