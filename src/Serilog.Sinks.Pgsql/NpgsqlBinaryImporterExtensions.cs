using Npgsql;
using NpgsqlTypes;

namespace Serilog.Sinks.Pgsql
{
    public static class NpgsqlBinaryImporterExtensions
    {
        public static void WriteNullable<T>(this NpgsqlBinaryImporter writer, T value, NpgsqlDbType npgsqlDbType)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.Write(value, npgsqlDbType);
            }
        }
    }
}
