using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Serilog.Sinks.Pgsql
{
    /// <summary>
    ///     Options that pertain to columns
    /// </summary>
    public class ColumnOptions
    {
        public ColumnOptions(
            ICollection<StandardColumn> store = null,
            LevelColumnOptions level = null,
            PropertiesColumnOptions properties = null,
            ExceptionColumnOptions exception = null,
            MessageTemplateColumnOptions messageTemplate = null,
            MessageColumnOptions message = null, 
            TimeStampColumnOptions timeStamp = null,
            LogEventColumnOptions logEvent = null,
            ICollection<DataColumnMapping> additionalDataColumns = null)
        {
            Store = store ?? Enum.GetValues(typeof(StandardColumn)).Cast<StandardColumn>().ToList();
            Level = level ?? new LevelColumnOptions();
            Properties = properties ?? new PropertiesColumnOptions();
            Exception = exception ?? new ExceptionColumnOptions();
            MessageTemplate = messageTemplate ?? new MessageTemplateColumnOptions();
            Message = message ?? new MessageColumnOptions();
            TimeStamp = timeStamp ?? new TimeStampColumnOptions();
            LogEvent = logEvent ?? new LogEventColumnOptions();
            AdditionalDataColumns = additionalDataColumns ?? new DataColumnMapping[0];
        }

        /// <summary>
        ///     A list of columns that will be stored in the logs table in the database.
        /// </summary>
        public IEnumerable<StandardColumn> Store { get; }

        /// <summary>
        ///     Options for the Level column.
        /// </summary>
        public LevelColumnOptions Level { get; }

        /// <summary>
        ///     Options for the Properties column.
        /// </summary>
        public PropertiesColumnOptions Properties { get; }

        /// <summary>
        /// Options for the Exception column.
        /// </summary>
        public ExceptionColumnOptions Exception { get; }

        /// <summary>
        /// Options for the MessageTemplate column.
        /// </summary>
        public MessageTemplateColumnOptions MessageTemplate { get; }

        /// <summary>
        /// Options for the Message column.
        /// </summary>
        public MessageColumnOptions Message { get; }

        /// <summary>
        ///     Options for the TimeStamp column.
        /// </summary>
        public TimeStampColumnOptions TimeStamp { get; }

        /// <summary>
        ///     Options for the LogEvent column.
        /// </summary>
        public LogEventColumnOptions LogEvent { get; }

        /// <summary>
        ///     Additional columns for data storage.
        /// </summary>
        public IEnumerable<DataColumnMapping> AdditionalDataColumns { get; }

        /// <summary>
        ///     Options for the Level column.
        /// </summary>
        public class LevelColumnOptions : CommonColumnOptions
        {
            public LevelColumnOptions(string columnName = "Level") : base(columnName) { }

            /// <summary>
            ///     If true will store Level as an enum in a smallint column as opposed to a string.
            /// </summary>
            public bool StoreAsSmallint { get; set; }
        }

        /// <summary>
        ///     Options for the Properties column.
        /// </summary>
        public class PropertiesColumnOptions : CommonColumnOptions
        {
            /// <summary>
            ///     Default constructor.
            /// </summary>
            public PropertiesColumnOptions(string columnName = "Properties", bool excludeAdditionalProperties = false)
                : base(columnName)
            {
                ExcludeAdditionalProperties = excludeAdditionalProperties;
            }

            /// <summary>
            ///     Exclude properties from the Properties column if they are being saved to additional columns.
            /// </summary>
            public bool ExcludeAdditionalProperties { get; }
        }

        /// <summary>
        /// Shared column customization options.
        /// </summary>
        public class CommonColumnOptions
        {
            /// <summary>
            /// The name of the column in the database.
            /// </summary>
            public string ColumnName { get; }

            public CommonColumnOptions(string columnName)
            {
                if (string.IsNullOrWhiteSpace(columnName))
                    throw new ArgumentException(nameof(columnName));

                ColumnName = columnName;
            }
        }

        /// <summary>
        ///     Options for the TimeStamp column.
        /// </summary>
        public class TimeStampColumnOptions : CommonColumnOptions
        {
            public TimeStampColumnOptions(string columnName = "TimeStamp") : base(columnName) { }

            /// <summary>
            ///     If true, the time is converted to universal time.
            /// </summary>
            public bool ConvertToUtc { get; set; }
        }

        /// <summary>
        ///     Options for the LogEvent column.
        /// </summary>
        public class LogEventColumnOptions : CommonColumnOptions
        {
            public LogEventColumnOptions(string columnName = "LogEvent") : base(columnName) { }

            /// <summary>
            ///     Exclude properties from the LogEvent column if they are being saved to additional columns.
            /// </summary>
            public bool ExcludeAdditionalProperties { get; set; }
        }

        /// <summary>
        /// Options for the message column
        /// </summary>
        public class MessageColumnOptions : CommonColumnOptions
        {
            public MessageColumnOptions(string columnName = "Message") : base(columnName) { }
        }

        /// <summary>
        /// Options for the Exception column.
        /// </summary>
        public class ExceptionColumnOptions : CommonColumnOptions
        {
            public ExceptionColumnOptions(string columnName = "Exception") : base(columnName) { }
        }

        /// <summary>
        /// Options for the MessageTemplate column.
        /// </summary>
        public class MessageTemplateColumnOptions : CommonColumnOptions
        {
            public MessageTemplateColumnOptions(string columnName = "MessageTemplate") : base(columnName) { }
        }
    }
}
