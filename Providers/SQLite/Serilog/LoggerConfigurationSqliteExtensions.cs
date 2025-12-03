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

using System;
using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace EnterpriseWebLibrary.Sqlite.Serilog
{
    /// <summary>
    ///     Adds the WriteTo.SQLite() extension method to <see cref="LoggerConfiguration" />.
    /// </summary>
    public static class LoggerConfigurationSqliteExtensions
    {
        /// <summary>
        ///     Adds a sink that writes log events to a SQLite database.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="sqliteDbPath">The path of SQLite db.</param>
        /// <param name="tableName">The name of the SQLite table to store log.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="batchSize">Number of messages to save as batch to database. Default is 10, max 1000</param>
        /// <param name="levelSwitch">
        /// A switch allowing the pass-through minimum level to be changed at runtime.
        /// </param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration SQLite(
            this LoggerSinkConfiguration loggerConfiguration,
            string sqliteDbPath,
            string tableName = "Logs",
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider? formatProvider = null,
            LoggingLevelSwitch? levelSwitch = null,
            uint batchSize = 100)
        {

            if (string.IsNullOrEmpty(sqliteDbPath)) {
                SelfLog.WriteLine("Invalid sqliteDbPath");

                throw new ArgumentNullException(nameof(sqliteDbPath));
            }

            if (!Uri.TryCreate(sqliteDbPath, UriKind.RelativeOrAbsolute, out var sqliteDbPathUri)) {
                throw new ArgumentException($"Invalid path {nameof(sqliteDbPath)}");
            }

            // Throws an exception on .net6-android
            //if (!sqliteDbPathUri.IsAbsoluteUri) {
            //    var basePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            //    sqliteDbPath = Path.Combine(Path.GetDirectoryName(basePath) ?? throw new NullReferenceException(), sqliteDbPath);
            //}

            try {
                var sqliteDbFile = new FileInfo(sqliteDbPath);
                sqliteDbFile.Directory?.Create();

                return loggerConfiguration.Sink(
	                new Sink( sqliteDbFile.FullName, tableName, formatProvider, batchSize ),
	                restrictedToMinimumLevel,
	                levelSwitch );
            }
            catch (Exception ex) {
                SelfLog.WriteLine(ex.Message);

                throw;
            }
        }
    }
}