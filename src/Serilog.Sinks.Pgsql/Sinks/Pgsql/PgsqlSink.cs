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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using Npgsql;
using NpgsqlTypes;
using Serilog.Sinks.Pgsql.Sinks.Pgsql;

namespace Serilog.Sinks.Pgsql
{
    /// <summary>
    ///     Writes log events as rows in a table of Pgsql database.
    /// </summary>
    public class PgsqlSink : PeriodicBatchingSink
    {
        /// <summary>
        ///     A reasonable default for the number of events posted in
        ///     each batch.
        /// </summary>
        public const int DefaultBatchPostingLimit = 50;

        /// <summary>
        ///     A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

        private readonly string _connectionString;
        private readonly IFormatProvider _formatProvider;
        private readonly string _tableName;
        private readonly string _schemaName;
        private readonly ColumnOptions _columnOptions;
        private readonly HashSet<string> _additionalDataColumnNames;
        private readonly PgsqlJsonFormatter _jsonFormatter;
        private readonly string _copyFromCommand;

        /// <summary>
        ///     Construct a sink posting to the specified database.
        /// </summary>
        /// <param name="connectionString">Connection string to access the database.</param>
        /// <param name="tableName">Name of the table to store the data in.</param>
        /// <param name="schemaName">Name of the schema for the table to store the data in. The default is 'dbo'.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="columnOptions">Options that pertain to columns</param>
        public PgsqlSink(
            string connectionString,
            string tableName,
            int batchPostingLimit,
            TimeSpan period,
            IFormatProvider formatProvider,
            ColumnOptions columnOptions = null,
            string schemaName = "dbo"
            )
            : base(batchPostingLimit, period)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionString");

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentNullException("tableName");

            _connectionString = connectionString;
            _tableName = tableName;
            _schemaName = schemaName;
            _formatProvider = formatProvider;
            _columnOptions = columnOptions ?? new ColumnOptions();
            _additionalDataColumnNames = 
                new HashSet<string>(
                    (_columnOptions.AdditionalDataColumns ?? Enumerable.Empty<DataColumnMapping>())
                        .Select(c => c.ColumnName),
                    StringComparer.OrdinalIgnoreCase);
            _jsonFormatter = new PgsqlJsonFormatter();
            _copyFromCommand = CreateCopyFromCommand(_schemaName, _tableName, _columnOptions);
        }

