using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Serilog.Sinks.Pgsql.Sinks.Pgsql
{
    /// <summary>
    /// Formats LogEvents and Properties to compact JSON
    /// </summary>
    public class PgsqlJsonFormatter
    {
        private readonly JsonValueFormatter _valueFormatter;

        public PgsqlJsonFormatter(JsonValueFormatter valueFormatter = null)
        {
            _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
        }

        public string ToJson(LogEvent logEvent)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                writer.Write("{");
                WriteTimestamp(logEvent.Timestamp, writer);
                WriteMessageTemplate(logEvent.MessageTemplate, writer);
                WriteTokensWithFormat(logEvent.Properties, logEvent.MessageTemplate.Tokens, writer);
                WriteLevel(logEvent.Level, writer);
                WriteException(logEvent.Exception, writer);
                writer.Write(",");
                WriteProperties(logEvent.Properties, writer);
                writer.Write("}");
            }
            return sb.ToString();
        }

        private static void WriteTimestamp(DateTimeOffset timestamp, StringWriter writer)
        {
            writer.Write($"\"@t\":\"{timestamp.UtcDateTime.ToString("O")}\"");
        }

        private static void WriteMessageTemplate(MessageTemplate messageTemplate, StringWriter writer)
        {
            writer.Write(",\"@mt\":");
            JsonValueFormatter.WriteQuotedJsonString(messageTemplate.Text, writer);
        }

        private static void WriteLevel(LogEventLevel Level, StringWriter writer)
        {
            if (Level != LogEventLevel.Information)
            {
                writer.Write($",\"@l\":\"{Level}\"");
            }
        }

        private static void WriteException(Exception exception, StringWriter writer)
        {
            if (exception != null)
            {
                writer.Write(",\"@x\":");
                JsonValueFormatter.WriteQuotedJsonString(exception.ToString(), writer);
            }
        }

        private void WriteTokensWithFormat(
            IReadOnlyDictionary<string, LogEventPropertyValue> properties, 
            IEnumerable<MessageTemplateToken> tokens,
            StringWriter writer)
        {
            var tokensWithFormat = tokens
                    .OfType<PropertyToken>()
                    .Where(pt => pt.Format != null);

            if (tokensWithFormat.Any())
            {
                writer.Write(",\"@r\":[");
                var delim = "";
                foreach (var r in tokensWithFormat)
                {
                    writer.Write(delim);
                    delim = ",";
                    using (var space = new StringWriter())
                    {
                        r.Render(properties, space);
                        JsonValueFormatter.WriteQuotedJsonString(space.ToString(), writer);
                    }
                }
                writer.Write(']');
            }
        }

        public string ToJson(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                writer.Write("{");
                WriteProperties(properties, writer);
                writer.Write("}");
            }
            return sb.ToString();
        }

        private void WriteProperties(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties, StringWriter writer)
        {
            using (var enumerator = properties.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    writer.Write($"\"{enumerator.Current.Key}\":");
                    _valueFormatter.Format(enumerator.Current.Value, writer);
                }
                while (enumerator.MoveNext())
                {
                    writer.Write($",\"{enumerator.Current.Key}\":");
                    _valueFormatter.Format(enumerator.Current.Value, writer);
                }
            }
        }
    }
}
