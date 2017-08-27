// Copyright 2017 Serilog Contributors

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//     http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.Pgsql;
using System;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.Pgsql() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationPgsqlExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events to a table in a Pgsql database.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="connectionString">The connection string to the database where to store the events.</param>
        /// <param name="tableName">Name of the table to store the events in.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="columnOptions"></param>
        /// <param name="schemaName">Name of the schema for the table to store the data in. The default is 'dbo'.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration Pgsql(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            string tableName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = PgsqlSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null,
            ColumnOptions columnOptions = null,
            string schemaName = "public"
            )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");

            var defaultedPeriod = period ?? PgsqlSink.DefaultPeriod;

            return loggerConfiguration.Sink(
                new PgsqlSink(
                    connectionString,
                    tableName,
                    batchPostingLimit,
                    defaultedPeriod,
                    formatProvider,
                    columnOptions,
                    schemaName
                    ),
                restrictedToMinimumLevel);
        }
    }
}