        /// <summary>
        ///     Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>
        ///     Override either <see cref="PeriodicBatchingSink.EmitBatch" /> or <see cref="PeriodicBatchingSink.EmitBatchAsync" />
        ///     ,
        ///     not both.
        /// </remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    using (var writer = conn.BeginBinaryImport(_copyFromCommand))
                    {
                        foreach (var logEvent in events)
                        {
                            WriteEvent(writer, logEvent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to write {0} log events to the database due to following error: {1}", events.Count(), ex.Message);
            }
        }

        private string CreateCopyFromCommand(string schemaName, string tableName, ColumnOptions columnOptions)
        {
            var fullTableName = $"\"{schemaName}\".\"{tableName}\"";
            var tableDescription = string.Join(
                ", ",
                columnOptions.Store.Select(ColumNameOrDefault)
                    .Concat(columnOptions.AdditionalDataColumns.Select(adc => adc.ColumnName))
                    .Select(x => $"\"{x}\""));
            var copyFromCommand = $"COPY {fullTableName} ({tableDescription}) FROM STDIN (FORMAT BINARY)";

            return copyFromCommand;
        }

        private string ColumNameOrDefault(StandardColumn column)
        {
            switch (column)
            {
                case StandardColumn.Message:
                    return _columnOptions.Message.ColumnName ?? "Message";
                case StandardColumn.MessageTemplate:
                    return _columnOptions.MessageTemplate.ColumnName ?? "MessageTemplate";
                case StandardColumn.Level:
                    return _columnOptions.Level.ColumnName ?? "Level";
                case StandardColumn.TimeStamp:
                    return _columnOptions.TimeStamp.ColumnName ?? "TimeStamp";
                case StandardColumn.Exception:
                    return _columnOptions.Exception.ColumnName ?? "Exception";
                case StandardColumn.Properties:
                    return _columnOptions.Properties.ColumnName ?? "Properties";
                case StandardColumn.LogEvent:
                    return _columnOptions.LogEvent.ColumnName ?? "LogEvent";
                default:
                    throw new ArgumentException(nameof(column));
            }
        }

        private void WriteEvent(NpgsqlBinaryImporter writer, LogEvent logEvent)
        {
            writer.StartRow();
            WriteRegularColumns(writer, logEvent);
            WriteAdditionalColumns(writer, logEvent);
        }

        private void WriteRegularColumns(NpgsqlBinaryImporter writer, LogEvent logEvent)
        {
            foreach (var column in _columnOptions.Store)
            {
                switch (column)
                {
                    case StandardColumn.Message:
                        var message = logEvent.RenderMessage(_formatProvider);
                        writer.WriteNullable(message, NpgsqlDbType.Text);
                        break;
                    case StandardColumn.MessageTemplate:
                        var messageTemplate = logEvent.MessageTemplate.Text;
                        writer.WriteNullable(messageTemplate, NpgsqlDbType.Text);
                        break;
                    case StandardColumn.Level:
                        WriteLevel(writer, logEvent);
                        break;
                    case StandardColumn.TimeStamp:
                        var timestamp = _columnOptions.TimeStamp.ConvertToUtc ? logEvent.Timestamp.DateTime.ToUniversalTime() : logEvent.Timestamp.DateTime;
                        writer.Write(timestamp, NpgsqlDbType.TimestampTZ);
                        break;
                    case StandardColumn.Exception:
                        var exception = logEvent.Exception?.ToString();
                        writer.WriteNullable(exception, NpgsqlDbType.Text);
                        break;
                    case StandardColumn.Properties:
                        var propertiesJson = ConvertPropertiesToJsonStructure(logEvent.Properties);
                        writer.Write(propertiesJson, NpgsqlDbType.Jsonb);
                        break;
                    case StandardColumn.LogEvent:
                        var logEventJson = LogEventToJson(logEvent);
                        writer.Write(logEventJson, NpgsqlDbType.Jsonb);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void WriteLevel(NpgsqlBinaryImporter writer, LogEvent logEvent)
        {
            if (_columnOptions.Level.StoreAsSmallint)
            {
                var level = (int)logEvent.Level;
                writer.Write(level, NpgsqlDbType.Smallint);
            }
            else
            {
                var level = logEvent.Level.ToString();
                writer.Write(level, NpgsqlDbType.Text);
            }
        }

        /// <summary>
        ///     Mapping values from properties which have a corresponding data row.
        ///     Matching is done based on Column name and property key
        /// </summary>
        private void WriteAdditionalColumns(NpgsqlBinaryImporter writer, LogEvent logEvent)
        {
            foreach (var column in _columnOptions.AdditionalDataColumns)
            {
                logEvent.Properties.TryGetValue(column.PropertyName, out LogEventPropertyValue propertyValue);
                var scalarValue = propertyValue as ScalarValue;
                if (scalarValue != null)
                {
                    writer.Write(scalarValue.ToString(), column.ColumnType);
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }

        private string ConvertPropertiesToJsonStructure(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            var options = _columnOptions.Properties;

            var filteredProperties = properties
                .Where(p => _columnOptions.Properties.ExcludeAdditionalProperties ?
                    !_additionalDataColumnNames.Contains(p.Key) : true);
            return _jsonFormatter.ToJson(filteredProperties);
        }

        private string LogEventToJson(LogEvent logEvent)
        {
            if (_columnOptions.LogEvent.ExcludeAdditionalProperties)
            {
                var filteredProperties = logEvent.Properties.Where(p => !_additionalDataColumnNames.Contains(p.Key));
                logEvent = new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, filteredProperties.Select(x => new LogEventProperty(x.Key, x.Value)));
            }

            return _jsonFormatter.ToJson(logEvent);
        }
    }
}
