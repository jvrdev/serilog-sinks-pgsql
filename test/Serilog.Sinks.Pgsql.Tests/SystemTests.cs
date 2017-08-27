using NpgsqlTypes;
using NUnit.Framework;
using System;

namespace Serilog.Sinks.Pgsql.Tests
{
    [TestFixture]
    public class SystemTests
    {
        [Test]
        public void WriteALogLine()
        {
            var columnOptions = new ColumnOptions(additionalDataColumns: 
                new[]
                {
                    new DataColumnMapping("Service", NpgsqlDbType.Text),
                    new DataColumnMapping("MachineName", NpgsqlDbType.Text)
                });
            var config = new Serilog.LoggerConfiguration()
                .Enrich.WithProperty("Service", "Tests")
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .WriteTo.Pgsql("Host=localhost;Database=logs_test;Username=postgres;Password=wblyStOLVnpyGKYv7sWr", "Logs", columnOptions: columnOptions);
            using (var logger = config.CreateLogger())
            {
                logger.Information("Hello world {@Bottle}", new { Liquid = "Water", Price = new { Amount = 5.0, Currency = "Euro" } });
            }
        }
    }
}
