// Copyright 2016 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Templates;

namespace EnterpriseWebLibrary.Sqlite.Serilog;

internal class Sink: BatchProvider, ILogEventSink {
	private static string timeFormat = null!;

	internal static void Init( string timeFormat ) {
		Sink.timeFormat = timeFormat;
	}

	private readonly string _databasePath;
	private readonly IFormatProvider? _formatProvider;
	private readonly string _tableName;
	private static readonly SemaphoreSlim semaphoreSlim = new( 1, 1 );

	public Sink( string sqlLiteDbPath, string tableName, IFormatProvider? formatProvider, uint batchSize = 100 ): base(
		batchSize: (int)batchSize,
		maxBufferSize: 100_000 ) {
		_databasePath = sqlLiteDbPath;
		_tableName = tableName;
		_formatProvider = formatProvider;

		InitializeDatabase();
	}

	#region ILogEvent implementation

	public void Emit( LogEvent logEvent ) {
		PushEvent( logEvent );
	}

	#endregion

	private void InitializeDatabase() {
		using var conn = GetSqLiteConnection();
		CreateSqlTable( conn );
	}

	private SqliteConnection GetSqLiteConnection() {
		var builder = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false };

		var sqLiteConnection = new SqliteConnection( builder.ConnectionString );
		sqLiteConnection.Open();

		// Microsoft.Data.Sqlite does not support setting the following pragmas with the connection string like System.Data.Sqlite does
		// So they must be set after opening the connection
		var sb = new StringBuilder();
		sb.Append( "PRAGMA journal_mode = Memory;" );
		sb.Append( "PRAGMA synchronous = Normal;" );
		sb.Append( "PRAGMA cache_size = 500;" );

		var pragmaCommand = new SqliteCommand( sb.ToString(), sqLiteConnection );
		pragmaCommand.ExecuteNonQuery();

		return sqLiteConnection;
	}

	private void CreateSqlTable( SqliteConnection sqlConnection ) {
		var colDefs = "Id INTEGER PRIMARY KEY AUTOINCREMENT,";
		colDefs += "Time TEXT,";
		colDefs += "Level TEXT,";
		colDefs += "Message TEXT,";
		colDefs += "Exception TEXT,";
		colDefs += "Properties TEXT";

		var sqlCreateText = $"CREATE TABLE IF NOT EXISTS {_tableName} ({colDefs})";

		var sqlCommand = new SqliteCommand( sqlCreateText, sqlConnection );
		sqlCommand.ExecuteNonQuery();
	}

	private SqliteCommand CreateSqlInsertCommand( SqliteConnection connection ) {
		var sqlInsertText = $"INSERT INTO {_tableName} ( Time, Level, Message, Exception, Properties )";
		sqlInsertText += " VALUES ( @time, @level, @message, @exception, @properties )";

		var sqlCommand = connection.CreateCommand();
		sqlCommand.CommandText = sqlInsertText;
		sqlCommand.CommandType = CommandType.Text;

		sqlCommand.Parameters.Add( new SqliteParameter( "@time", DbType.DateTime2 ) );
		sqlCommand.Parameters.Add( new SqliteParameter( "@level", DbType.String ) );
		sqlCommand.Parameters.Add( new SqliteParameter( "@message", DbType.String ) );
		sqlCommand.Parameters.Add( new SqliteParameter( "@exception", DbType.String ) );
		sqlCommand.Parameters.Add( new SqliteParameter( "@properties", DbType.String ) );

		return sqlCommand;
	}

	protected override async Task<bool> WriteLogEventAsync( ICollection<LogEvent>? logEventsBatch ) {
		if( logEventsBatch == null || logEventsBatch.Count == 0 )
			return true;
		await semaphoreSlim.WaitAsync().ConfigureAwait( false );
		try {
			await using var sqlConnection = GetSqLiteConnection();
			try {
				await WriteToDatabaseAsync( logEventsBatch, sqlConnection ).ConfigureAwait( false );
				return true;
			}
			catch( SqliteException e ) {
				SelfLog.WriteLine( e.Message );
				return false;
			}
			catch( Exception e ) {
				SelfLog.WriteLine( e.Message );
				return false;
			}
		}
		finally {
			semaphoreSlim.Release();
		}
	}

	private async Task WriteToDatabaseAsync( ICollection<LogEvent> logEventsBatch, SqliteConnection sqlConnection ) {
		await using var tr = sqlConnection.BeginTransaction();
		await using var sqlCommand = CreateSqlInsertCommand( sqlConnection );
		sqlCommand.Transaction = tr;

		var stringBuilder = new StringBuilder( 1000 );
		foreach( var logEvent in logEventsBatch ) {
			sqlCommand.Parameters[ "@time" ].Value = logEvent.Timestamp.ToUniversalTime().ToString( timeFormat );
			sqlCommand.Parameters[ "@level" ].Value = logEvent.Level.ToString();

			await using( var messageWriter = new StringWriter( stringBuilder ) ) {
				new ExpressionTemplate( "{@m}", formatProvider: _formatProvider ).Format( logEvent, messageWriter );
				sqlCommand.Parameters[ "@message" ].Value = messageWriter.ToString();
			}
			stringBuilder.Clear();

			sqlCommand.Parameters[ "@exception" ].Value = logEvent.Exception?.ToString() ?? string.Empty;

			await using( var propertyWriter = new StringWriter( stringBuilder ) ) {
				var formatter = new JsonValueFormatter();
				foreach( var property in logEvent.Properties ) {
					JsonValueFormatter.WriteQuotedJsonString( property.Key, propertyWriter );
					await propertyWriter.WriteAsync( ':' );
					formatter.Format( property.Value, propertyWriter );
					await propertyWriter.WriteAsync( ',' );
				}
			}
			if( stringBuilder.Length > 0 )
				stringBuilder.Length -= 1;
			sqlCommand.Parameters[ "@properties" ].Value = stringBuilder.ToString().Surround( "{", "}" );
			stringBuilder.Clear();

			await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait( false );
		}
		tr.Commit();
	}
}