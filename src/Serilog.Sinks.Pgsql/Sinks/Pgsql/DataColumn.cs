using NpgsqlTypes;
using System;

namespace Serilog.Sinks.Pgsql
{
    /// <summary>
    /// Maps a log event property to a Pgsql table column
    /// </summary>
    public struct DataColumnMapping
    {
        public readonly string ColumnName;
        public readonly NpgsqlDbType ColumnType;
        public readonly string PropertyName;

        public DataColumnMapping(string columnName, NpgsqlDbType columnType, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) throw new ArgumentException(nameof(columnName));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException(nameof(propertyName));

            ColumnName = columnName;
            PropertyName = propertyName;
            ColumnType = columnType;
        }

        public DataColumnMapping(string columnAndPropertyName, NpgsqlDbType columnType)
            : this(columnAndPropertyName, columnType, columnAndPropertyName)
        {
        }
    }
}